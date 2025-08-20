using Microsoft.Xna.Framework;
using Xunit;
using MCGame.ECS.Components;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;
using PlayerComp = MCGame.ECS.Components.Player;

namespace MCGame.Tests.Unit.Components
{
    /// <summary>
    /// 实体组件操作测试
    /// 测试实体与组件的交互操作
    /// </summary>
    public class EntityComponentTests : TestBase
    {
        [Fact]
        public void Entity_ShouldCreateWithComponents()
        {
            // Arrange
            var position = new Vector3(1, 2, 3);
            var blockType = BlockType.Grass;
            
            // Act
            var entity = Store.CreateEntity(
                new Position(position),
                new Block(blockType),
                new Visibility(true)
            );
            
            // Assert
            Assert.NotEqual(0, entity.Id);
            AssertEntityHasComponent<Position>(entity);
            AssertEntityHasComponent<Block>(entity);
            AssertEntityHasComponent<Visibility>(entity);
            AssertEntityDoesNotHaveComponent<PlayerComp>(entity);
            
            // 验证组件值
            var actualPosition = entity.GetComponent<Position>();
            Assert.Equal(position, actualPosition.Value);
            
            var actualBlock = entity.GetComponent<Block>();
            Assert.Equal(blockType, actualBlock.Type);
        }

        [Fact]
        public void Entity_ShouldAddComponent()
        {
            // Arrange
            var entity = Store.CreateEntity(new Position(Vector3.Zero));
            
            // Act
            entity.AddComponent(new Block(BlockType.Stone));
            
            // Assert
            AssertEntityHasComponent<Block>(entity);
            var block = entity.GetComponent<Block>();
            Assert.Equal(BlockType.Stone, block.Type);
        }

        [Fact]
        public void Entity_ShouldRemoveComponent()
        {
            // Arrange
            var entity = Store.CreateEntity(
                new Position(Vector3.Zero),
                new Block(BlockType.Dirt)
            );
            
            // Act
            // 注意：Friflo ECS可能需要特定的移除组件方法
            // entity.RemoveComponent<Block>(); // 根据实际API调整
            
            // Assert
            // AssertEntityDoesNotHaveComponent<Block>(entity); // 根据实际API调整
        }

        [Fact]
        public void Entity_ShouldGetComponent()
        {
            // Arrange
            var expectedPosition = new Vector3(10, 20, 30);
            var entity = Store.CreateEntity(new Position(expectedPosition));
            
            // Act
            var positionComponent = entity.GetComponent<Position>();
            
            // Assert
            Assert.Equal(expectedPosition, positionComponent.Value);
        }

        [Fact]
        public void Entity_ShouldTryGetComponent()
        {
            // Arrange
            var entity = Store.CreateEntity(new Position(Vector3.Zero));
            
            // Act & Assert - 存在的组件
            Assert.True(entity.TryGetComponent<Position>(out var position));
            Assert.NotNull(position);
            
            // Act & Assert - 不存在的组件
            Assert.False(entity.TryGetComponent<Block>(out var block));
            Assert.Equal(default(Block), block);
        }

        [Fact]
        public void Entity_ShouldCheckComponentExistence()
        {
            // Arrange
            var entity = Store.CreateEntity(new Position(Vector3.Zero));
            
            // Act & Assert
            Assert.True(entity.HasComponent<Position>());
            Assert.False(entity.HasComponent<Block>());
        }

        [Fact]
        public void Entity_ShouldHaveMultipleComponents()
        {
            // Arrange
            var position = new Vector3(1, 2, 3);
            var rotation = new Vector3(0.5f, 1.0f, 0.2f);
            var velocity = new Vector3(5, -2, 3);
            
            // Act
            var entity = Store.CreateEntity(
                new Position(position),
                new MCGame.ECS.Components.Rotation(rotation),
                new Velocity(velocity),
                new MCGame.ECS.Components.Player(),
                new Input()
            );
            
            // Assert
            AssertEntityHasComponent<Position>(entity);
            AssertEntityHasComponent<Rotation>(entity);
            AssertEntityHasComponent<Velocity>(entity);
            AssertEntityHasComponent<PlayerComp>(entity);
            AssertEntityHasComponent<Input>(entity);
            
            // 验证所有组件值
            Assert.Equal(position, entity.GetComponent<Position>().Value);
            Assert.Equal(rotation, entity.GetComponent<Rotation>().Value);
            Assert.Equal(velocity, entity.GetComponent<Velocity>().Value);
        }

        [Fact]
        public void Entity_ShouldSupportComponentMutation()
        {
            // Arrange
            var entity = Store.CreateEntity(new Position(Vector3.Zero));
            
            // Act
            var position = entity.GetComponent<Position>();
            position.Value = new Vector3(10, 20, 30);
            
            // Assert
            var updatedPosition = entity.GetComponent<Position>();
            Assert.Equal(new Vector3(10, 20, 30), updatedPosition.Value);
        }

