using Microsoft.Xna.Framework;
using Xunit;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS;
using MCGame.ECS.Components;
using MCGame.ECS.Managers;
using MCGame.Core;
using System.Diagnostics;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.Tests.Integration.Performance
{
    /// <summary>
    /// 性能测试
    /// 测试ECS系统的性能表现
    /// </summary>
    public class PerformanceTests
    {
        [Fact]
        public void EntityCreation_ShouldBeFast()
        {
            // Arrange
            var store = new EntityStore();
            var entityCount = 10000;
            var stopwatch = new Stopwatch();
            
            // Act
            stopwatch.Start();
            
            var entities = new List<Friflo.Engine.ECS.Entity>();
            for (int i = 0; i < entityCount; i++)
            {
                var entity = store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
                entities.Add(entity);
            }
            
            stopwatch.Stop();
            
            // Assert
            var creationTime = stopwatch.ElapsedMilliseconds;
            var entitiesPerSecond = entityCount / (creationTime / 1000.0);
            
            Assert.True(creationTime < 1000, $"Entity creation took {creationTime}ms, expected < 1000ms");
            Assert.True(entitiesPerSecond > 10000, $"Created {entitiesPerSecond:F0} entities/second, expected > 10000");
            
            Console.WriteLine($"Created {entityCount} entities in {creationTime}ms ({entitiesPerSecond:F0} entities/second)");
        }

        [Fact]
        public void QueryPerformance_ShouldBeFast()
        {
            // Arrange
            var store = new EntityStore();
            var entityCount = 10000;
            
            // 创建测试实体
            for (int i = 0; i < entityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
            }
            
            var query = store.Query<Block, Position>();
            var stopwatch = new Stopwatch();
            var iterations = 1000;
            
            // Act
            stopwatch.Start();
            
            for (int i = 0; i < iterations; i++)
            {
                var count = query.Count;
                var entities = query.Entities.ToList();
            }
            
            stopwatch.Stop();
            
            // Assert
            var queryTime = stopwatch.ElapsedMilliseconds;
            var queriesPerSecond = iterations / (queryTime / 1000.0);
            
            Assert.True(queryTime < 1000, $"Query execution took {queryTime}ms, expected < 1000ms");
            Assert.True(queriesPerSecond > 100, $"Executed {queriesPerSecond:F0} queries/second, expected > 100");
            
            Console.WriteLine($"Executed {iterations} queries in {queryTime}ms ({queriesPerSecond:F0} queries/second)");
        }

        [Fact]
        public void SystemUpdatePerformance_ShouldBeFast()
        {
            // Arrange
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            var entityCount = 1000;
            
            // 创建测试实体
            for (int i = 0; i < entityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Velocity(0, -1, 0),
                    new Physics()
                );
            }
            
            // 添加测试系统
            var testSystem = new PerformanceTestSystem();
            systemRoot.Add(testSystem);
            
            var stopwatch = new Stopwatch();
            var iterations = 1000;
            
            // Act
            stopwatch.Start();
            
            for (int i = 0; i < iterations; i++)
            {
                systemRoot.Update(new UpdateTick());
            }
            
            stopwatch.Stop();
            
            // Assert
            var updateTime = stopwatch.ElapsedMilliseconds;
            var updatesPerSecond = iterations / (updateTime / 1000.0);
            
            Assert.True(updateTime < 1000, $"System update took {updateTime}ms, expected < 1000ms");
            Assert.True(updatesPerSecond > 60, $"Executed {updatesPerSecond:F0} updates/second, expected > 60");
            
            Console.WriteLine($"Executed {iterations} system updates in {updateTime}ms ({updatesPerSecond:F0} updates/second)");
            Console.WriteLine($"Processed {testSystem.TotalProcessedEntities} entities total");
        }

        [Fact]
        public void BlockManagerBatchPerformance_ShouldBeFast()
        {
            // Arrange
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            var batchCount = 100;
            var batchSize = 100;
            var totalBlocks = batchCount * batchSize;
            
            var stopwatch = new Stopwatch();
            
            // Act
            stopwatch.Start();
            
            for (int batch = 0; batch < batchCount; batch++)
            {
                var blockTypes = new BlockType[batchSize];
                var positions = new Vector3[batchSize];
                
                for (int i = 0; i < batchSize; i++)
                {
                    blockTypes[i] = BlockType.Grass;
                    positions[i] = new Vector3(batch * batchSize + i, 0, 0);
                }
                
                blockManager.SetBlocksBatch(blockTypes, positions);
            }
            
            stopwatch.Stop();
            
            // Assert
            var creationTime = stopwatch.ElapsedMilliseconds;
            var blocksPerSecond = totalBlocks / (creationTime / 1000.0);
            
            Assert.True(creationTime < 2000, $"Batch creation took {creationTime}ms, expected < 2000ms");
            Assert.True(blocksPerSecond > 50000, $"Created {blocksPerSecond:F0} blocks/second, expected > 50000");
            
            Console.WriteLine($"Created {totalBlocks} blocks in {creationTime}ms ({blocksPerSecond:F0} blocks/second)");
        }

        [Fact]
        public void MemoryUsage_ShouldBeReasonable()
        {
            // Arrange
            var store = new EntityStore();
            var initialMemory = GC.GetTotalMemory(false);
            
            // Act
            var entityCount = 10000;
            var entities = new List<Friflo.Engine.ECS.Entity>();
            
            for (int i = 0; i < entityCount; i++)
            {
                var entity = store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true),
                    new Collider(new BoundingBox(new Vector3(i, 0, 0), new Vector3(i + 1, 1, 1))),
                    new Lighting(15)
                );
                entities.Add(entity);
            }
            
            var afterCreationMemory = GC.GetTotalMemory(false);
            
            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            var afterGCMemory = GC.GetTotalMemory(false);
            
            // Assert
            var memoryPerEntity = (afterCreationMemory - initialMemory) / (double)entityCount;
            var memoryAfterGC = (afterGCMemory - initialMemory) / (double)entityCount;
            
            Assert.True(memoryPerEntity < 1000, $"Memory per entity: {memoryPerEntity:F0} bytes, expected < 1000");
            Assert.True(memoryAfterGC < 500, $"Memory per entity after GC: {memoryAfterGC:F0} bytes, expected < 500");
            
            Console.WriteLine($"Memory per entity: {memoryPerEntity:F0} bytes");
            Console.WriteLine($"Memory per entity after GC: {memoryAfterGC:F0} bytes");
            Console.WriteLine($"Total memory for {entityCount} entities: {afterCreationMemory - initialMemory} bytes");
        }

        [Fact]
        public void ECSWorldPerformance_ShouldBeFast()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var stopwatch = new Stopwatch();
            
            // 创建测试世界
            stopwatch.Start();
            
            // 创建区块
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    ecsWorld.CreateChunkEntity(new ChunkPosition(x, 0, z));
                }
            }
            
            // 创建方块
            for (int x = -50; x <= 50; x++)
            {
                for (int z = -50; z <= 50; z++)
                {
                    if (x % 2 == 0 && z % 2 == 0)
                    {
                        ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(x, 0, z));
                    }
                }
            }
            
            // 创建玩家
            ecsWorld.PlayerEntity = ecsWorld.Store.CreateEntity(
                new Position(0, 64, 0),
                new MCGame.ECS.Components.Rotation(0, 0, 0),
                new Velocity(0, 0, 0),
                new MCGame.ECS.Components.Player(),
                new Input(),
                new Camera(),
                new Visibility(true)
            );
            
            var creationTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            
            // 更新世界
            var updateCount = 100;
            for (int i = 0; i < updateCount; i++)
            {
                var gameTime = new GameTime(
                    TimeSpan.FromSeconds(i * 0.016),
                    TimeSpan.FromSeconds(0.016));
                ecsWorld.Update(gameTime);
            }
            
            var updateTime = stopwatch.ElapsedMilliseconds;
            
            // Assert
            Assert.True(creationTime < 2000, $"World creation took {creationTime}ms, expected < 2000ms");
            Assert.True(updateTime < 2000, $"World update took {updateTime}ms, expected < 2000ms");
            
            var stats = ecsWorld.GetEntityStats();
            Console.WriteLine($"Created world with {stats.TotalEntities} entities in {creationTime}ms");
            Console.WriteLine($"Updated world {updateCount} times in {updateTime}ms");
            Console.WriteLine($"Average update time: {updateTime / (double)updateCount:F2}ms");
        }

        [Fact]
        public void RaycastPerformance_ShouldBeFast()
        {
            // Arrange
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
            
            var raycastCount = 1000;
            var stopwatch = new Stopwatch();
            
            // Act
            stopwatch.Start();
            
            for (int i = 0; i < raycastCount; i++)
            {
                var origin = new Vector3(i % 100, 5, i / 100);
                var direction = new Vector3(0, -1, 0);
                var hit = blockManager.Raycast(origin, direction, 10f);
            }
            
            stopwatch.Stop();
            
            // Assert
            var raycastTime = stopwatch.ElapsedMilliseconds;
            var raycastsPerSecond = raycastCount / (raycastTime / 1000.0);
            
            Assert.True(raycastTime < 1000, $"Raycast took {raycastTime}ms, expected < 1000ms");
            Assert.True(raycastsPerSecond > 1000, $"Executed {raycastsPerSecond:F0} raycasts/second, expected > 1000");
            
            Console.WriteLine($"Executed {raycastCount} raycasts in {raycastTime}ms ({raycastsPerSecond:F0} raycasts/second)");
        }

        [Fact]
        public void ChunkLoadingPerformance_ShouldBeFast()
        {
            // Arrange
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            var chunkManager = new ECSChunkManager(store, blockManager, 10);
            
            var stopwatch = new Stopwatch();
            
            // Act
            stopwatch.Start();
            
            // 模拟玩家移动和区块加载
            for (int step = 0; step < 20; step++)
            {
                var playerChunk = new ChunkPosition(step, 0, step);
                chunkManager.UpdateChunkLoading(playerChunk);
            }
            
            stopwatch.Stop();
            
            // Assert
            var loadingTime = stopwatch.ElapsedMilliseconds;
            var stats = chunkManager.GetStats();
            
            Assert.True(loadingTime < 1000, $"Chunk loading took {loadingTime}ms, expected < 1000ms");
            Assert.True(stats.TotalChunks > 0, $"No chunks were loaded");
            
            Console.WriteLine($"Loaded {stats.TotalChunks} chunks in {loadingTime}ms");
            Console.WriteLine($"Average chunk loading time: {loadingTime / (double)stats.TotalChunks:F2}ms per chunk");
        }

        [Fact]
        public void LargeScaleSimulation_ShouldBeStable()
        {
            // Arrange
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            
            // 创建大量实体
            var entityCount = 5000;
            for (int i = 0; i < entityCount; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 100, 0)),
                    new Velocity(0, -1, 0),
                    new Physics(1f, 0.1f),
                    new Visibility(true)
                );
            }
            
            // 添加物理系统
            var physicsSystem = new PerformancePhysicsSystem();
            systemRoot.Add(physicsSystem);
            
            var stopwatch = new Stopwatch();
            var frameCount = 1000;
            
            // Act
            stopwatch.Start();
            
            for (int frame = 0; frame < frameCount; frame++)
            {
                systemRoot.Update(new UpdateTick());
            }
            
            stopwatch.Stop();
            
            // Assert
            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageFrameTime = totalTime / (double)frameCount;
            var fps = 1000 / averageFrameTime;
            
            Assert.True(averageFrameTime < 16.67, $"Average frame time: {averageFrameTime:F2}ms, expected < 16.67ms (60 FPS)");
            Assert.True(fps > 30, $"FPS: {fps:F1}, expected > 30");
            
            Console.WriteLine($"Simulated {entityCount} entities for {frameCount} frames");
            Console.WriteLine($"Average frame time: {averageFrameTime:F2}ms");
            Console.WriteLine($"FPS: {fps:F1}");
            Console.WriteLine($"Total time: {totalTime}ms");
        }

        #region Test Systems

        private class PerformanceTestSystem : QuerySystem<Position, Velocity>
        {
            public int TotalProcessedEntities { get; private set; }

            protected override void OnUpdate()
            {
                var count = Query.Entities.Count();
                TotalProcessedEntities += count;
                
                // 简单的位置更新
                foreach (var entity in Query.Entities)
                {
                    var position = entity.GetComponent<Position>();
                    var velocity = entity.GetComponent<Velocity>();
                    position.Value += velocity.Value * 0.016f;
                }
            }
        }

        private class PerformancePhysicsSystem : QuerySystem<Position, Velocity, Physics>
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

        #endregion
    }
}