using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace MCGame.Tests.Unit
{
    /// <summary>
    /// 单元测试基类
    /// 提供通用的测试设置和清理功能
    /// </summary>
    public class TestBase
    {
        protected EntityStore Store { get; private set; }
        protected SystemRoot SystemRoot { get; private set; }

        public TestBase()
        {
            Setup();
        }

        /// <summary>
        /// 测试设置
        /// </summary>
        protected virtual void Setup()
        {
            Store = new EntityStore();
            SystemRoot = new SystemRoot(Store);
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        protected virtual void Cleanup()
        {
            // 清理所有实体
            // 注意：Friflo ECS可能需要特定的清理方法
            // 目前让GC处理，后续根据实际API调整
            Store = null;
            SystemRoot = null;
        }

        /// <summary>
        /// 创建测试实体
        /// </summary>
        protected Entity CreateTestEntity(Vector3 position, params IComponent[] components)
        {
            var allComponents = new List<IComponent>
            {
                new MCGame.ECS.Components.Position(position)
            };

            allComponents.AddRange(components);
            
            return Store.CreateEntity(allComponents.ToArray());
        }

        /// <summary>
        /// 创建测试方块实体
        /// </summary>
        protected Entity CreateTestBlockEntity(MCGame.Core.BlockType blockType, Vector3 position)
        {
            return Store.CreateEntity(
                new MCGame.ECS.Components.Block(blockType),
                new MCGame.ECS.Components.Position(position),
                new MCGame.ECS.Components.Visibility(true),
                new MCGame.ECS.Components.Collider(new BoundingBox(position, position + Vector3.One)),
                new MCGame.ECS.Components.Lighting(15)
            );
        }

        /// <summary>
        /// 创建测试玩家实体
        /// </summary>
        protected Entity CreateTestPlayerEntity(Vector3 position)
        {
            return Store.CreateEntity(
                new MCGame.ECS.Components.Position(position),
                new MCGame.ECS.Components.Rotation(0, 0, 0),
                new MCGame.ECS.Components.Velocity(0, 0, 0),
                new MCGame.ECS.Components.Player(),
                new MCGame.ECS.Components.Input(),
                new MCGame.ECS.Components.Visibility(true)
            );
        }

        /// <summary>
        /// 创建测试区块实体
        /// </summary>
        protected Entity CreateTestChunkEntity(MCGame.Core.ChunkPosition position)
        {
            var worldPos = position.ToWorldPosition(16);
            var bounds = new BoundingBox(worldPos, worldPos + new Vector3(16, 256, 16));

            return Store.CreateEntity(
                new MCGame.ECS.Components.Chunk(position),
                new MCGame.ECS.Components.Position(worldPos),
                new MCGame.ECS.Components.Mesh(bounds),
                new MCGame.ECS.Components.Visibility(true),
                new MCGame.ECS.Components.Collider(bounds, false)
            );
        }

        /// <summary>
        /// 验证实体具有指定组件
        /// </summary>
        protected void AssertEntityHasComponent<T>(Entity entity) where T : struct, IComponent
        {
            Assert.True(entity.HasComponent<T>(), $"Entity should have component {typeof(T).Name}");
        }

        /// <summary>
        /// 验证实体不具有指定组件
        /// </summary>
        protected void AssertEntityDoesNotHaveComponent<T>(Entity entity) where T : struct, IComponent
        {
            Assert.False(entity.HasComponent<T>(), $"Entity should not have component {typeof(T).Name}");
        }

        /// <summary>
        /// 验证组件值
        /// </summary>
        protected void AssertComponentValue<T>(Entity entity, T expected) where T : struct, IComponent
        {
            var actual = entity.GetComponent<T>();
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// 验证查询结果数量
        /// </summary>
        protected void AssertQueryCount<T>(ArchetypeQuery query, int expectedCount)
        {
            Assert.Equal(expectedCount, query.Count);
        }

        /// <summary>
        /// 运行系统更新
        /// </summary>
        protected void UpdateSystems()
        {
            SystemRoot.Update(new UpdateTick());
        }
    }
}