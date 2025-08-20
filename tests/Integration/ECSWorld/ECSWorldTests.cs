using Microsoft.Xna.Framework;
using Xunit;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS;
using MCGame.ECS.Components;
using MCGame.ECS.Managers;
using MCGame.ECS.Systems;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.Tests.Integration.ECSWorld
{
    /// <summary>
    /// ECS世界集成测试
    /// 测试ECSWorld的整体功能集成
    /// </summary>
    public class ECSWorldTests
    {
        [Fact]
        public void ECSWorld_ShouldInitializeCorrectly()
        {
            // Act
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // Assert
            Assert.NotNull(ecsWorld.Store);
            Assert.NotNull(ecsWorld.EntityStore);
            Assert.Equal(0, ecsWorld.Store.Count);
            
            var stats = ecsWorld.GetEntityStats();
            Assert.Equal(0, stats.TotalEntities);
            Assert.Equal(0, stats.ChunkEntities);
            Assert.Equal(0, stats.BlockEntities);
            Assert.Equal(0, stats.PlayerEntities);
        }

        [Fact]
        public void ECSWorld_ShouldCreateChunkEntity()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var chunkPos = new ChunkPosition(1, 0, 2);
            
            // Act
            var chunkEntity = ecsWorld.CreateChunkEntity(chunkPos);
            
            // Assert
            Assert.NotEqual(0, chunkEntity.Id);
            AssertEntityHasComponent<Chunk>(chunkEntity);
            AssertEntityHasComponent<Position>(chunkEntity);
            AssertEntityHasComponent<Mesh>(chunkEntity);
            AssertEntityHasComponent<Visibility>(chunkEntity);
            AssertEntityHasComponent<Collider>(chunkEntity);
            
            var chunk = chunkEntity.GetComponent<Chunk>();
            Assert.Equal(chunkPos, chunk.Position);
            
            var position = chunkEntity.GetComponent<Position>();
            var expectedWorldPos = chunkPos.ToWorldPosition(16);
            Assert.Equal(expectedWorldPos, position.Value);
        }

        [Fact]
        public void ECSWorld_ShouldCreateBlockEntity()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var blockType = BlockType.Grass;
            var position = new Vector3(1, 2, 3);
            
            // Act
            var blockEntity = ecsWorld.CreateBlockEntity(blockType, position);
            
            // Assert
            Assert.NotEqual(0, blockEntity.Id);
            AssertEntityHasComponent<Block>(blockEntity);
            AssertEntityHasComponent<Position>(blockEntity);
            AssertEntityHasComponent<Visibility>(blockEntity);
            AssertEntityHasComponent<Collider>(blockEntity);
            AssertEntityHasComponent<Lighting>(blockEntity);
            
            var block = blockEntity.GetComponent<Block>();
            Assert.Equal(blockType, block.Type);
            
            var pos = blockEntity.GetComponent<Position>();
            Assert.Equal(position, pos.Value);
        }

        [Fact]
        public void ECSWorld_ShouldCreateBlockEntitiesBatch()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var blockCount = 10;
            
            var blockTypes = new BlockType[blockCount];
            var positions = new Vector3[blockCount];
            
            for (int i = 0; i < blockCount; i++)
            {
                blockTypes[i] = BlockType.Grass;
                positions[i] = new Vector3(i, 0, 0);
            }
            
            // Act
            var entities = ecsWorld.CreateBlockEntitiesBatch(blockTypes, positions);
            
            // Assert
            Assert.Equal(blockCount, entities.Length);
            
            for (int i = 0; i < blockCount; i++)
            {
                Assert.NotEqual(0, entities[i].Id);
                AssertEntityHasComponent<Block>(entities[i]);
                AssertEntityHasComponent<Position>(entities[i]);
                
                var block = entities[i].GetComponent<Block>();
                var position = entities[i].GetComponent<Position>();
                
                Assert.Equal(blockTypes[i], block.Type);
                Assert.Equal(positions[i], position.Value);
            }
        }

        [Fact]
        public void ECSWorld_ShouldGetChunkEntity()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var chunkPos = new ChunkPosition(1, 0, 2);
            
            var createdEntity = ecsWorld.CreateChunkEntity(chunkPos);
            
            // Act
            var retrievedEntity = ecsWorld.GetChunkEntity(chunkPos);
            
            // Assert
            Assert.Equal(createdEntity.Id, retrievedEntity.Id);
        }

        [Fact]
        public void ECSWorld_ShouldReturnDefaultForNonExistentChunk()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var chunkPos = new ChunkPosition(999, 0, 999);
            
            // Act
            var entity = ecsWorld.GetChunkEntity(chunkPos);
            
            // Assert
            Assert.Equal(default(Entity), entity);
        }

        [Fact]
        public void ECSWorld_ShouldGetBlockEntity()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var blockType = BlockType.Grass;
            var position = new Vector3(1, 2, 3);
            
            var createdEntity = ecsWorld.CreateBlockEntity(blockType, position);
            
            // Act
            var retrievedEntity = ecsWorld.GetBlockEntity(position);
            
            // Assert
            Assert.Equal(createdEntity.Id, retrievedEntity.Id);
        }

        [Fact]
        public void ECSWorld_ShouldReturnDefaultForNonExistentBlock()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var position = new Vector3(999, 999, 999);
            
            // Act
            var entity = ecsWorld.GetBlockEntity(position);
            
            // Assert
            Assert.Equal(default(Entity), entity);
        }

        [Fact]
        public void ECSWorld_ShouldUpdateSystems()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));
            
            // 创建一些测试实体
            ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(0, 0, 0));
            ecsWorld.CreateBlockEntity(BlockType.Stone, new Vector3(1, 0, 0));
            
            // Act
            ecsWorld.Update(gameTime);
            
            // Assert
            // 验证系统执行了（通过性能统计）
            var perfStats = ecsWorld.GetPerformanceStats();
            Assert.NotNull(perfStats);
            Assert.NotEmpty(perfStats);
        }

        [Fact]
        public void ECSWorld_ShouldSetViewFrustum()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var cameraPosition = new Vector3(0, 64, 0);
            var viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Forward, Vector3.Up);
            var projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, 16f/9f, 0.1f, 1000f);
            var frustum = new BoundingFrustum(viewMatrix * projectionMatrix);
            
            // 创建一些测试实体
            ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(0, 0, 0));
            ecsWorld.CreateBlockEntity(BlockType.Stone, new Vector3(10, 0, 0));
            ecsWorld.CreateBlockEntity(BlockType.Dirt, new Vector3(50, 0, 0)); // 较远的方块
            
            // Act
            ecsWorld.SetViewFrustum(frustum, cameraPosition);
            ecsWorld.Update(new GameTime());
            
            // Assert
            var visibleBlocks = ecsWorld.GetVisibleBlocks();
            Assert.NotEmpty(visibleBlocks);
            
            // 验证可见性计算
            foreach (var block in visibleBlocks)
            {
                var visibility = block.GetComponent<Visibility>();
                var position = block.GetComponent<Position>();
                var distance = Vector3.Distance(position.Value, cameraPosition);
                
                Assert.True(visibility.IsVisible);
                Assert.True(distance <= 200f); // 渲染距离
            }
        }

        [Fact]
        public void ECSWorld_ShouldGetVisibleChunks()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var cameraPosition = new Vector3(0, 64, 0);
            
            // 创建一些测试区块
            ecsWorld.CreateChunkEntity(new ChunkPosition(0, 0, 0));
            ecsWorld.CreateChunkEntity(new ChunkPosition(1, 0, 0));
            ecsWorld.CreateChunkEntity(new ChunkPosition(2, 0, 0));
            
            var viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Forward, Vector3.Up);
            var projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, 16f/9f, 0.1f, 1000f);
            var frustum = new BoundingFrustum(viewMatrix * projectionMatrix);
            
            // Act
            ecsWorld.SetViewFrustum(frustum, cameraPosition);
            ecsWorld.Update(new GameTime());
            
            var visibleChunks = ecsWorld.GetVisibleChunks();
            
            // Assert
            Assert.NotEmpty(visibleChunks);
            
            // 验证所有可见区块都有Visibility组件
            foreach (var chunk in visibleChunks)
            {
                AssertEntityHasComponent<Visibility>(chunk);
                var visibility = chunk.GetComponent<Visibility>();
                Assert.True(visibility.IsVisible);
            }
        }

        [Fact]
        public void ECSWorld_ShouldProvidePerformanceStats()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // 创建一些测试实体
            for (int i = 0; i < 10; i++)
            {
                ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(i, 0, 0));
            }
            
            for (int i = 0; i < 3; i++)
            {
                ecsWorld.CreateChunkEntity(new ChunkPosition(i, 0, 0));
            }
            
            // Act
            var perfStats = ecsWorld.GetPerformanceStats();
            var entityStats = ecsWorld.GetEntityStats();
            
            // Assert
            Assert.NotNull(perfStats);
            Assert.NotEmpty(perfStats);
            
            Assert.Equal(13, entityStats.TotalEntities); // 10 blocks + 3 chunks
            Assert.Equal(3, entityStats.ChunkEntities);
            Assert.Equal(10, entityStats.BlockEntities);
            Assert.Equal(0, entityStats.PlayerEntities);
        }

        [Fact]
        public void ECSWorld_ShouldHandleComplexScenario()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // 创建一个复杂的世界场景
            // 1. 创建玩家
            var playerEntity = ecsWorld.Store.CreateEntity(
                new Position(0, 64, 0),
                new MCGame.ECS.Components.Rotation(0, 0, 0),
                new Velocity(0, 0, 0),
                new MCGame.ECS.Components.Player(),
                new Input(),
                new Camera(),
                new Visibility(true)
            );
            
            // 2. 创建一些区块
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ecsWorld.CreateChunkEntity(new ChunkPosition(x, 0, z));
                }
            }
            
            // 3. 在每个区块中创建一些方块
            for (int x = -10; x <= 10; x++)
            {
                for (int z = -10; z <= 10; z++)
                {
                    if (x % 2 == 0 && z % 2 == 0) // 创建网格状方块
                    {
                        ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(x, 0, z));
                    }
                }
            }
            
            // 4. 设置视锥体
            var cameraPosition = new Vector3(0, 64, 0);
            var viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Forward, Vector3.Up);
            var projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, 16f/9f, 0.1f, 1000f);
            var frustum = new BoundingFrustum(viewMatrix * projectionMatrix);
            
            // Act
            ecsWorld.SetViewFrustum(frustum, cameraPosition);
            ecsWorld.Update(new GameTime());
            
            // Assert
            var stats = ecsWorld.GetEntityStats();
            Assert.True(stats.TotalEntities > 0);
            Assert.True(stats.ChunkEntities > 0);
            Assert.True(stats.BlockEntities > 0);
            
            var visibleBlocks = ecsWorld.GetVisibleBlocks();
            var visibleChunks = ecsWorld.GetVisibleChunks();
            
            Assert.NotEmpty(visibleBlocks);
            Assert.NotEmpty(visibleChunks);
            
            var perfStats = ecsWorld.GetPerformanceStats();
            Assert.NotNull(perfStats);
            Assert.NotEmpty(perfStats);
        }

        [Fact]
        public void ECSWorld_ShouldSupportPlayerEntity()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var playerPosition = new Vector3(0, 64, 0);
            
            // Act
            ecsWorld.PlayerEntity = ecsWorld.Store.CreateEntity(
                new Position(playerPosition),
                new MCGame.ECS.Components.Rotation(0, 0, 0),
                new Velocity(0, 0, 0),
                new MCGame.ECS.Components.Player(),
                new Input(),
                new Camera(),
                new Visibility(true)
            );
            
            // Assert
            Assert.NotEqual(0, ecsWorld.PlayerEntity.Id);
            AssertEntityHasComponent<Position>(ecsWorld.PlayerEntity);
            AssertEntityHasComponent<Player>(ecsWorld.PlayerEntity);
            AssertEntityHasComponent<Input>(ecsWorld.PlayerEntity);
            AssertEntityHasComponent<Camera>(ecsWorld.PlayerEntity);
            
            var position = ecsWorld.PlayerEntity.GetComponent<Position>();
            Assert.Equal(playerPosition, position.Value);
        }

        [Fact]
        public void ECSWorld_ShouldDestroyWithoutErrors()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // 创建一些实体
            for (int i = 0; i < 10; i++)
            {
                ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(i, 0, 0));
            }
            
            // Act & Assert
            var exception = Record.Exception(() => ecsWorld.Destroy());
            Assert.Null(exception);
        }

        [Fact]
        public void ECSWorld_ShouldHandleEmptyWorld()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // Act
            var stats = ecsWorld.GetEntityStats();
            var perfStats = ecsWorld.GetPerformanceStats();
            var visibleBlocks = ecsWorld.GetVisibleBlocks();
            var visibleChunks = ecsWorld.GetVisibleChunks();
            
            // Assert
            Assert.Equal(0, stats.TotalEntities);
            Assert.NotNull(perfStats);
            Assert.Empty(visibleBlocks);
            Assert.Empty(visibleChunks);
        }

        [Fact]
        public void ECSWorld_ShouldUpdateMultipleTimes()
        {
            // Arrange
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // 创建一些测试实体
            ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(0, 0, 0));
            ecsWorld.CreateChunkEntity(new ChunkPosition(0, 0, 0));
            
            // Act
            for (int i = 0; i < 10; i++)
            {
                var gameTime = new GameTime(
                    TimeSpan.FromSeconds(i * 0.016),
                    TimeSpan.FromSeconds(0.016));
                ecsWorld.Update(gameTime);
            }
            
            // Assert
            var stats = ecsWorld.GetEntityStats();
            Assert.Equal(2, stats.TotalEntities);
            
            var perfStats = ecsWorld.GetPerformanceStats();
            Assert.NotNull(perfStats);
        }

        #region Helper Methods

        private static void AssertEntityHasComponent<T>(Entity entity) where T : struct, IComponent
        {
            Assert.True(entity.HasComponent<T>(), $"Entity should have component {typeof(T).Name}");
        }

        #endregion
    }
}