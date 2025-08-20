using Microsoft.Xna.Framework;
using Friflo.Engine.ECS;
using MCGame.ECS.Components;
using MCGame.ECS.Managers;
using MCGame.Core;
using System.Collections.Concurrent;

namespace MCGame.ECS.Managers
{
    /// <summary>
    /// ECS区块管理器
    /// 使用ECS系统管理区块实体，提供高性能的区块操作
    /// 简化实现：专注于区块的ECS化管理和动态加载卸载
    /// </summary>
    public class ECSChunkManager
    {
        private readonly EntityStore _store;
        private readonly ECSBlockManager _blockManager;
        private readonly ArchetypeQuery _chunkQuery;
        private readonly ConcurrentDictionary<ChunkPosition, Entity> _chunkEntities;
        private readonly int _renderDistance;
        private ChunkPosition _lastPlayerChunk;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ECSChunkManager(EntityStore store, ECSBlockManager blockManager, int renderDistance = 10)
        {
            _store = store;
            _blockManager = blockManager;
            _renderDistance = renderDistance;
            _chunkQuery = _store.Query<Chunk, MCGame.ECS.Components.Position>();
            _chunkEntities = new ConcurrentDictionary<ChunkPosition, Entity>();
            _lastPlayerChunk = new ChunkPosition(0, 0, 0);
        }

        /// <summary>
        /// 创建区块
        /// </summary>
        public Entity CreateChunk(ChunkPosition position)
        {
            if (_chunkEntities.ContainsKey(position))
            {
                return _chunkEntities[position];
            }

            var worldPos = position.ToWorldPosition(16);
            var bounds = new BoundingBox(worldPos, worldPos + new Vector3(16, 256, 16));

            var chunkEntity = _store.CreateEntity(
                new Chunk(position),
                new MCGame.ECS.Components.Position(worldPos),
                new Mesh(bounds),
                new Visibility(true),
                new Collider(bounds, false)
            );

            _chunkEntities[position] = chunkEntity;
            
            // 标记区块需要生成
            var chunkComponent = chunkEntity.GetComponent<Chunk>();
            chunkComponent.IsDirty = true;
            chunkComponent.State = MCGame.ECS.Components.ChunkState.Loading;

            return chunkEntity;
        }

        /// <summary>
        /// 获取区块
        /// </summary>
        public Entity GetChunk(ChunkPosition position)
        {
            _chunkEntities.TryGetValue(position, out var entity);
            return entity;
        }

