using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MCGame.Core;
using MCGame.Rendering;
using MCGame.Utils;
using MCGame.Blocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MCGame.Chunks
{
    /// <summary>
    /// 区块管理器
    /// 管理区块的加载、卸载、生成和渲染
    /// 简化实现：使用字典存储区块，基于任务池的异步生成
    /// </summary>
    public class ChunkManager
    {
        // 区块存储
        private readonly Dictionary<ChunkPosition, Chunk> _loadedChunks;
        private readonly SimpleObjectPool<Chunk> _chunkPool;

        // 异步生成队列
        private readonly ConcurrentQueue<ChunkPosition> _generationQueue;
        private readonly ConcurrentQueue<Chunk> _meshingQueue;

        // 多线程控制
        private readonly Task[] _generationWorkers;
        private readonly Task[] _meshingWorkers;
        private readonly CancellationTokenSource _cancellationToken;

        // 配置参数
        private readonly int _loadRadius = 10;
        private readonly int _unloadRadius = 15;
        private readonly int _maxConcurrentGeneration = 4;
        private readonly int _maxConcurrentMeshing = 2;

        // 世界设置
        private readonly long _worldSeed;
        private readonly WorldSettings _worldSettings;

        // 性能统计
        private int _chunksGeneratedThisFrame;
        private int _chunksMeshedThisFrame;

        // 引用
        private readonly BlockRegistry _blockRegistry;
        private readonly GraphicsDevice _graphicsDevice;

        public IReadOnlyDictionary<ChunkPosition, Chunk> LoadedChunks => _loadedChunks;
        public int LoadedChunkCount => _loadedChunks.Count;
        public int QueuedGenerationCount => _generationQueue.Count;
        public int QueuedMeshingCount => _meshingQueue.Count;

        public ChunkManager(GraphicsDevice graphicsDevice, BlockRegistry blockRegistry, WorldSettings worldSettings)
        {
            _graphicsDevice = graphicsDevice;
            _blockRegistry = blockRegistry;
            _worldSettings = worldSettings;
            _worldSeed = worldSettings.Seed;

            // 初始化存储
            _loadedChunks = new Dictionary<ChunkPosition, Chunk>();
            _generationQueue = new ConcurrentQueue<ChunkPosition>();
            _meshingQueue = new ConcurrentQueue<Chunk>();

            // 初始化对象池
            _chunkPool = new SimpleObjectPool<Chunk>(() => new Chunk(new ChunkPosition(0, 0, 0)), chunk => chunk.Reset(), 10, 50);

            // 初始化多线程工作器
            _cancellationToken = new CancellationTokenSource();
            _generationWorkers = new Task[_maxConcurrentGeneration];
            _meshingWorkers = new Task[_maxConcurrentMeshing];

            // 启动工作线程
            StartWorkers();
        }

        /// <summary>
        /// 启动工作线程
        /// </summary>
        private void StartWorkers()
        {
            // 启动区块生成工作器
            for (int i = 0; i < _generationWorkers.Length; i++)
            {
                _generationWorkers[i] = Task.Run(GenerationWorker);
            }

            // 启动网格生成工作器
            for (int i = 0; i < _meshingWorkers.Length; i++)
            {
                _meshingWorkers[i] = Task.Run(MeshingWorker);
            }
        }

        /// <summary>
        /// 区块生成工作器
        /// </summary>
        private async Task GenerationWorker()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (_generationQueue.TryDequeue(out var chunkPos))
                {
                    try
                    {
                        var chunk = GetOrCreateChunk(chunkPos);
                        if (chunk.State == ChunkState.Unloaded)
                        {
                            await GenerateChunkAsync(chunk);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error generating chunk {chunkPos}: {ex.Message}");
                    }
                }
                else
                {
                    await Task.Delay(10); // 避免CPU占用过高
                }
            }
        }

        /// <summary>
        /// 网格生成工作器
        /// </summary>
        private async Task MeshingWorker()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (_meshingQueue.TryDequeue(out var chunk))
                {
                    try
                    {
                        if (chunk.IsDirty && chunk.State == ChunkState.Ready)
                        {
                            await GenerateChunkMeshAsync(chunk);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error meshing chunk {chunk.Position}: {ex.Message}");
                    }
                }
                else
                {
                    await Task.Delay(10); // 避免CPU占用过高
                }
            }
        }

        /// <summary>
        /// 异步生成区块
        /// </summary>
        private async Task GenerateChunkAsync(Chunk chunk)
        {
            chunk.State = ChunkState.Generating;

            // 在主线程中生成地形（简化实现）
            await Task.Run(() =>
            {
                chunk.GenerateTerrain(_worldSeed);
                chunk.State = ChunkState.Ready;
                chunk.IsLoaded = true;
                chunk.IsDirty = true;

                // 加入网格生成队列
                _meshingQueue.Enqueue(chunk);
            });
        }

        /// <summary>
        /// 异步生成区块网格
        /// </summary>
        private async Task GenerateChunkMeshAsync(Chunk chunk)
        {
            chunk.State = ChunkState.Meshing;

            await Task.Run(() =>
            {
                var mesher = new ChunkMesher(_blockRegistry, _graphicsDevice);
                chunk.Mesh = mesher.GenerateMesh(chunk);
                chunk.IsMeshGenerated = true;
                chunk.IsDirty = false;
                chunk.State = ChunkState.Ready;

                Interlocked.Increment(ref _chunksMeshedThisFrame);
            });
        }

        /// <summary>
        /// 更新区块管理器
        /// </summary>
        public void Update(Vector3 playerPosition)
        {
            // 重置统计
            _chunksGeneratedThisFrame = 0;
            _chunksMeshedThisFrame = 0;

            // 计算玩家所在区块
            var playerChunkPos = ChunkPosition.FromWorldPosition(playerPosition, Chunk.SIZE);

            // 更新区块加载
            UpdateChunkLoading(playerChunkPos);

            // 更新区块卸载
            UpdateChunkUnloading(playerChunkPos);

            // 更新邻近区块引用
            UpdateChunkNeighbors();
        }

        /// <summary>
        /// 更新区块加载
        /// </summary>
        private void UpdateChunkLoading(ChunkPosition playerChunkPos)
        {
            // 计算需要加载的区块范围
            var chunksToLoad = GetChunksInRadius(playerChunkPos, _loadRadius);

            foreach (var chunkPos in chunksToLoad)
            {
                if (!_loadedChunks.ContainsKey(chunkPos))
                {
                    // 加入生成队列
                    _generationQueue.Enqueue(chunkPos);
                }
            }
        }

        /// <summary>
        /// 更新区块卸载
        /// </summary>
        private void UpdateChunkUnloading(ChunkPosition playerChunkPos)
        {
            var chunksToUnload = new List<ChunkPosition>();

            foreach (var chunkPos in _loadedChunks.Keys)
            {
                var distance = GetChunkDistance(playerChunkPos, chunkPos);
                if (distance > _unloadRadius)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }

            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }
        }

        /// <summary>
        /// 更新区块邻近引用
        /// </summary>
        private void UpdateChunkNeighbors()
        {
            foreach (var chunk in _loadedChunks.Values)
            {
                var neighbors = new Chunk[3, 3, 3];

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            var neighborPos = new ChunkPosition(
                                chunk.Position.X + x,
                                chunk.Position.Y + y,
                                chunk.Position.Z + z
                            );

                            _loadedChunks.TryGetValue(neighborPos, out neighbors[x + 1, y + 1, z + 1]);
                        }
                    }
                }

                chunk.UpdateNeighbors(neighbors);
            }
        }

        /// <summary>
        /// 获取指定半径内的区块
        /// </summary>
        private List<ChunkPosition> GetChunksInRadius(ChunkPosition center, int radius)
        {
            var chunks = new List<ChunkPosition>();

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        var distance = Math.Sqrt(x * x + y * y + z * z);
                        if (distance <= radius)
                        {
                            chunks.Add(new ChunkPosition(
                                center.X + x,
                                center.Y + y,
                                center.Z + z
                            ));
                        }
                    }
                }
            }

            return chunks;
        }

        /// <summary>
        /// 获取区块间距离
        /// </summary>
        private float GetChunkDistance(ChunkPosition a, ChunkPosition b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// 获取或创建区块
        /// </summary>
        private Chunk GetOrCreateChunk(ChunkPosition position)
        {
            if (_loadedChunks.TryGetValue(position, out var chunk))
            {
                return chunk;
            }

            chunk = _chunkPool.Get();
            chunk.Reset();
            chunk.Position = position;
            chunk.State = ChunkState.Loading;

            _loadedChunks[position] = chunk;
            return chunk;
        }

        /// <summary>
        /// 卸载区块
        /// </summary>
        private void UnloadChunk(ChunkPosition position)
        {
            if (_loadedChunks.TryGetValue(position, out var chunk))
            {
                chunk.Unload();
                _loadedChunks.Remove(position);
                _chunkPool.Return(chunk);
            }
        }

        /// <summary>
        /// 获取指定位置的区块
        /// </summary>
        public Chunk GetChunk(ChunkPosition position)
        {
            _loadedChunks.TryGetValue(position, out var chunk);
            return chunk;
        }

        /// <summary>
        /// 获取世界坐标处的区块
        /// </summary>
        public Chunk GetChunkAtWorldPosition(Vector3 worldPosition)
        {
            var chunkPos = ChunkPosition.FromWorldPosition(worldPosition, Chunk.SIZE);
            return GetChunk(chunkPos);
        }

        /// <summary>
        /// 获取指定位置的方块
        /// </summary>
        public BlockData GetBlock(Vector3 worldPosition)
        {
            var chunk = GetChunkAtWorldPosition(worldPosition);
            return chunk?.GetBlockWorld(worldPosition) ?? BlockData.Empty;
        }

        /// <summary>
        /// 设置指定位置的方块
        /// </summary>
        public void SetBlock(Vector3 worldPosition, BlockData block)
        {
            var chunk = GetChunkAtWorldPosition(worldPosition);
            chunk?.SetBlockWorld(worldPosition, block);
        }

        /// <summary>
        /// 获取所有可见区块
        /// </summary>
        public List<Chunk> GetVisibleChunks(FrustumCulling frustumCulling)
        {
            var visibleChunks = new List<Chunk>();

            foreach (var chunk in _loadedChunks.Values)
            {
                if (chunk.IsLoaded && chunk.IsMeshGenerated && frustumCulling.IsChunkVisible(chunk))
                {
                    visibleChunks.Add(chunk);
                }
            }

            return visibleChunks;
        }

        /// <summary>
        /// 获取性能统计
        /// </summary>
        public ChunkManagerStats GetStats()
        {
            return new ChunkManagerStats
            {
                LoadedChunks = _loadedChunks.Count,
                QueuedGeneration = _generationQueue.Count,
                QueuedMeshing = _meshingQueue.Count,
                ChunksGeneratedThisFrame = _chunksGeneratedThisFrame,
                ChunksMeshedThisFrame = _chunksMeshedThisFrame,
                PoolSize = _chunkPool.Count
            };
        }

        /// <summary>
        /// 销毁区块管理器
        /// </summary>
        public void Dispose()
        {
            // 取消所有工作线程
            _cancellationToken.Cancel();

            // 等待线程完成
            Task.WaitAll(_generationWorkers.Concat(_meshingWorkers).ToArray());

            // 卸载所有区块
            foreach (var chunk in _loadedChunks.Values)
            {
                chunk.Unload();
            }

            _loadedChunks.Clear();
            _chunkPool.Dispose();
            _cancellationToken.Dispose();
        }
    }

    /// <summary>
    /// 区块管理器统计信息
    /// </summary>
    public struct ChunkManagerStats
    {
        public int LoadedChunks { get; set; }
        public int QueuedGeneration { get; set; }
        public int QueuedMeshing { get; set; }
        public int ChunksGeneratedThisFrame { get; set; }
        public int ChunksMeshedThisFrame { get; set; }
        public int PoolSize { get; set; }
    }
}