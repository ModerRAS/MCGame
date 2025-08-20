using Microsoft.Xna.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Components;
using MCGame.ECS.Systems;
using MCGame.ECS.Utils;
using PlayerComponent = MCGame.ECS.Components.Player;
using MCGame.Core;

namespace MCGame.ECS
{
    /// <summary>
    /// ECS世界管理器
    /// 管理ECS实体、组件和系统的生命周期
    /// 简化实现：专注于方块、区块和玩家的ECS化管理
    /// 性能优化：集成对象池、查询缓存和性能监控
    /// </summary>
    public class ECSWorld : IDisposable
    {
        private readonly EntityStore _store;
        private readonly SystemRoot _systemRoot;
        private readonly ArchetypeQuery _chunkQuery;
        private readonly ArchetypeQuery _blockQuery;
        private readonly ArchetypeQuery _playerQuery;
        
        // 性能优化组件
        private readonly ECSObjectPool _objectPool;
        private readonly QueryCacheManager _cacheManager;
        private readonly BatchOperationOptimizer _batchOptimizer;
        private readonly ECSPerformanceMonitor _performanceMonitor;

        // 系统引用
        private PlayerInputSystem _inputSystem;
        private PlayerMovementSystem _movementSystem;
        private PhysicsSystem _physicsSystem;
        private CameraSystem _cameraSystem;
        private VisibilitySystem _visibilitySystem;
        private ChunkStateSystem _chunkSystem;

        // 玩家实体
        public Entity PlayerEntity { get; set; }

        /// <summary>
        /// 获取实体存储
        /// </summary>
        public EntityStore Store => _store;
        
        /// <summary>
        /// 获取实体存储（兼容性）
        /// </summary>
        public EntityStore EntityStore => _store;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ECSWorld()
        {
            // 创建实体存储
            _store = new EntityStore();

            // 创建系统根节点
            _systemRoot = new SystemRoot(_store);

            // 初始化性能优化组件
            _objectPool = new ECSObjectPool();
            _cacheManager = new QueryCacheManager(_store, _objectPool);
            _batchOptimizer = new BatchOperationOptimizer(_store, _objectPool, _cacheManager);
            _performanceMonitor = new ECSPerformanceMonitor(_objectPool);

            // 初始化系统
            InitializeSystems();

            // 创建查询
            _chunkQuery = _store.Query<Chunk>();
            _blockQuery = _store.Query<Block>();
            _playerQuery = _store.Query<PlayerComponent>();

            // 不创建默认玩家，由主游戏创建
            // CreateDefaultPlayer();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        private void InitializeSystems()
        {
            // 输入系统
            _inputSystem = new PlayerInputSystem();
            _systemRoot.Add(_inputSystem);

            // 移动系统
            _movementSystem = new PlayerMovementSystem();
            _systemRoot.Add(_movementSystem);

            // 物理系统
            _physicsSystem = new PhysicsSystem();
            _systemRoot.Add(_physicsSystem);

            // 相机系统
            _cameraSystem = new CameraSystem();
            _systemRoot.Add(_cameraSystem);

            // 可见性系统
            _visibilitySystem = new VisibilitySystem();
            _systemRoot.Add(_visibilitySystem);

            // 区块系统
            _chunkSystem = new ChunkStateSystem();
            _systemRoot.Add(_chunkSystem);

            // 可选：启用性能监控
            _systemRoot.SetMonitorPerf(true);
        }

        /// <summary>
        /// 创建默认玩家
        /// </summary>
        private void CreateDefaultPlayer()
        {
            PlayerEntity = _store.CreateEntity(
                new MCGame.ECS.Components.Position(0, 64, 0),
                new MCGame.ECS.Components.Rotation(0, 0, 0),
                new Velocity(0, 0, 0),
                new PlayerComponent(),
                new Camera(75f, 16f/9f, 0.1f, 1000f),
                new Input(),
                new Physics(1f, 0.1f),
                new Collider(new BoundingBox(new Vector3(-0.3f, 0, -0.3f), new Vector3(0.3f, 1.8f, 0.3f))),
                new Visibility(true)
            );
        }

        /// <summary>
        /// 创建区块实体
        /// </summary>
        public Entity CreateChunkEntity(ChunkPosition position)
        {
            var worldPos = position.ToWorldPosition(16);
            var bounds = new BoundingBox(worldPos, worldPos + new Vector3(16, 256, 16));

            return _store.CreateEntity(
                new Chunk(position),
                new MCGame.ECS.Components.Position(worldPos),
                new Mesh(bounds),
                new Visibility(true),
                new Collider(bounds, false) // 区块不是实心碰撞体
            );
        }

        /// <summary>
        /// 创建方块实体
        /// </summary>
        public Entity CreateBlockEntity(BlockType blockType, Vector3 position)
        {
            var bounds = new BoundingBox(position, position + Vector3.One);
            
            return _store.CreateEntity(
                new Block(blockType),
                new MCGame.ECS.Components.Position(position),
                new Visibility(true),
                new Collider(bounds),
                new Lighting(15)
            );
        }

        /// <summary>
        /// 批量创建方块实体（优化性能）
        /// </summary>
        public Entity[] CreateBlockEntitiesBatch(BlockType[] blockTypes, Vector3[] positions)
        {
            // 使用性能优化的批量创建方法
            var entities = _batchOptimizer.CreateBlocksBatch(blockTypes, positions);
            
            // 记录创建操作
            _performanceMonitor.RecordValue("EntityStore.Creations", blockTypes.Length);
            _cacheManager.MarkChanged();
            
            return entities;
        }

        /// <summary>
        /// 获取区块实体
        /// </summary>
        public Entity GetChunkEntity(ChunkPosition position)
        {
            foreach (var chunk in _chunkQuery.Entities)
            {
                if (chunk.GetComponent<Chunk>().Position.Equals(position))
                {
                    return chunk;
                }
            }
            return default;
        }

        /// <summary>
        /// 获取指定位置的方块实体
        /// </summary>
        public Entity GetBlockEntity(Vector3 position)
        {
            foreach (var block in _blockQuery.Entities)
            {
                var blockPos = block.GetComponent<MCGame.ECS.Components.Position>().Value;
                if (Vector3.Distance(blockPos, position) < 0.1f)
                {
                    return block;
                }
            }
            return default;
        }

        /// <summary>
        /// 更新世界
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // 开始性能监控
            _performanceMonitor.BeginFrame();
            
            try
            {
                // 记录实体数量
                _performanceMonitor.RecordValue("EntityStore.Count", _store.Count);
                
                // 更新Tick时间
                // Tick.UpdateTime(gameTime); // 可能需要使用Friflo ECS的UpdateTick

                // 更新所有系统
                var systemStopwatch = System.Diagnostics.Stopwatch.StartNew();
                _systemRoot.Update(new UpdateTick());
                systemStopwatch.Stop();
                
                // 记录系统更新时间
                _performanceMonitor.RecordValue("System.UpdateTime", systemStopwatch.Elapsed.TotalMilliseconds);
            }
            finally
            {
                // 结束性能监控
                _performanceMonitor.EndFrame();
            }
        }

