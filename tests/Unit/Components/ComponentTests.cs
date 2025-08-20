using Microsoft.Xna.Framework;
using Xunit;
using MCGame.ECS.Components;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.Tests.Unit.Components
{
    /// <summary>
    /// 组件系统单元测试
    /// 测试所有ECS组件的基本功能
    /// </summary>
    public class ComponentTests : TestBase
    {
        [Fact]
        public void PositionComponent_ShouldStoreVector3Correctly()
        {
            // Arrange
            var expectedPosition = new Vector3(1.5f, 2.7f, -3.2f);
            
            // Act
            var positionComponent = new Position(expectedPosition);
            
            // Assert
            Assert.Equal(expectedPosition, positionComponent.Value);
            Assert.Equal(1.5f, positionComponent.Value.X);
            Assert.Equal(2.7f, positionComponent.Value.Y);
            Assert.Equal(-3.2f, positionComponent.Value.Z);
        }

        [Fact]
        public void PositionComponent_ShouldConstructFromIndividualValues()
        {
            // Arrange
            var x = 10.5f;
            var y = 20.7f;
            var z = -30.2f;
            var expectedVector = new Vector3(x, y, z);
            
            // Act
            var positionComponent = new Position(x, y, z);
            
            // Assert
            Assert.Equal(expectedVector, positionComponent.Value);
        }

        [Fact]
        public void RotationComponent_ShouldStoreEulerAnglesCorrectly()
        {
            // Arrange
            var expectedRotation = new Vector3(0.5f, 1.2f, -0.8f);
            
            // Act
            var rotationComponent = new MCGame.ECS.Components.Rotation(expectedRotation);
            
            // Assert
            Assert.Equal(expectedRotation, rotationComponent.Value);
            Assert.Equal(0.5f, rotationComponent.Value.X);  // Pitch
            Assert.Equal(1.2f, rotationComponent.Value.Y);  // Yaw
            Assert.Equal(-0.8f, rotationComponent.Value.Z); // Roll
        }

        [Fact]
        public void VelocityComponent_ShouldStoreVector3Correctly()
        {
            // Arrange
            var expectedVelocity = new Vector3(5.0f, -2.0f, 3.5f);
            
            // Act
            var velocityComponent = new Velocity(expectedVelocity);
            
            // Assert
            Assert.Equal(expectedVelocity, velocityComponent.Value);
        }

        [Fact]
        public void BlockComponent_ShouldStoreBlockTypeCorrectly()
        {
            // Arrange
            var expectedBlockType = BlockType.Grass;
            
            // Act
            var blockComponent = new Block(expectedBlockType);
            
            // Assert
            Assert.Equal(expectedBlockType, blockComponent.Type);
            Assert.Equal(expectedBlockType, blockComponent.Data.Type);
        }

        [Fact]
        public void BlockComponent_ShouldInitializeBlockData()
        {
            // Arrange
            var blockType = BlockType.Stone;
            
            // Act
            var blockComponent = new Block(blockType);
            
            // Assert
            Assert.NotNull(blockComponent.Data);
            Assert.Equal(blockType, blockComponent.Data.Type);
            Assert.True(blockComponent.Data.IsSolid);
            Assert.Equal(1.0f, blockComponent.Data.Hardness);
        }

        [Fact]
        public void PlayerComponent_ShouldInitializeWithDefaultValues()
        {
            // Act
            var playerComponent = new MCGame.ECS.Components.Player();
            
            // Assert
            Assert.Equal(10f, playerComponent.MoveSpeed);
            Assert.Equal(0.1f, playerComponent.LookSpeed);
            Assert.Equal(8f, playerComponent.JumpSpeed);
            Assert.False(playerComponent.IsGrounded);
            Assert.False(playerComponent.IsFlying);
        }

        [Fact]
        public void PlayerComponent_ShouldAcceptCustomValues()
        {
            // Arrange
            var moveSpeed = 15f;
            var lookSpeed = 0.15f;
            var jumpSpeed = 12f;
            
            // Act
            var playerComponent = new Player(moveSpeed, lookSpeed, jumpSpeed);
            
            // Assert
            Assert.Equal(moveSpeed, playerComponent.MoveSpeed);
            Assert.Equal(lookSpeed, playerComponent.LookSpeed);
            Assert.Equal(jumpSpeed, playerComponent.JumpSpeed);
        }

        [Fact]
        public void CameraComponent_ShouldInitializeWithDefaultValues()
        {
            // Act
            var cameraComponent = new Camera();
            
            // Assert
            Assert.Equal(75f, cameraComponent.FieldOfView);
            Assert.Equal(16f/9f, cameraComponent.AspectRatio);
            Assert.Equal(0.1f, cameraComponent.NearPlane);
            Assert.Equal(1000f, cameraComponent.FarPlane);
            Assert.True(cameraComponent.IsDirty);
            Assert.Equal(Matrix.Identity, cameraComponent.ViewMatrix);
            Assert.Equal(Matrix.Identity, cameraComponent.ProjectionMatrix);
        }

        [Fact]
        public void CameraComponent_ShouldAcceptCustomValues()
        {
            // Arrange
            var fov = 90f;
            var aspectRatio = 4f/3f;
            var nearPlane = 0.5f;
            var farPlane = 500f;
            
            // Act
            var cameraComponent = new Camera(fov, aspectRatio, nearPlane, farPlane);
            
            // Assert
            Assert.Equal(fov, cameraComponent.FieldOfView);
            Assert.Equal(aspectRatio, cameraComponent.AspectRatio);
            Assert.Equal(nearPlane, cameraComponent.NearPlane);
            Assert.Equal(farPlane, cameraComponent.FarPlane);
        }

        [Fact]
        public void VisibilityComponent_ShouldInitializeWithDefaultValues()
        {
            // Act
            var visibilityComponent = new Visibility();
            
            // Assert
            Assert.True(visibilityComponent.IsVisible);
            Assert.Equal(0f, visibilityComponent.Distance);
            Assert.False(visibilityComponent.InFrustum);
        }

        [Fact]
        public void VisibilityComponent_ShouldAcceptCustomValues()
        {
            // Arrange
            var isVisible = false;
            
            // Act
            var visibilityComponent = new Visibility(isVisible);
            
            // Assert
            Assert.Equal(isVisible, visibilityComponent.IsVisible);
        }

        [Fact]
        public void LightingComponent_ShouldInitializeWithDefaultValues()
        {
            // Act
            var lightingComponent = new Lighting();
            
            // Assert
            Assert.Equal((byte)15, lightingComponent.Brightness);
            Assert.Equal((byte)15, lightingComponent.Sunlight);
            Assert.Equal((byte)0, lightingComponent.Torchlight);
            Assert.Equal(Color.White, lightingComponent.TintColor);
        }

        [Fact]
        public void LightingComponent_ShouldAcceptCustomValues()
        {
            // Arrange
            var brightness = (byte)10;
            
            // Act
            var lightingComponent = new Lighting(brightness);
            
            // Assert
            Assert.Equal(brightness, lightingComponent.Brightness);
        }

        [Fact]
        public void ColliderComponent_ShouldInitializeWithBounds()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            
            // Act
            var colliderComponent = new Collider(bounds);
            
            // Assert
            Assert.Equal(bounds, colliderComponent.Bounds);
            Assert.True(colliderComponent.IsSolid);
            Assert.False(colliderComponent.IsTrigger);
        }

        [Fact]
        public void ColliderComponent_ShouldAcceptSolidParameter()
        {
            // Arrange
            var bounds = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            var isSolid = false;
            
            // Act
            var colliderComponent = new Collider(bounds, isSolid);
            
            // Assert
            Assert.Equal(bounds, colliderComponent.Bounds);
            Assert.Equal(isSolid, colliderComponent.IsSolid);
        }

        [Fact]
        public void InputComponent_ShouldInitializeWithDefaultValues()
        {
            // Act
            var inputComponent = new Input();
            
            // Assert
            Assert.Equal(Vector2.Zero, inputComponent.Movement);
            Assert.Equal(Vector2.Zero, inputComponent.Look);
            Assert.False(inputComponent.Jump);
            Assert.False(inputComponent.Sprint);
            Assert.False(inputComponent.Fly);
        }

        [Fact]
        public void LifetimeComponent_ShouldInitializeWithTimeLeft()
        {
            // Arrange
            var timeLeft = 5.0f;
            
            // Act
            var lifetimeComponent = new Lifetime(timeLeft);
            
            // Assert
            Assert.Equal(timeLeft, lifetimeComponent.TimeLeft);
            Assert.False(lifetimeComponent.IsExpired);
        }

        [Fact]
        public void PhysicsComponent_ShouldInitializeWithDefaultValues()
        {
            // Act
            var physicsComponent = new Physics();
            
            // Assert
            Assert.Equal(1f, physicsComponent.Mass);
            Assert.Equal(0.1f, physicsComponent.Drag);
            Assert.Equal(0f, physicsComponent.Bounciness);
            Assert.Equal(new Vector3(0, -9.81f, 0), physicsComponent.Gravity);
        }

        [Fact]
        public void PhysicsComponent_ShouldAcceptCustomValues()
        {
            // Arrange
            var mass = 2.5f;
            var drag = 0.2f;
            
            // Act
            var physicsComponent = new Physics(mass, drag);
            
            // Assert
            Assert.Equal(mass, physicsComponent.Mass);
            Assert.Equal(drag, physicsComponent.Drag);
        }

        [Fact]
        public void ComponentInterface_ShouldBeImplementedCorrectly()
        {
            // 测试所有组件都正确实现了IComponent接口
            
            // Arrange & Act
            var position = new Position(Vector3.Zero);
            var rotation = new MCGame.ECS.Components.Rotation(Vector3.Zero);
            var velocity = new Velocity(Vector3.Zero);
            var block = new Block(BlockType.Air);
            var player = new MCGame.ECS.Components.Player();
            var camera = new Camera();
            var visibility = new Visibility();
            var lighting = new Lighting();
            var collider = new Collider(BoundingBox.Zero);
            var input = new Input();
            var lifetime = new Lifetime(1f);
            var physics = new Physics();
            
            // Assert - 所有组件都是结构体且实现了IComponent接口
            Assert.True(position is IComponent);
            Assert.True(rotation is IComponent);
            Assert.True(velocity is IComponent);
            Assert.True(block is IComponent);
            Assert.True(player is IComponent);
            Assert.True(camera is IComponent);
            Assert.True(visibility is IComponent);
            Assert.True(lighting is IComponent);
            Assert.True(collider is IComponent);
            Assert.True(input is IComponent);
            Assert.True(lifetime is IComponent);
            Assert.True(physics is IComponent);
        }
    }
}