using Friflo.Engine.ECS;
using MCGame.ECS.Components;

namespace MCGame.ECS.Tests
{
    /// <summary>
    /// 简单的ECS功能测试
    /// 验证ECS系统是否正常工作
    /// </summary>
    public static class SimpleECSValidation
    {
        public static void RunTest()
        {
            Console.WriteLine("=== ECS功能验证测试 ===");
            
            try
            {
                // 创建EntityStore
                var store = new EntityStore();
                Console.WriteLine("✅ EntityStore创建成功");
                
                // 创建测试实体
                var entity = store.CreateEntity();
                Console.WriteLine("✅ 实体创建成功");
                
                // 添加组件
                entity.AddComponent(new MCGame.ECS.Components.Position { Value = new System.Numerics.Vector3(0, 0, 0) });
                entity.AddComponent(new Block { Type = (MCGame.Core.BlockType)1 });
                Console.WriteLine("✅ 组件添加成功");
                
                // 查询组件
                var positionQuery = store.Query<MCGame.ECS.Components.Position>();
                var positionCount = positionQuery.Entities.Count;
                Console.WriteLine($"✅ 位置组件查询成功，实体数量: {positionCount}");
                
                // 验证组件值
                foreach (var posEntity in positionQuery.Entities)
                {
                    var pos = posEntity.GetComponent<MCGame.ECS.Components.Position>();
                    Console.WriteLine($"📍 实体位置: {pos.Value}");
                }
                
                Console.WriteLine("🎉 ECS功能验证测试通过！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ECS功能验证测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex.StackTrace}");
            }
        }
    }
}