        /// <summary>
        /// 设置视锥体（用于可见性计算）
        /// </summary>
        public void SetViewFrustum(BoundingFrustum frustum, Vector3 cameraPosition)
        {
            _visibilitySystem.SetViewFrustum(frustum, cameraPosition);
        }

        /// <summary>
        /// 获取所有可见区块
        /// </summary>
        public Entity[] GetVisibleChunks()
        {
            var visibleChunks = new List<Entity>();
            foreach (var chunk in _chunkQuery.Entities)
            {
                var visibility = chunk.GetComponent<Visibility>();
                if (visibility.IsVisible)
                {
                    visibleChunks.Add(chunk);
                }
            }
            return visibleChunks.ToArray();
        }

        /// <summary>
        /// 获取所有可见方块
        /// </summary>
        public Entity[] GetVisibleBlocks()
        {
            var visibleBlocks = new List<Entity>();
            foreach (var block in _blockQuery.Entities)
            {
                var visibility = block.GetComponent<Visibility>();
                if (visibility.IsVisible)
                {
                    visibleBlocks.Add(block);
                }
            }
            return visibleBlocks.ToArray();
        }

        /// <summary>
        /// 销毁世界
        /// </summary>
        public void Destroy()
        {
            // 简化实现：不需要显式清理，让GC处理
            // 如果需要清理，可以遍历所有实体并删除
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Destroy();
            
            // 释放性能优化组件
            _objectPool?.Clear();
            _cacheManager?.Clear();
            _performanceMonitor?.Dispose();
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            return _systemRoot.GetPerfLog();
        }

        /// <summary>
        /// 获取优化后的性能统计信息
        /// </summary>
        public string GetOptimizedPerformanceStats()
        {
            return _performanceMonitor.GetPerformanceReport();
        }

        /// <summary>
        /// 获取性能警告
        /// </summary>
        public List<string> GetPerformanceWarnings()
        {
            return _performanceMonitor.GetPerformanceWarnings();
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public void ResetPerformanceStats()
        {
            _performanceMonitor.Reset();
        }

        /// <summary>
        /// 获取可见方块（使用缓存）
        /// </summary>
        public Entity[] GetVisibleBlocksOptimized()
        {
            var cache = _cacheManager.GetVisibleBlocksCache();
            return cache.Data.ToArray();
        }

        /// <summary>
        /// 获取可见区块（使用缓存）
        /// </summary>
        public Entity[] GetVisibleChunksOptimized()
        {
            var cache = _cacheManager.GetVisibleChunksCache();
            return cache.Data.ToArray();
        }

        /// <summary>
        /// 批量处理方块实体（性能优化）
        /// </summary>
        public void ProcessBlocksBatchOptimized(Action<Entity> processAction)
        {
            _batchOptimizer.ProcessBlocksByTypeBatch((blockType, entities) =>
            {
                foreach (var entity in entities)
                {
                    processAction(entity);
                }
            });
        }

        /// <summary>
        /// 批量处理区块实体（性能优化）
        /// </summary>
        public void ProcessChunksBatchOptimized(Action<Entity> processAction)
        {
            _batchOptimizer.ProcessBlocksByTypeBatch((blockType, entities) =>
            {
                foreach (var entity in entities)
                {
                    processAction(entity);
                }
            });
        }

        /// <summary>
        /// 获取实体统计信息
        /// </summary>
        public EntityStats GetEntityStats()
        {
            return new EntityStats
            {
                TotalEntities = _store.Count,
                ChunkEntities = _chunkQuery.Count,
                BlockEntities = _blockQuery.Count,
                PlayerEntities = _playerQuery.Count
            };
        }

        /// <summary>
        /// 实体统计信息
        /// </summary>
        public struct EntityStats
        {
            public int TotalEntities;
            public int ChunkEntities;
            public int BlockEntities;
            public int PlayerEntities;
        }
    }
}