using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Components;
using Microsoft.Xna.Framework;

namespace MCGame.SimpleTest
{
    /// <summary>
    /// 简化的Friflo ECS API测试
    /// </summary>
    public class SimpleTest
    {
        public static void RunBasicTests()
        {
            Console.WriteLine("开始运行Friflo ECS API基本测试...");
            
            var testResults = new TestResult();
            
            try
            {
                // 1. 基础实体创建测试
                TestEntityCreation(testResults);
                
                // 2. 组件操作测试
                TestComponentOperations(testResults);
                
                // 3. 查询系统测试
                TestQuerySystem(testResults);
                
                // 4. 系统执行测试
                TestSystemExecution(testResults);
                
                // 5. 性能测试
                TestPerformance(testResults);
                
                testResults.PrintResults();
                
                if (testResults.PassedTests == testResults.TotalTests)
                {
                    Console.WriteLine("\n✅ 所有基本测试通过！");
                }
                else
                {
                    Console.WriteLine($"\n❌ {testResults.FailedTests} 个测试失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试运行出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private static void TestEntityCreation(TestResult results)
        {
            Console.WriteLine("\n--- 测试1: 基础实体创建 ---");
            
            try
            {
                var store = new EntityStore();
                
                // 创建实体
                var entity = store.CreateEntity();
                Assert.True(entity.Id > 0, "实体ID应该大于0");
                
                // 创建带组件的实体
                var entity2 = store.CreateEntity();
                entity2.AddComponent(new MCGame.ECS.Components.Position(new Vector3(1, 2, 3)));
                
                var position = entity2.GetComponent<MCGame.ECS.Components.Position>();
                Assert.Equal(1, position.Value.X);
                Assert.Equal(2, position.Value.Y);
                Assert.Equal(3, position.Value.Z);
                
                results.AddPassed("实体创建");
                Console.WriteLine($"✓ 成功创建 {store.Count} 个实体");
            }
            catch (Exception ex)
            {
                results.AddFailed("实体创建", ex.Message);
                Console.WriteLine($"✗ 实体创建失败: {ex.Message}");
            }
        }
        
        private static void TestComponentOperations(TestResult results)
        {
            Console.WriteLine("\n--- 测试2: 组件操作 ---");
            
            try
            {
                var store = new EntityStore();
                var entity = store.CreateEntity();
                
                // 添加组件
                entity.AddComponent(new MCGame.ECS.Components.Position(new Vector3(10, 20, 30)));
                entity.AddComponent(new MCGame.ECS.Components.Velocity(new Vector3(1, 2, 3)));
                
                Assert.True(entity.HasComponent<MCGame.ECS.Components.Position>(), "应该有Position组件");
                Assert.True(entity.HasComponent<MCGame.ECS.Components.Velocity>(), "应该有Velocity组件");
                
                // 获取组件
                var position = entity.GetComponent<MCGame.ECS.Components.Position>();
                var velocity = entity.GetComponent<MCGame.ECS.Components.Velocity>();
                
                Assert.Equal(10, position.Value.X);
                Assert.Equal(1, velocity.Value.X);
                
                // 修改组件
                position.Value = new Vector3(15, 25, 35);
                Assert.Equal(15, position.Value.X);
                
                // 尝试获取不存在的组件
                Assert.False(entity.HasComponent<MCGame.ECS.Components.Block>(), "不应该有Block组件");
                
                results.AddPassed("组件操作");
                Console.WriteLine($"✓ 成功操作组件，实体共有 {entity.Components.Count} 个组件");
            }
            catch (Exception ex)
            {
                results.AddFailed("组件操作", ex.Message);
                Console.WriteLine($"✗ 组件操作失败: {ex.Message}");
            }
        }
        
        private static void TestQuerySystem(TestResult results)
        {
            Console.WriteLine("\n--- 测试3: 查询系统 ---");
            
            try
            {
                var store = new EntityStore();
                
                // 创建测试实体
                for (int i = 0; i < 10; i++)
                {
                    store.CreateEntity(new MCGame.ECS.Components.Position(new Vector3(i, 0, 0)), new MCGame.ECS.Components.Velocity(0, -1, 0));
                }
                
                // 创建一些只有Position的实体
                for (int i = 0; i < 5; i++)
                {
                    store.CreateEntity(new MCGame.ECS.Components.Position(new Vector3(i, 10, 0)));
                }
                
                // 查询所有有Position的实体
                var positionQuery = store.Query<MCGame.ECS.Components.Position>();
                Assert.Equal(15, positionQuery.Entities.Count);
                
                // 查询同时有Position和Velocity的实体
                var positionVelocityQuery = store.Query<MCGame.ECS.Components.Position, MCGame.ECS.Components.Velocity>();
                Assert.Equal(10, positionVelocityQuery.Entities.Count);
                
                // 遍历查询结果
                int count = 0;
                foreach (var entity in positionVelocityQuery.Entities)
                {
                    Assert.True(entity.HasComponent<MCGame.ECS.Components.Position>());
                    Assert.True(entity.HasComponent<MCGame.ECS.Components.Velocity>());
                    count++;
                }
                Assert.Equal(10, count);
                
                results.AddPassed("查询系统");
                Console.WriteLine($"✓ 查询成功：Position实体 {positionQuery.Entities.Count} 个，Position+Velocity实体 {positionVelocityQuery.Entities.Count} 个");
            }
            catch (Exception ex)
            {
                results.AddFailed("查询系统", ex.Message);
                Console.WriteLine($"✗ 查询系统失败: {ex.Message}");
            }
        }
        
        private static void TestSystemExecution(TestResult results)
        {
            Console.WriteLine("\n--- 测试4: 系统执行 ---");
            
            try
            {
                var store = new EntityStore();
                var systemRoot = new SystemRoot(store);
                
                // 创建测试实体
                for (int i = 0; i < 5; i++)
                {
                    store.CreateEntity(new MCGame.ECS.Components.Position(new Vector3(i, 0, 0)), new MCGame.ECS.Components.Velocity(0, -1, 0));
                }
                
                // 创建测试系统
                var testSystem = new TestPhysicsSystem();
                systemRoot.Add(testSystem);
                
                // 执行系统
                systemRoot.Update(new UpdateTick());
                
                Assert.True(testSystem.Executed, "系统应该被执行");
                Assert.Equal(5, testSystem.ProcessedCount);
                
                results.AddPassed("系统执行");
                Console.WriteLine($"✓ 系统执行成功，处理了 {testSystem.ProcessedCount} 个实体");
            }
            catch (Exception ex)
            {
                results.AddFailed("系统执行", ex.Message);
                Console.WriteLine($"✗ 系统执行失败: {ex.Message}");
            }
        }
        
        private static void TestPerformance(TestResult results)
        {
            Console.WriteLine("\n--- 测试5: 性能测试 ---");
            
            try
            {
                var store = new EntityStore();
                var stopwatch = Stopwatch.StartNew();
                
                // 创建大量实体
                const int entityCount = 1000;
                for (int i = 0; i < entityCount; i++)
                {
                    store.CreateEntity(
                        new MCGame.ECS.Components.Position(new Vector3(i, 0, 0)),
                        new MCGame.ECS.Components.Velocity(0, -1, 0),
                        new MCGame.ECS.Components.Block(MCGame.Core.BlockType.Grass),
                        new MCGame.ECS.Components.Visibility(true)
                    );
                }
                
                stopwatch.Stop();
                var creationTime = stopwatch.ElapsedMilliseconds;
                
                // 测试查询性能
                stopwatch.Restart();
                var query = store.Query<MCGame.ECS.Components.Position, MCGame.ECS.Components.Velocity, MCGame.ECS.Components.Block>();
                var queryCount = query.Entities.Count;
                stopwatch.Stop();
                var queryTime = stopwatch.ElapsedMilliseconds;
                
                // 测试系统性能
                var systemRoot = new SystemRoot(store);
                var testSystem = new TestPhysicsSystem();
                systemRoot.Add(testSystem);
                
                stopwatch.Restart();
                systemRoot.Update(new UpdateTick());
                stopwatch.Stop();
                var systemTime = stopwatch.ElapsedMilliseconds;
                
                Assert.Equal(entityCount, store.Count);
                Assert.Equal(entityCount, queryCount);
                Assert.True(testSystem.Executed);
                Assert.Equal(entityCount, testSystem.ProcessedCount);
                
                results.AddPassed("性能测试");
                Console.WriteLine($"✓ 性能测试通过：");
                Console.WriteLine($"  - 创建 {entityCount} 个实体耗时: {creationTime}ms");
                Console.WriteLine($"  - 查询 {queryCount} 个实体耗时: {queryTime}ms");
                Console.WriteLine($"  - 系统更新耗时: {systemTime}ms");
                Console.WriteLine($"  - 实体总数: {store.Count}");
            }
            catch (Exception ex)
            {
                results.AddFailed("性能测试", ex.Message);
                Console.WriteLine($"✗ 性能测试失败: {ex.Message}");
            }
        }
        
        public static void Main(string[] args)
        {
            Console.WriteLine("MCGame Friflo ECS API 简化测试");
            Console.WriteLine("===================================");
            
            RunBasicTests();
            
            Console.WriteLine("\n测试完成！");
        }
    }
    
    /// <summary>
    /// 测试结果统计
    /// </summary>
    public class TestResult
    {
        public int TotalTests { get; private set; }
        public int PassedTests { get; private set; }
        public int FailedTests { get; private set; }
        private readonly List<string> failedTests = new List<string>();
        
        public void AddPassed(string testName)
        {
            TotalTests++;
            PassedTests++;
            Console.WriteLine($"✓ {testName}");
        }
        
        public void AddFailed(string testName, string errorMessage)
        {
            TotalTests++;
            FailedTests++;
            failedTests.Add($"{testName}: {errorMessage}");
            Console.WriteLine($"✗ {testName}: {errorMessage}");
        }
        
        public void PrintResults()
        {
            Console.WriteLine("\n测试结果汇总:");
            Console.WriteLine($"总测试数: {TotalTests}");
            Console.WriteLine($"通过: {PassedTests}");
            Console.WriteLine($"失败: {FailedTests}");
            Console.WriteLine($"成功率: {(double)PassedTests / TotalTests * 100:F1}%");
            
            if (FailedTests > 0)
            {
                Console.WriteLine("\n失败的测试:");
                foreach (var failedTest in failedTests)
                {
                    Console.WriteLine($"  - {failedTest}");
                }
            }
        }
    }
    
    /// <summary>
    /// 测试用的物理系统
    /// </summary>
    public class TestPhysicsSystem : QuerySystem<MCGame.ECS.Components.Position, MCGame.ECS.Components.Velocity>
    {
        public bool Executed { get; private set; }
        public int ProcessedCount { get; private set; }
        
        protected override void OnUpdate()
        {
            Executed = true;
            ProcessedCount = Query.Entities.Count;
            
            // 简单的物理更新
            foreach (var entity in Query.Entities)
            {
                var position = entity.GetComponent<MCGame.ECS.Components.Position>();
                var velocity = entity.GetComponent<MCGame.ECS.Components.Velocity>();
                
                // 更新位置
                position.Value += velocity.Value * 0.016f; // 60 FPS
            }
        }
    }
}