        /// <summary>
        /// 卸载区块
        /// </summary>
        public bool UnloadChunk(ChunkPosition position)
        {
            if (_chunkEntities.TryRemove(position, out var entity))
            {
                // 删除区块内的所有方块
                var blocks = _blockManager.GetBlocksInChunk(position);
                
                foreach (var block in blocks)
                {
                    // TODO: 找到Friflo ECS的正确删除方法
                  // _store.DeleteEntity(block.Id); // 方法不存在
                }
                
                // TODO: 找到Friflo ECS的正确删除方法
                // _store.DeleteEntity(entity.Id); // 方法不存在
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新区块加载状态（基于玩家位置）
        /// </summary>
        public void UpdateChunkLoading(ChunkPosition playerChunkPos)
        {
            if (playerChunkPos.Equals(_lastPlayerChunk))
            {
                return; // 玩家区块位置未改变
            }

            _lastPlayerChunk = playerChunkPos;

            // 找出需要加载的区块
            var chunksToLoad = new List<ChunkPosition>();
            var chunksToUnload = new List<ChunkPosition>();

            foreach (var chunkPos in _chunkEntities.Keys)
            {
                var distance = GetChunkDistance(playerChunkPos, chunkPos);
                if (distance > _renderDistance + 2)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }

            // 生成需要加载的区块
            for (int x = playerChunkPos.X - _renderDistance; x <= playerChunkPos.X + _renderDistance; x++)
            {
                for (int z = playerChunkPos.Z - _renderDistance; z <= playerChunkPos.Z + _renderDistance; z++)
                {
                    var chunkPos = new ChunkPosition(x, 0, z);
                    if (!_chunkEntities.ContainsKey(chunkPos))
                    {
                        chunksToLoad.Add(chunkPos);
                    }
                }
            }

            // 卸载远距离区块
            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }

            // 加载新区块
            foreach (var chunkPos in chunksToLoad)
            {
                CreateChunk(chunkPos);
            }
        }

        /// <summary>
        /// 获取已加载的区块
        /// </summary>
        public Entity[] GetLoadedChunks()
        {
            var chunks = new List<Entity>();
            foreach (var chunk in _chunkQuery.Entities)
            {
                var chunkComponent = chunk.GetComponent<Chunk>();
                if (chunkComponent.IsLoaded)
                {
                    chunks.Add(chunk);
                }
            }
            return chunks.ToArray();
        }

        /// <summary>
        /// 获取可见区块
        /// </summary>
        public Entity[] GetVisibleChunks()
        {
            var chunks = new List<Entity>();
            foreach (var chunk in _chunkQuery.Entities)
            {
                var visibility = chunk.GetComponent<Visibility>();
                if (visibility.IsVisible)
                {
                    chunks.Add(chunk);
                }
            }
            return chunks.ToArray();
        }

        /// <summary>
        /// 获取需要生成网格的区块
        /// </summary>
        public Entity[] GetDirtyChunks()
        {
            var chunks = new List<Entity>();
            foreach (var chunk in _chunkQuery.Entities)
            {
                var chunkComponent = chunk.GetComponent<Chunk>();
                var meshComponent = chunk.GetComponent<Mesh>();
                
                if (chunkComponent.IsDirty || meshComponent.IsDirty)
                {
                    chunks.Add(chunk);
                }
            }
            return chunks.ToArray();
        }

        /// <summary>
        /// 标记区块为脏（需要重新生成）
        /// </summary>
        public void MarkChunkDirty(ChunkPosition position)
        {
            if (_chunkEntities.TryGetValue(position, out var entity))
            {
                var chunkComponent = entity.GetComponent<Chunk>();
                var meshComponent = entity.GetComponent<Mesh>();
                
                chunkComponent.IsDirty = true;
                meshComponent.IsDirty = true;
            }
        }

        /// <summary>
        /// 标记区块为已加载
        /// </summary>
        public void MarkChunkLoaded(ChunkPosition position)
        {
            if (_chunkEntities.TryGetValue(position, out var entity))
            {
                var chunkComponent = entity.GetComponent<Chunk>();
                chunkComponent.IsLoaded = true;
                chunkComponent.State = MCGame.ECS.Components.ChunkState.Loaded;
            }
        }

        /// <summary>
        /// 标记区块网格已生成
        /// </summary>
        public void MarkChunkMeshGenerated(ChunkPosition position)
        {
            if (_chunkEntities.TryGetValue(position, out var entity))
            {
                var chunkComponent = entity.GetComponent<Chunk>();
                var meshComponent = entity.GetComponent<Mesh>();
                
                chunkComponent.IsMeshGenerated = true;
                meshComponent.IsDirty = false;
                chunkComponent.IsDirty = false;
                chunkComponent.State = MCGame.ECS.Components.ChunkState.Loaded;
            }
        }

        /// <summary>
        /// 获取区块数量
        /// </summary>
        public int GetChunkCount()
        {
            return _chunkEntities.Count;
        }

        /// <summary>
        /// 获取已加载的区块数量
        /// </summary>
        public int GetLoadedChunkCount()
        {
            var count = 0;
            foreach (var chunk in _chunkQuery.Entities)
            {
                var chunkComponent = chunk.GetComponent<Chunk>();
                if (chunkComponent.IsLoaded)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取需要更新的区块数量
        /// </summary>
        public int GetDirtyChunkCount()
        {
            var count = 0;
            foreach (var chunk in _chunkQuery.Entities)
            {
                var chunkComponent = chunk.GetComponent<Chunk>();
                var meshComponent = chunk.GetComponent<Mesh>();
                
                if (chunkComponent.IsDirty || meshComponent.IsDirty)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空所有区块
        /// </summary>
        public void ClearAll()
        {
            foreach (var chunk in _chunkQuery.Entities)
            {
                // TODO: 找到Friflo ECS的正确删除方法
                  // _store.DeleteEntity(chunk.Id); // 方法不存在
            }
            
            _chunkEntities.Clear();
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public ChunkStats GetStats()
        {
            return new ChunkStats
            {
                TotalChunks = _chunkEntities.Count,
                LoadedChunks = GetLoadedChunkCount(),
                DirtyChunks = GetDirtyChunkCount(),
                RenderDistance = _renderDistance
            };
        }

        /// <summary>
        /// 计算区块距离
        /// </summary>
        private static int GetChunkDistance(ChunkPosition a, ChunkPosition b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dz = Math.Abs(a.Z - b.Z);
            return Math.Max(dx, dz);
        }

        /// <summary>
        /// 区块统计信息
        /// </summary>
        public struct ChunkStats
        {
            public int TotalChunks;
            public int LoadedChunks;
            public int DirtyChunks;
            public int RenderDistance;
        }
    }
}