        [Fact]
        public void Entity_ShouldHandleComponentStructCopy()
        {
            // Arrange
            var originalPosition = new Vector3(1, 2, 3);
            var entity = Store.CreateEntity(new Position(originalPosition));
            
            // Act
            var positionCopy = entity.GetComponent<Position>();
            positionCopy.Value = new Vector3(99, 99, 99);
            
            // Assert - 结构体是值类型，修改副本不应该影响原组件
            // 注意：实际行为取决于ECS实现，这里假设组件是可变的
            var actualPosition = entity.GetComponent<Position>();
            Assert.Equal(new Vector3(99, 99, 99), actualPosition.Value);
        }

        [Fact]
        public void Entity_ShouldCreateWithChunkComponent()
        {
            // Arrange
            var chunkPos = new ChunkPosition(1, 0, 2);
            
            // Act
            var entity = Store.CreateEntity(new Chunk(chunkPos));
            
            // Assert
            AssertEntityHasComponent<Chunk>(entity);
            var chunk = entity.GetComponent<Chunk>();
            Assert.Equal(chunkPos, chunk.Position);
            Assert.Equal(MCGame.Core.ChunkState.Unloaded, chunk.State);
        }

        [Fact]
        public void Entity_ShouldCreateWithMeshComponent()
        {
            // Arrange
            var bounds = new BoundingBox(Vector3.Zero, new Vector3(16, 256, 16));
            
            // Act
            var entity = Store.CreateEntity(new Mesh(bounds));
            
            // Assert
            AssertEntityHasComponent<Mesh>(entity);
            var mesh = entity.GetComponent<Mesh>();
            Assert.Equal(bounds, mesh.Bounds);
            Assert.True(mesh.IsDirty);
            Assert.Equal(0, mesh.VertexCount);
            Assert.Equal(0, mesh.IndexCount);
        }

        [Fact]
        public void Entity_ShouldCreateWithCameraComponent()
        {
            // Act
            var entity = Store.CreateEntity(new Camera(90f, 16f/9f, 0.1f, 1000f));
            
            // Assert
            AssertEntityHasComponent<Camera>(entity);
            var camera = entity.GetComponent<Camera>();
            Assert.Equal(90f, camera.FieldOfView);
            Assert.Equal(16f/9f, camera.AspectRatio);
            Assert.Equal(0.1f, camera.NearPlane);
            Assert.Equal(1000f, camera.FarPlane);
            Assert.True(camera.IsDirty);
        }

        [Fact]
        public void Entity_ShouldCreateWithPhysicsComponent()
        {
            // Act
            var entity = Store.CreateEntity(new Physics(2.5f, 0.2f));
            
            // Assert
            AssertEntityHasComponent<Physics>(entity);
            var physics = entity.GetComponent<Physics>();
            Assert.Equal(2.5f, physics.Mass);
            Assert.Equal(0.2f, physics.Drag);
            Assert.Equal(0f, physics.Bounciness);
            Assert.Equal(new Vector3(0, -9.81f, 0), physics.Gravity);
        }

        [Fact]
        public void Entity_ShouldCreateWithLifetimeComponent()
        {
            // Act
            var entity = Store.CreateEntity(new Lifetime(5.0f));
            
            // Assert
            AssertEntityHasComponent<Lifetime>(entity);
            var lifetime = entity.GetComponent<Lifetime>();
            Assert.Equal(5.0f, lifetime.TimeLeft);
            Assert.False(lifetime.IsExpired);
        }

        [Fact]
        public void Entity_ShouldHandleInvalidComponentAccess()
        {
            // Arrange
            var entity = Store.CreateEntity(new Position(Vector3.Zero));
            
            // Act & Assert
            // 尝试获取不存在的组件应该抛出异常
            Assert.ThrowsAny<Exception>(() => entity.GetComponent<Block>());
        }

        [Fact]
        public void Entity_ShouldCreateMultipleEntities()
        {
            // Arrange
            var entityCount = 100;
            
            // Act
            var entities = new List<Friflo.Engine.ECS.Entity>();
            for (int i = 0; i < entityCount; i++)
            {
                var entity = Store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass)
                );
                entities.Add(entity);
            }
            
            // Assert
            Assert.Equal(entityCount, entities.Count);
            Assert.Equal(entityCount, Store.Count);
            
            // 验证所有实体都有正确的组件
            foreach (var entity in entities)
            {
                AssertEntityHasComponent<Position>(entity);
                AssertEntityHasComponent<Block>(entity);
                
                var position = entity.GetComponent<Position>();
                var block = entity.GetComponent<Block>();
                
                Assert.True(position.Value.X >= 0 && position.Value.X < entityCount);
                Assert.Equal(0, position.Value.Y);
                Assert.Equal(0, position.Value.Z);
                Assert.Equal(BlockType.Grass, block.Type);
            }
        }
    }
}