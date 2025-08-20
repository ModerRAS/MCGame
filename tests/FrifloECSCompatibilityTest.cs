using Microsoft.Xna.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Components;
using MCGame.ECS.Managers;
using MCGame.ECS.Systems;
using MCGame.Blocks;
using MCGame.Core;
using System;
using System.Diagnostics;
using PlayerComponent = MCGame.ECS.Components.Player;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.ECS.Tests
{
    /// <summary>
    /// Friflo ECS兼容性测试
    /// 验证ECS实现是否与Friflo ECS 3.4.2 API完全兼容
    /// </summary>
    public class FrifloECSCompatibilityTest
    {
        private readonly EntityStore _store;
        private readonly SystemRoot _systemRoot;
        private readonly ECSBlockManager _blockManager;
        private readonly ECSChunkManager _chunkManager;

        public FrifloECSCompatibilityTest()
        {
            _store = new EntityStore();
            _systemRoot = new SystemRoot(_store);
            _blockManager = new ECSBlockManager(_store);
            _chunkManager = new ECSChunkManager(_store, _blockManager);
        }

        /// <summary>
        /// 运行所有兼容性测试
        /// </summary>
        public TestResult RunAllTests()
        {
            var result = new TestResult();
            
            // 测试1: 实体创建和组件操作
            result.EntityCreationTest = TestEntityCreation();
            
            // 测试2: QuerySystem功能
            result.QuerySystemTest = TestQuerySystem();
            
            // 测试3: 批量操作
            result.BatchOperationTest = TestBatchOperations();
            
            // 测试4: 实体删除
            result.EntityDeletionTest = TestEntityDeletion();
            
            // 测试5: 内存管理
            result.MemoryManagementTest = TestMemoryManagement();
            
            // 测试6: 系统集成
            result.SystemIntegrationTest = TestSystemIntegration();
            
            // 计算总体结果
            result.TotalTests = 6;
            result.PassedTests = new[] {
                result.EntityCreationTest,
                result.QuerySystemTest,
                result.BatchOperationTest,
                result.EntityDeletionTest,
                result.MemoryManagementTest,
                result.SystemIntegrationTest
            }.Count(t => t);
            
            return result;
        }

        /// <summary>
        /// 测试实体创建和组件操作
        /// </summary>
        private bool TestEntityCreation()
        {
            try
            {
                // 创建实体
                var entity = _store.CreateEntity(
                    new Block(BlockType.Grass),
                    new Position(1, 2, 3),
                    new Visibility(true)
                );

                // 验证实体创建成功
                if (entity.Id == 0) return false;

                // 验证组件访问
                var block = entity.GetComponent<Block>();
                var position = entity.GetComponent<Position>();
                var visibility = entity.GetComponent<Visibility>();

                if (block.Type != BlockType.Grass) return false;
                if (position.Value != new Vector3(1, 2, 3)) return false;
                if (visibility.IsVisible != true) return false;

                // 验证TryGetComponent
                if (!entity.TryGetComponent<Block>(out var block2)) return false;
                if (block2.Type != BlockType.Grass) return false;

                // 验证不存在组件
                if (entity.TryGetComponent<MCGame.ECS.Components.Rotation>(out _)) return false;

                // 清理 - 暂时注释删除方法
                // TODO: 找到Friflo ECS的正确删除方法
                // entity.Dispose(); // Friflo ECS可能没有Dispose方法
                // entity.DeleteEntity(); // Friflo ECS可能没有DeleteEntity方法
                // _store.DeleteEntity(entity.Id); // Friflo ECS可能没有DeleteEntity方法
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Entity Creation Test Failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试QuerySystem功能
        /// </summary>
        private bool TestQuerySystem()
        {
            try
            {
                // 创建测试实体
                var entities = new List<Friflo.Engine.ECS.Entity>();
                for (int i = 0; i < 10; i++)
                {
                    var entity = _store.CreateEntity(
                        new Block(BlockType.Stone),
                        new Position(i, 0, 0),
                        new Visibility(i % 2 == 0)
                    );
                    entities.Add(entity);
                }

                // 测试ArchetypeQuery
                var query = _store.Query<Block, Position>();
                if (query.EntityCount != 10) return false;

                // 测试带过滤的查询
                var visibleQuery = _store.Query<Block, Position, Visibility>();
                var visibleCount = 0;
                foreach (var entity in visibleQuery.Entities)
                {
                    var visibility = entity.GetComponent<Visibility>();
                    if (visibility.IsVisible) visibleCount++;
                }
                if (visibleCount != 5) return false;

                // 测试QuerySystem
                var testSystem = new CompatibilityTestQuerySystem();
                var systemRoot = new SystemRoot(_store);
                systemRoot.Add(testSystem);

                // 运行系统
                systemRoot.Update(new UpdateTick());

                // 验证系统是否正确执行
                if (!testSystem.Executed) return false;

                // 清理 - 暂时注释删除方法
                // TODO: 找到Friflo ECS的正确删除方法
                // foreach (var entity in entities)
                // {
                //     entity.DeleteEntity();
                // }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QuerySystem Test Failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试批量操作
        /// </summary>
        private bool TestBatchOperations()
        {
            try
            {
                // 准备批量数据
                var positions = new Vector3[100];
                var blockTypes = new BlockType[100];
                for (int i = 0; i < 100; i++)
                {
                    positions[i] = new Vector3(i, 0, 0);
                    blockTypes[i] = BlockType.Dirt;
                }

                // 批量创建
                _blockManager.SetBlocksBatch(blockTypes, positions);

                // 验证创建结果
                var allBlocks = _blockManager.GetAllBlocks();
                if (allBlocks.Length != 100) return false;

                // 批量删除
                foreach (var pos in positions)
                {
                    _blockManager.RemoveBlock(pos);
                }

                // 验证删除结果
                var remainingBlocks = _blockManager.GetAllBlocks();
                if (remainingBlocks.Length != 0) return false;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Batch Operation Test Failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试实体删除
        /// </summary>
        private bool TestEntityDeletion()
        {
            try
            {
                // 创建实体
                var entity = _store.CreateEntity(
                    new Block(BlockType.Grass),
                    new Position(1, 2, 3)
                );

                var entityId = entity.Id;

                // 删除实体 - 暂时注释删除方法
                // TODO: 找到Friflo ECS的正确删除方法
                // entity.DeleteEntity(); // Friflo ECS可能没有DeleteEntity方法
                // _store.DeleteEntity(entity.Id); // Friflo ECS可能没有DeleteEntity方法

                // 验证实体已被删除
                var query = _store.Query<Block>();
                foreach (var existingEntity in query.Entities)
                {
                    if (existingEntity.Id == entityId)
                    {
                        return false; // 实体仍然存在
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Entity Deletion Test Failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试内存管理
        /// </summary>
        private bool TestMemoryManagement()
        {
            try
            {
                var initialMemory = GC.GetTotalMemory(false);

                // 创建大量实体
                var entities = new List<Friflo.Engine.ECS.Entity>();
                for (int i = 0; i < 1000; i++)
                {
                    var entity = _store.CreateEntity(
                        new Block(BlockType.Stone),
                        new Position(i, 0, 0),
                        new Visibility(true)
                    );
                    entities.Add(entity);
                }

                var afterCreationMemory = GC.GetTotalMemory(false);

                // 删除所有实体
                foreach (var entity in entities)
                {
                    // entity.DeleteEntity(); // TODO: 找到Friflo ECS的正确删除方法
                }

                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var afterCleanupMemory = GC.GetTotalMemory(false);

                // 验证内存被正确释放
                var memoryIncrease = afterCreationMemory - initialMemory;
                var memoryAfterCleanup = afterCleanupMemory - initialMemory;

                // 允许一些内存开销，但应该大部分被释放
                if (memoryAfterCleanup > memoryIncrease * 0.3)
                {
                    Console.WriteLine($"Memory Management Test: Memory not properly released");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Memory Management Test Failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试系统集成
        /// </summary>
        private bool TestSystemIntegration()
        {
            try
            {
                // 创建ECS世界
                var ecsWorld = new MCGame.ECS.ECSWorld();

                // 创建测试实体
                var playerEntity = ecsWorld.Store.CreateEntity(
                    new MCGame.ECS.Components.Position(0, 64, 0),
                    new Velocity(0, 0, 0),
                    new MCGame.ECS.Components.Player(),
                    new Input(),
                    new Visibility(true)
                );

                // 运行系统更新
                var gameTime = new GameTime();
                ecsWorld.Update(gameTime);

                // 验证系统正常运行
                var stats = ecsWorld.GetPerformanceStats();
                if (string.IsNullOrEmpty(stats)) return false;

                var entityStats = ecsWorld.GetEntityStats();
                if (entityStats.TotalEntities == 0) return false;

                // 清理
                // playerEntity.DeleteEntity(); // TODO: 找到Friflo ECS的正确删除方法
                ecsWorld.Destroy();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System Integration Test Failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试结果
        /// </summary>
        public class TestResult
        {
            public int TotalTests;
            public int PassedTests;
            public bool EntityCreationTest;
            public bool QuerySystemTest;
            public bool BatchOperationTest;
            public bool EntityDeletionTest;
            public bool MemoryManagementTest;
            public bool SystemIntegrationTest;

            public void PrintResults()
            {
                Console.WriteLine($"=== Friflo ECS兼容性测试结果 ===");
                Console.WriteLine($"总测试数: {TotalTests}");
                Console.WriteLine($"通过测试: {PassedTests}");
                Console.WriteLine($"失败测试: {TotalTests - PassedTests}");
                Console.WriteLine($"成功率: {(double)PassedTests / TotalTests * 100:F1}%");
                Console.WriteLine();
                Console.WriteLine($"详细结果:");
                Console.WriteLine($"- 实体创建测试: {(EntityCreationTest ? "通过" : "失败")}");
                Console.WriteLine($"- QuerySystem测试: {(QuerySystemTest ? "通过" : "失败")}");
                Console.WriteLine($"- 批量操作测试: {(BatchOperationTest ? "通过" : "失败")}");
                Console.WriteLine($"- 实体删除测试: {(EntityDeletionTest ? "通过" : "失败")}");
                Console.WriteLine($"- 内存管理测试: {(MemoryManagementTest ? "通过" : "失败")}");
                Console.WriteLine($"- 系统集成测试: {(SystemIntegrationTest ? "通过" : "失败")}");
            }
        }

        /// <summary>
        /// 测试用的QuerySystem
        /// </summary>
        private class CompatibilityTestQuerySystem : QuerySystem<Block, Position>
        {
            public bool Executed { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                
                // 测试ForEachEntity - 由于Friflo ECS限制，使用传统遍历
                var count = 0;
                foreach (var entity in Query.Entities)
                {
                    var block = entity.GetComponent<Block>();
                    var position = entity.GetComponent<Position>();
                    count++;
                }

                if (count == 0)
                {
                    throw new Exception("Query did not execute");
                }
            }
        }
    }
}