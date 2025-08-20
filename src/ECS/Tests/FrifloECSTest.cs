using Friflo.Engine.ECS;
using MCGame.ECS.Components;
using MCGame.Blocks;
using MCGame.Core;
using Microsoft.Xna.Framework;

namespace MCGame.ECS.Tests
{
    /// <summary>
    /// Friflo ECS API测试
    /// 用于验证正确的API使用方式
    /// </summary>
    public class FrifloECSTest
    {
        public void TestEntityLifecycle()
        {
            var store = new EntityStore();
            
            // 创建实体
            var entity = store.CreateEntity(
                new Block(BlockType.Grass),
                new MCGame.ECS.Components.Position(1, 2, 3),
                new Visibility(true)
            );
            
            // 测试实体删除方法
            TestEntityDeletion(entity, store);
        }
        
        private void TestEntityDeletion(Entity entity, EntityStore store)
        {
            // 尝试不同的删除方法
            try
            {
                // 方法1: 使用Delete方法（如果存在）
                // entity.Delete();
                
                // 方法2: 使用Store的删除方法
                // store.DeleteEntity(entity.Id);
                
                // 方法3: 使用Archetype删除
                // entity.Archetype.DeleteEntity(entity);
                
                // 方法4: 使用Dispose方法（如果存在）
                // entity.Dispose();
                
                // 注意：根据Friflo ECS文档，可能需要使用特定的删除方法
                // 目前先注释掉，等待找到正确的API
            }
            catch (System.Exception ex)
            {
                // 记录错误，找到正确的删除方法
                System.Console.WriteLine($"Entity deletion failed: {ex.Message}");
            }
        }
        
        public void TestQueryIteration()
        {
            var store = new EntityStore();
            
            // 创建测试实体
            for (int i = 0; i < 10; i++)
            {
                store.CreateEntity(
                    new Block(BlockType.Grass),
                    new MCGame.ECS.Components.Position(i, i, i),
                    new Visibility(true)
                );
            }
            
            // 测试查询遍历
            var query = store.Query<Block, MCGame.ECS.Components.Position, Visibility>();
            
            // 正确的遍历方式
            foreach (var entity in query.Entities)
            {
                var block = entity.GetComponent<Block>();
                var position = entity.GetComponent<MCGame.ECS.Components.Position>();
                var visibility = entity.GetComponent<Visibility>();
                
                System.Console.WriteLine($"Block at {position.Value} is visible: {visibility.IsVisible}");
            }
        }
    }
}