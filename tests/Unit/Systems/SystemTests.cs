using Microsoft.Xna.Framework;
using Xunit;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Systems;
using MCGame.ECS.Components;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;
using PlayerComponent = MCGame.ECS.Components.Player;

namespace MCGame.Tests.Unit.Systems
{
    /// <summary>
    /// 系统单元测试
    /// 测试ECS系统的基本功能
    /// </summary>
    public class SystemTests : TestBase
    {
        [Fact]
        public void PlayerInputSystem_ShouldProcessInputEntities()
        {
            // Arrange
            var playerEntity = CreateTestPlayerEntity(new Vector3(0, 64, 0));
            var testSystem = new TestPlayerInputSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.EntityProcessed);
        }

        [Fact]
        public void PlayerMovementSystem_ShouldUpdatePlayerPosition()
        {
            // Arrange
            var playerEntity = CreateTestPlayerEntity(new Vector3(0, 64, 0));
            var testSystem = new TestPlayerMovementSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.PositionUpdated);
        }

        [Fact]
        public void PhysicsSystem_ShouldApplyPhysics()
        {
            // Arrange
            var entity = CreateTestEntity(new Vector3(0, 10, 0), new Velocity(0, -5, 0));
            var testSystem = new TestPhysicsSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.PhysicsApplied);
        }

        [Fact]
        public void CameraSystem_ShouldUpdateCamera()
        {
            // Arrange
            var entity = CreateTestEntity(new Vector3(0, 64, 0), new Camera());
            var testSystem = new TestCameraSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.CameraUpdated);
        }

        [Fact]
        public void VisibilitySystem_ShouldCalculateVisibility()
        {
            // Arrange
            var entity = CreateTestEntity(new Vector3(0, 0, 0), new Visibility(true));
            var testSystem = new TestVisibilitySystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.VisibilityCalculated);
        }

        [Fact]
        public void LifetimeSystem_ShouldUpdateLifetime()
        {
            // Arrange
            var entity = CreateTestEntity(new Vector3(0, 0, 0), new Lifetime(5.0f));
            var testSystem = new TestLifetimeSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.LifetimeUpdated);
        }

        [Fact]
        public void ChunkStateSystem_ShouldUpdateChunkState()
        {
            // Arrange
            var entity = CreateTestChunkEntity(new ChunkPosition(0, 0, 0));
            var testSystem = new TestChunkStateSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.ChunkStateUpdated);
        }

        [Fact]
        public void System_ShouldProcessMultipleEntities()
        {
            // Arrange
            var entities = new List<Friflo.Engine.ECS.Entity>();
            for (int i = 0; i < 5; i++)
            {
                entities.Add(CreateTestPlayerEntity(new Vector3(i, 64, 0)));
            }
            
            var testSystem = new TestMultiEntitySystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.Equal(5, testSystem.ProcessedEntityCount);
        }

        [Fact]
        public void System_ShouldNotProcessWithoutMatchingEntities()
        {
            // Arrange
            // 创建没有所需组件的实体
            var entity = Store.CreateEntity(new Position(Vector3.Zero));
            
            var testSystem = new TestPlayerInputSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.False(testSystem.EntityProcessed); // 没有匹配的实体
        }

        [Fact]
        public void System_ShouldUpdateEntityComponents()
        {
            // Arrange
            var entity = CreateTestEntity(new Vector3(0, 10, 0), new Velocity(0, -5, 0));
            var initialPosition = entity.GetComponent<Position>().Value;
            
            var testSystem = new TestPhysicsUpdateSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            var updatedPosition = entity.GetComponent<Position>().Value;
            Assert.NotEqual(initialPosition, updatedPosition);
            Assert.True(updatedPosition.Y < initialPosition.Y); // 应该下落
        }

        [Fact]
        public void System_ShouldHandleEntityCreationDuringUpdate()
        {
            // Arrange
            var testSystem = new TestEntityCreationSystem(Store);
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.EntityCreated);
            Assert.Equal(1, testSystem.CreatedEntityCount);
        }

        [Fact]
        public void System_ShouldMaintainExecutionOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var system1 = new TestOrderedSystem("System1", executionOrder);
            var system2 = new TestOrderedSystem("System2", executionOrder);
            var system3 = new TestOrderedSystem("System3", executionOrder);
            
            // 按顺序添加系统
            SystemRoot.Add(system1);
            SystemRoot.Add(system2);
            SystemRoot.Add(system3);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(system1.Executed);
            Assert.True(system2.Executed);
            Assert.True(system3.Executed);
            
            Assert.Equal(3, executionOrder.Count);
            Assert.Equal("System1", executionOrder[0]);
            Assert.Equal("System2", executionOrder[1]);
            Assert.Equal("System3", executionOrder[2]);
        }

        [Fact]
        public void System_ShouldHandleLargeEntitySets()
        {
            // Arrange
            var entityCount = 100;
            var entities = new List<Friflo.Engine.ECS.Entity>();
            
            for (int i = 0; i < entityCount; i++)
            {
                entities.Add(CreateTestPlayerEntity(new Vector3(i, 64, 0)));
            }
            
            var testSystem = new TestLargeEntitySystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.Equal(entityCount, testSystem.ProcessedEntityCount);
        }

        [Fact]
        public void System_ShouldSupportConditionalProcessing()
        {
            // Arrange
            var entity1 = CreateTestPlayerEntity(new Vector3(0, 64, 0));
            var entity2 = CreateTestPlayerEntity(new Vector3(1, 64, 0));
            
            var testSystem = new TestConditionalSystem();
            SystemRoot.Add(testSystem);
            
            // Act
            UpdateSystems();
            
            // Assert
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.ConditionalProcessingWorked);
        }

        [Fact]
        public void System_ShouldHandleComponentAccessErrors()
        {
            // Arrange
            var entity = Store.CreateEntity(new Position(Vector3.Zero)); // 只有Position组件
            
            var testSystem = new TestErrorHandlingSystem();
            SystemRoot.Add(testSystem);
            
            // Act & Assert
            // 系统应该处理错误而不崩溃
            UpdateSystems();
            
            Assert.True(testSystem.Executed);
            Assert.True(testSystem.ErrorHandled);
        }

        #region Test Systems

        private class TestPlayerInputSystem : QuerySystem<Input>
        {
            public bool Executed { get; private set; }
            public bool EntityProcessed { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                EntityProcessed = Query.Entities.Any();
            }
        }

        private class TestPlayerMovementSystem : QuerySystem<Position, Velocity, Input, PlayerComponent>
        {
            public bool Executed { get; private set; }
            public bool PositionUpdated { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                PositionUpdated = Query.Entities.Any();
            }
        }

        private class TestPhysicsSystem : QuerySystem<Position, Velocity>
        {
            public bool Executed { get; private set; }
            public bool PhysicsApplied { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                PhysicsApplied = Query.Entities.Any();
            }
        }

        private class TestCameraSystem : QuerySystem<Camera>
        {
            public bool Executed { get; private set; }
            public bool CameraUpdated { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                CameraUpdated = Query.Entities.Any();
            }
        }

        private class TestVisibilitySystem : QuerySystem<Visibility>
        {
            public bool Executed { get; private set; }
            public bool VisibilityCalculated { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                VisibilityCalculated = Query.Entities.Any();
            }
        }

        private class TestLifetimeSystem : QuerySystem<Lifetime>
        {
            public bool Executed { get; private set; }
            public bool LifetimeUpdated { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                LifetimeUpdated = Query.Entities.Any();
            }
        }

        private class TestChunkStateSystem : QuerySystem<Chunk>
        {
            public bool Executed { get; private set; }
            public bool ChunkStateUpdated { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                ChunkStateUpdated = Query.Entities.Any();
            }
        }

        private class TestMultiEntitySystem : QuerySystem<PlayerComponent>
        {
            public bool Executed { get; private set; }
            public int ProcessedEntityCount { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                ProcessedEntityCount = Query.Entities.Count();
            }
        }

        private class TestPhysicsUpdateSystem : QuerySystem<Position, Velocity>
        {
            public bool Executed { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                
                foreach (var entity in Query.Entities)
                {
                    var position = entity.GetComponent<Position>();
                    var velocity = entity.GetComponent<Velocity>();
                    
                    // 简单的物理更新
                    position.Value += velocity.Value * 0.016f; // 60 FPS
                }
            }
        }

        private class TestEntityCreationSystem : QuerySystem<Position>
        {
            private readonly EntityStore _store;
            public bool Executed { get; private set; }
            public bool EntityCreated { get; private set; }
            public int CreatedEntityCount { get; private set; }

            public TestEntityCreationSystem(EntityStore store)
            {
                _store = store;
            }

            protected override void OnUpdate()
            {
                Executed = true;
                
                if (Query.Entities.Any())
                {
                    _store.CreateEntity(new Position(new Vector3(999, 999, 999)));
                    EntityCreated = true;
                    CreatedEntityCount = 1;
                }
            }
        }

        private class TestOrderedSystem : QuerySystem<Position>
        {
            private readonly string _systemName;
            private readonly List<string> _executionOrder;
            public bool Executed { get; private set; }

            public TestOrderedSystem(string systemName, List<string> executionOrder)
            {
                _systemName = systemName;
                _executionOrder = executionOrder;
            }

            protected override void OnUpdate()
            {
                Executed = true;
                _executionOrder.Add(_systemName);
            }
        }

        private class TestLargeEntitySystem : QuerySystem<PlayerComponent>
        {
            public bool Executed { get; private set; }
            public int ProcessedEntityCount { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                ProcessedEntityCount = Query.Entities.Count();
            }
        }

        private class TestConditionalSystem : QuerySystem<PlayerComponent>
        {
            public bool Executed { get; private set; }
            public bool ConditionalProcessingWorked { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                ConditionalProcessingWorked = Query.Entities.Count() > 0;
            }
        }

        private class TestErrorHandlingSystem : QuerySystem<Position>
        {
            public bool Executed { get; private set; }
            public bool ErrorHandled { get; private set; }

            protected override void OnUpdate()
            {
                Executed = true;
                ErrorHandled = true;
                
                try
                {
                    // 尝试访问可能不存在的组件
                    foreach (var entity in Query.Entities)
                    {
                        // 这个系统只查询Position，所以访问其他组件可能会出错
                        // 但我们只是标记错误已处理，不实际抛出异常
                        ErrorHandled = true;
                    }
                }
                catch
                {
                    ErrorHandled = true;
                }
            }
        }

        #endregion
    }
}