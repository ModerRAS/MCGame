using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Xna.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Components;
using MCGame.ECS.Managers;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.Tests.Benchmark.Performance
{
    /// <summary>
    /// ECS性能基准测试
    /// 使用BenchmarkDotNet进行性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class ECSBenchmarks
    {
        private const int EntityCount = 10000;
        private const int QueryIterations = 1000;
        private const int SystemIterations = 1000;

        [GlobalSetup]
        public void Setup()
        {
            // 预热JIT
            var warmupStore = new EntityStore();
            for (int i = 0; i < 100; i++)
            {
                warmupStore.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
            }
        }

        [Benchmark]
        public void EntityCreation()
        {
            var store = new EntityStore();
            
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
            }
        }

        [Benchmark]
        public void EntityCreationBatch()
        {
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            
            var blockTypes = new BlockType[EntityCount];
            var positions = new Vector3[EntityCount];
            
            for (int i = 0; i < EntityCount; i++)
            {
                blockTypes[i] = BlockType.Grass;
                positions[i] = new Vector3(i, 0, 0);
            }
            
            blockManager.SetBlocksBatch(blockTypes, positions);
        }

        [Benchmark]
        public void QueryExecution()
        {
            var store = new EntityStore();
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
            }
            
            var query = store.Query<Block, Position>();
            
            for (int i = 0; i < QueryIterations; i++)
            {
                var count = query.Count;
                var entities = query.Entities.ToList();
            }
        }

        [Benchmark]
        public void QuerySingleComponent()
        {
            var store = new EntityStore();
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
            }
            
            var query = store.Query<Block>();
            
            for (int i = 0; i < QueryIterations; i++)
            {
                var count = query.Count;
                var entities = query.Entities.ToList();
            }
        }

        [Benchmark]
        public void QueryMultipleComponents()
        {
            var store = new EntityStore();
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true),
                    new Collider(new BoundingBox(new Vector3(i, 0, 0), new Vector3(i + 1, 1, 1))),
                    new Lighting(15)
                );
            }
            
            var query = store.Query<Block, Position, Visibility>();
            
            for (int i = 0; i < QueryIterations; i++)
            {
                var count = query.Count;
                var entities = query.Entities.ToList();
            }
        }

        [Benchmark]
        public void SystemUpdate()
        {
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Velocity(0, -1, 0),
                    new Physics()
                );
            }
            
            var testSystem = new BenchmarkPhysicsSystem();
            systemRoot.Add(testSystem);
            
            for (int i = 0; i < SystemIterations; i++)
            {
                systemRoot.Update(new UpdateTick());
            }
        }

        [Benchmark]
        public void SystemUpdateMultipleSystems()
        {
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Velocity(0, -1, 0),
                    new Physics(),
                    new Visibility(true),
                    new Lifetime(10f)
                );
            }
            
            var physicsSystem = new BenchmarkPhysicsSystem();
            var visibilitySystem = new BenchmarkVisibilitySystem();
            var lifetimeSystem = new BenchmarkLifetimeSystem();
            
            systemRoot.Add(physicsSystem);
            systemRoot.Add(visibilitySystem);
            systemRoot.Add(lifetimeSystem);
            
            for (int i = 0; i < SystemIterations; i++)
            {
                systemRoot.Update(new UpdateTick());
            }
        }

        [Benchmark]
        public void ComponentAccess()
        {
            var store = new EntityStore();
            var entities = new List<Friflo.Engine.ECS.Entity>();
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                var entity = store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
                entities.Add(entity);
            }
            
            // 组件访问测试
            for (int i = 0; i < QueryIterations; i++)
            {
                foreach (var entity in entities)
                {
                    var position = entity.GetComponent<Position>();
                    var block = entity.GetComponent<Block>();
                    var visibility = entity.GetComponent<Visibility>();
                }
            }
        }

        [Benchmark]
        public void ComponentMutation()
        {
            var store = new EntityStore();
            var entities = new List<Friflo.Engine.ECS.Entity>();
            
            // 创建测试实体
            for (int i = 0; i < EntityCount; i++)
            {
                var entity = store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Velocity(0, -1, 0),
                    new Physics()
                );
                entities.Add(entity);
            }
            
            // 组件修改测试
            for (int i = 0; i < QueryIterations; i++)
            {
                foreach (var entity in entities)
                {
                    var position = entity.GetComponent<Position>();
                    var velocity = entity.GetComponent<Velocity>();
                    
                    position.Value += velocity.Value * 0.016f;
                    velocity.Value *= 0.99f;
                }
            }
        }

        [Benchmark]
        public void BlockManagerOperations()
        {
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            
            // 创建方块
            for (int i = 0; i < EntityCount; i++)
            {
                blockManager.SetBlock(BlockType.Grass, new Vector3(i, 0, 0));
            }
            
            // 方块管理器操作测试
            for (int i = 0; i < QueryIterations; i++)
            {
                var allBlocks = blockManager.GetAllBlocks();
                var blockCount = blockManager.GetBlockCount();
                var chunkCount = blockManager.GetChunkCount();
                
                foreach (var block in allBlocks.Take(100))
                {
                    var position = block.GetComponent<Position>();
                    var blockType = block.GetComponent<Block>();
                }
            }
        }

        [Benchmark]
        public void RaycastOperations()
        {
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            
            // 创建测试场景
            for (int x = 0; x < 100; x++)
            {
                for (int z = 0; z < 100; z++)
                {
                    blockManager.SetBlock(BlockType.Grass, new Vector3(x, 0, z));
                }
            }
            
            // 射线检测测试
            for (int i = 0; i < QueryIterations; i++)
            {
                var origin = new Vector3(i % 100, 5, i / 100);
                var direction = new Vector3(0, -1, 0);
                var hit = blockManager.Raycast(origin, direction, 10f);
            }
        }

        [Benchmark]
        public void ECSWorldOperations()
        {
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // 创建测试世界
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    ecsWorld.CreateChunkEntity(new ChunkPosition(x, 0, z));
                }
            }
            
            for (int x = -25; x <= 25; x++)
            {
                for (int z = -25; z <= 25; z++)
                {
                    if (x % 2 == 0 && z % 2 == 0)
                    {
                        ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(x, 0, z));
                    }
                }
            }
            
            // ECS世界操作测试
            for (int i = 0; i < SystemIterations; i++)
            {
                var gameTime = new GameTime(TimeSpan.FromSeconds(i * 0.016), TimeSpan.FromSeconds(0.016));
                ecsWorld.Update(gameTime);
                
                var stats = ecsWorld.GetEntityStats();
                var visibleBlocks = ecsWorld.GetVisibleBlocks();
                var visibleChunks = ecsWorld.GetVisibleChunks();
            }
        }

        [Benchmark]
        public void MemoryAllocationTest()
        {
            var store = new EntityStore();
            var initialMemory = GC.GetTotalMemory(false);
            
            // 创建大量实体
            for (int i = 0; i < EntityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true),
                    new Collider(new BoundingBox(new Vector3(i, 0, 0), new Vector3(i + 1, 1, 1))),
                    new Lighting(15)
                );
            }
            
            var afterCreationMemory = GC.GetTotalMemory(false);
            
            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            var afterGCMemory = GC.GetTotalMemory(false);
        }

        [Benchmark]
        public void ConcurrentOperations()
        {
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            var testSystem = new BenchmarkPhysicsSystem();
            systemRoot.Add(testSystem);
            
            var threadCount = 4;
            var operationsPerThread = EntityCount / threadCount;
            var threads = new List<System.Threading.Thread>();
            
            // 创建线程
            for (int t = 0; t < threadCount; t++)
            {
                var threadId = t;
                var thread = new System.Threading.Thread(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var entityId = threadId * operationsPerThread + i;
                        store.CreateEntity(
                            new Position(new Vector3(entityId, 0, 0)),
                            new Velocity(0, -1, 0),
                            new Physics()
                        );
                    }
                });
                threads.Add(thread);
            }
            
            // 启动线程
            foreach (var thread in threads)
            {
                thread.Start();
            }
            
            // 等待线程完成
            foreach (var thread in threads)
            {
                thread.Join();
            }
            
            // 运行系统更新
            for (int i = 0; i < SystemIterations; i++)
            {
                systemRoot.Update(new UpdateTick());
            }
        }
    }

    #region Benchmark Systems

    public class BenchmarkPhysicsSystem : QuerySystem<Position, Velocity, Physics>
    {
        protected override void OnUpdate()
        {
            foreach (var entity in Query.Entities)
            {
                var position = entity.GetComponent<Position>();
                var velocity = entity.GetComponent<Velocity>();
                var physics = entity.GetComponent<Physics>();
                
                // 应用重力
                velocity.Value += physics.Gravity * 0.016f;
                
                // 应用阻力
                velocity.Value *= (1f - physics.Drag * 0.016f);
                
                // 更新位置
                position.Value += velocity.Value * 0.016f;
                
                // 简单的地面碰撞
                if (position.Value.Y <= 0)
                {
                    position.Value.Y = 0;
                    velocity.Value.Y = 0;
                }
            }
        }
    }

    public class BenchmarkVisibilitySystem : QuerySystem<Position, Visibility>
    {
        private Vector3 _cameraPosition = new Vector3(0, 64, 0);
        
        protected override void OnUpdate()
        {
            foreach (var entity in Query.Entities)
            {
                var position = entity.GetComponent<Position>();
                var visibility = entity.GetComponent<Visibility>();
                
                var distance = Vector3.Distance(position.Value, _cameraPosition);
                visibility.IsVisible = distance < 200f;
                visibility.Distance = distance;
            }
        }
    }

    public class BenchmarkLifetimeSystem : QuerySystem<Lifetime>
    {
        protected override void OnUpdate()
        {
            foreach (var entity in Query.Entities)
            {
                var lifetime = entity.GetComponent<Lifetime>();
                lifetime.TimeLeft -= 0.016f;
                
                if (lifetime.TimeLeft <= 0)
                {
                    lifetime.IsExpired = true;
                }
            }
        }
    }

    #endregion
}