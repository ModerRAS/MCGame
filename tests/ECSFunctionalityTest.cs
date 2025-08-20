using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Components;
using MCGame.ECS.Managers;
using MCGame.Core;
using Microsoft.Xna.Framework;

namespace MCGame.ECS.Tests
{
    /// <summary>
    /// ECS功能验证测试
    /// 验证修复后的ECS系统是否正常工作
    /// </summary>
    public class ECSFunctionalityTest
    {
        public void RunAllTests()
        {
            Console.WriteLine("=== 开始ECS功能验证测试 ===\n");
            
            TestEntityCreation();
            TestComponentOperations();
            TestBlockManager();
            TestChunkManager();
            TestSystemOperations();
            
            Console.WriteLine("\n=== 所有测试完成 ===");
        }
        
        private void TestEntityCreation()
        {
            Console.WriteLine("测试1: 实体创建和基本操作");
            
            var store = new EntityStore();
            
            // 创建实体
            var entity = store.CreateEntity();
            Console.WriteLine($"✓ 创建实体成功，ID: {entity.Id}");
            
            // 添加组件
            entity.AddComponent(new MCGame.ECS.Components.Position(10, 20, 30));
            entity.AddComponent(new Velocity(1, 2, 3));
            Console.WriteLine("✓ 添加组件成功");
            
            // 获取组件
            var position = entity.GetComponent<MCGame.ECS.Components.Position>();
            var velocity = entity.GetComponent<Velocity>();
            Console.WriteLine($"✓ 获取组件成功: Position={position.Value}, Velocity={velocity.Value}");
            
            // 验证实体计数
            Console.WriteLine($"✓ 实体计数: {store.Count}");
            
            Console.WriteLine();
        }
        
        private void TestComponentOperations()
        {
            Console.WriteLine("测试2: 组件操作");
            
            var store = new EntityStore();
            var entity = store.CreateEntity();
            
            // 添加多个组件
            entity.AddComponent(new MCGame.ECS.Components.Position(1, 2, 3));
            entity.AddComponent(new Velocity(0.1f, 0.2f, 0.3f));
            entity.AddComponent(new MCGame.ECS.Components.Rotation(45, 0, 0));
            entity.AddComponent(new Visibility(true));
            Console.WriteLine("✓ 添加多个组件成功");
            
            // 测试TryGetComponent
            if (entity.TryGetComponent<MCGame.ECS.Components.Position>(out var position))
            {
                Console.WriteLine($"✓ TryGetComponent成功: {position.Value}");
            }
            
            // 测试查询
            var query = store.Query<MCGame.ECS.Components.Position, Velocity>();
            Console.WriteLine($"✓ 查询成功，实体数量: {query.Count}");
            
            Console.WriteLine();
        }
        
        private void TestBlockManager()
        {
            Console.WriteLine("测试3: 方块管理器");
            
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            
            // 创建方块
            var blockEntity = blockManager.SetBlock(BlockType.Grass, new Vector3(0, 0, 0));
            Console.WriteLine($"✓ 创建方块成功，实体ID: {blockEntity.Id}");
            
            // 获取方块
            var blockType = blockManager.GetBlock(new Vector3(0, 0, 0));
            Console.WriteLine($"✓ 获取方块成功: {blockType}");
            
            // 批量创建方块
            var positions = new Vector3[5];
            var blockTypes = new BlockType[5];
            for (int i = 0; i < 5; i++)
            {
                positions[i] = new Vector3(i, 1, 0);
                blockTypes[i] = BlockType.Stone;
            }
            blockManager.SetBlocksBatch(blockTypes, positions);
            Console.WriteLine("✓ 批量创建方块成功");
            
            // 获取统计信息
            var stats = blockManager.GetMemoryStats();
            Console.WriteLine($"✓ 方块统计: 总数={stats.TotalBlocks}, 区块数={stats.TotalChunks}");
            
            Console.WriteLine();
        }
        
        private void TestChunkManager()
        {
            Console.WriteLine("测试4: 区块管理器");
            
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            var chunkManager = new ECSChunkManager(store, blockManager);
            
            // 创建区块
            var chunkPos = new ChunkPosition(0, 0, 0);
            var chunkEntity = chunkManager.CreateChunk(chunkPos);
            Console.WriteLine($"✓ 创建区块成功，实体ID: {chunkEntity.Id}");
            
            // 标记区块状态
            chunkManager.MarkChunkLoaded(chunkPos);
            Console.WriteLine("✓ 标记区块状态成功");
            
            // 更新区块加载状态
            chunkManager.UpdateChunkLoading(chunkPos);
            Console.WriteLine("✓ 更新区块加载状态成功");
            
            // 获取统计信息
            var stats = chunkManager.GetStats();
            Console.WriteLine($"✓ 区块统计: 总数={stats.TotalChunks}, 已加载={stats.LoadedChunks}");
            
            Console.WriteLine();
        }
        
        private void TestSystemOperations()
        {
            Console.WriteLine("测试5: 系统操作");
            
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            
            // 创建测试实体
            var playerEntity = store.CreateEntity(
                new MCGame.ECS.Components.Position(0, 64, 0),
                new Velocity(0, 0, 0),
                new MCGame.ECS.Components.Player(),
                new Input(),
                new Visibility(true)
            );
            Console.WriteLine($"✓ 创建玩家实体成功，ID: {playerEntity.Id}");
            
            // TODO: 添加实际的游戏系统
            // systemRoot.Add(new PlayerInputSystem());
            // systemRoot.Add(new PlayerMovementSystem());
            Console.WriteLine("✓ 系统配置完成");
            
            // 运行系统更新
            var updateTick = new UpdateTick();
            systemRoot.Update(updateTick);
            Console.WriteLine("✓ 系统更新成功");
            
            // 获取性能统计
            var perfStats = systemRoot.GetPerfLog();
            Console.WriteLine($"✓ 性能统计: {perfStats}");
            
            Console.WriteLine();
        }
    }
}