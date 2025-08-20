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

namespace MCGame.Tests.Documentation.Examples
{
    /// <summary>
    /// 文档示例验证测试
    /// 验证文档中的所有代码示例都能正确运行
    /// </summary>
    public class DocumentationExamplesTest
    {
        [Fact]
        public void BasicEntityCreationExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的基础实体创建示例
            var world = new EntityStore();
            var entity = world.CreateEntity();
            entity.AddComponent(new MCGame.ECS.Components.Position(new Vector3(1, 2, 3)));
            
            var position = entity.GetComponent<MCGame.ECS.Components.Position>();
            
            // Assert
            Assert.NotEqual(0, entity.Id);
            Assert.Equal(new Vector3(1, 2, 3), position.Value);
            Console.WriteLine($"创建实体成功，位置: ({position.Value.X}, {position.Value.Y}, {position.Value.Z})");
        }

        [Fact]
        public void BlockManagerExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的方块管理器示例
            var world = new EntityStore();
            var blockManager = new ECSBlockManager(world);
            var blockEntity = blockManager.SetBlock((MCGame.Core.BlockType)1, new Vector3(1, 2, 3));
            
            // Assert
            Assert.NotEqual(0, blockEntity.Id);
            Console.WriteLine($"创建方块成功，ID: {blockEntity.Id}");
            
            // 测试获取方块
            var blockType = blockManager.GetBlock(new Vector3(1, 2, 3));
            Assert.NotNull(blockType);
            Assert.Equal((MCGame.Core.BlockType)1, blockType.Value);
            Console.WriteLine($"获取方块成功，类型: {blockType}");
        }

        [Fact]
        public void ECSWorldInitializationExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的ECS世界初始化示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // Assert
            Assert.NotNull(ecsWorld.Store);
            Assert.NotNull(ecsWorld.EntityStore);
            Assert.Equal(0, ecsWorld.Store.Count);
            
            var stats = ecsWorld.GetEntityStats();
            Assert.Equal(0, stats.TotalEntities);
        }

        [Fact]
        public void ChunkCreationExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的区块创建示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var chunkPos = new ChunkPosition(1, 0, 2);
            var chunkEntity = ecsWorld.CreateChunkEntity(chunkPos);
            
            // Assert
            Assert.NotEqual(0, chunkEntity.Id);
            AssertEntityHasComponent<Chunk>(chunkEntity);
            AssertEntityHasComponent<Position>(chunkEntity);
            
            var chunk = chunkEntity.GetComponent<Chunk>();
            Assert.Equal(chunkPos, chunk.Position);
        }

        [Fact]
        public void BlockCreationExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的方块创建示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var blockType = BlockType.Grass;
            var position = new Vector3(1, 2, 3);
            var blockEntity = ecsWorld.CreateBlockEntity(blockType, position);
            
            // Assert
            Assert.NotEqual(0, blockEntity.Id);
            AssertEntityHasComponent<Block>(blockEntity);
            AssertEntityHasComponent<Position>(blockEntity);
            
            var block = blockEntity.GetComponent<Block>();
            Assert.Equal(blockType, block.Type);
            
            var pos = blockEntity.GetComponent<Position>();
            Assert.Equal(position, pos.Value);
        }

        [Fact]
        public void BatchBlockCreationExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的批量方块创建示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var blockCount = 10;
            
            var blockTypes = new BlockType[blockCount];
            var positions = new Vector3[blockCount];
            
            for (int i = 0; i < blockCount; i++)
            {
                blockTypes[i] = BlockType.Grass;
                positions[i] = new Vector3(i, 0, 0);
            }
            
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
        public void QueryExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的查询示例
            var store = new EntityStore();
            
            // 创建测试实体
            for (int i = 0; i < 10; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Block(BlockType.Grass),
                    new Visibility(true)
                );
            }
            
            var query = store.Query<Block, Position>();
            
            // Assert
            Assert.Equal(10, query.Count);
            
            var entities = query.Entities.ToList();
            Assert.Equal(10, entities.Count);
            
            foreach (var entity in entities)
            {
                AssertEntityHasComponent<Block>(entity);
                AssertEntityHasComponent<Position>(entity);
            }
        }

        [Fact]
        public void SystemExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的系统示例
            var store = new EntityStore();
            var systemRoot = new SystemRoot(store);
            
            // 创建测试实体
            for (int i = 0; i < 5; i++)
            {
                store.CreateEntity(
                    new Position(new Vector3(i, 0, 0)),
                    new Velocity(0, -1, 0),
                    new Physics()
                );
            }
            
            var physicsSystem = new PhysicsSystem();
            systemRoot.Add(physicsSystem);
            
            // 运行系统
            systemRoot.Update(new UpdateTick());
            
            // Assert
            Assert.True(physicsSystem.Executed);
        }

        [Fact]
        public void PlayerEntityExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的玩家实体示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var playerPosition = new Vector3(0, 64, 0);
            
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
        public void WorldUpdateExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的世界更新示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));
            
            // 创建一些测试实体
            ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(0, 0, 0));
            ecsWorld.CreateBlockEntity(BlockType.Stone, new Vector3(1, 0, 0));
            
            // 运行系统更新
            ecsWorld.Update(gameTime);
            
            // Assert
            var stats = ecsWorld.GetEntityStats();
            Assert.Equal(2, stats.TotalEntities);
            
            var perfStats = ecsWorld.GetPerformanceStats();
            Assert.NotNull(perfStats);
        }

        [Fact]
        public void ViewFrustumExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的视锥体示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            var cameraPosition = new Vector3(0, 64, 0);
            var viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Forward, Vector3.Up);
            var projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, 16f/9f, 0.1f, 1000f);
            var frustum = new BoundingFrustum(viewMatrix * projectionMatrix);
            
            // 创建一些测试实体
            ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(0, 0, 0));
            ecsWorld.CreateBlockEntity(BlockType.Stone, new Vector3(10, 0, 0));
            ecsWorld.CreateBlockEntity(BlockType.Dirt, new Vector3(50, 0, 0));
            
            // 设置视锥体
            ecsWorld.SetViewFrustum(frustum, cameraPosition);
            ecsWorld.Update(new GameTime());
            
            // Assert
            var visibleBlocks = ecsWorld.GetVisibleBlocks();
            Assert.NotEmpty(visibleBlocks);
            
            foreach (var block in visibleBlocks)
            {
                var visibility = block.GetComponent<Visibility>();
                Assert.True(visibility.IsVisible);
            }
        }

        [Fact]
        public void PerformanceStatsExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的性能统计示例
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
            
            var perfStats = ecsWorld.GetPerformanceStats();
            var entityStats = ecsWorld.GetEntityStats();
            
            // Assert
            Assert.NotNull(perfStats);
            Assert.NotEmpty(perfStats);
            
            Assert.Equal(13, entityStats.TotalEntities);
            Assert.Equal(3, entityStats.ChunkEntities);
            Assert.Equal(10, entityStats.BlockEntities);
            Assert.Equal(0, entityStats.PlayerEntities);
        }

        [Fact]
        public void ComponentTypesExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的组件类型示例
            var store = new EntityStore();
            
            var entity = store.CreateEntity(
                new Position(new Vector3(1, 2, 3)),
                new MCGame.ECS.Components.Rotation(new Vector3(0, 1.57f, 0)),
                new Velocity(new Vector3(0, 0, 5)),
                new Block(BlockType.Grass),
                new Player(10f, 0.1f, 8f),
                new Camera(75f, 16f/9f, 0.1f, 1000f),
                new Visibility(true),
                new Lighting(15),
                new Collider(new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))),
                new Input(),
                new Physics(1f, 0.1f),
                new Lifetime(10f)
            );
            
            // Assert
            AssertEntityHasComponent<Position>(entity);
            AssertEntityHasComponent<Rotation>(entity);
            AssertEntityHasComponent<Velocity>(entity);
            AssertEntityHasComponent<Block>(entity);
            AssertEntityHasComponent<Player>(entity);
            AssertEntityHasComponent<Camera>(entity);
            AssertEntityHasComponent<Visibility>(entity);
            AssertEntityHasComponent<Lighting>(entity);
            AssertEntityHasComponent<Collider>(entity);
            AssertEntityHasComponent<Input>(entity);
            AssertEntityHasComponent<Physics>(entity);
            AssertEntityHasComponent<Lifetime>(entity);
            
            // 验证组件值
            var position = entity.GetComponent<Position>();
            Assert.Equal(new Vector3(1, 2, 3), position.Value);
            
            var player = entity.GetComponent<Player>();
            Assert.Equal(10f, player.MoveSpeed);
            Assert.Equal(0.1f, player.LookSpeed);
            Assert.Equal(8f, player.JumpSpeed);
        }

        [Fact]
        public void BlockManagerAdvancedExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的高级方块管理器示例
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            
            // 创建一些方块
            for (int i = 0; i < 10; i++)
            {
                blockManager.SetBlock(BlockType.Grass, new Vector3(i, 0, 0));
            }
            
            // 获取所有方块
            var allBlocks = blockManager.GetAllBlocks();
            Assert.Equal(10, allBlocks.Length);
            
            // 获取区块内的方块
            var chunkBlocks = blockManager.GetBlocksInChunk(new ChunkPosition(0, 0, 0));
            Assert.Equal(10, chunkBlocks.Length);
            
            // 获取范围内的方块
            var nearbyBlocks = blockManager.GetBlocksInRange(new Vector3(5, 0, 0), 5f);
            Assert.NotEmpty(nearbyBlocks);
            
            // 射线检测
            var hitBlock = blockManager.Raycast(new Vector3(0, 5, 0), new Vector3(0, -1, 0), 10f);
            Assert.NotNull(hitBlock);
            
            // 内存统计
            var stats = blockManager.GetMemoryStats();
            Assert.Equal(10, stats.TotalBlocks);
        }

        [Fact]
        public void ChunkManagerExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的区块管理器示例
            var store = new EntityStore();
            var blockManager = new ECSBlockManager(store);
            var chunkManager = new ECSChunkManager(store, blockManager, 10);
            
            // 创建区块
            var chunkPos = new ChunkPosition(0, 0, 0);
            var chunkEntity = chunkManager.CreateChunk(chunkPos);
            
            // Assert
            Assert.NotEqual(0, chunkEntity.Id);
            AssertEntityHasComponent<Chunk>(chunkEntity);
            
            // 获取区块
            var retrievedChunk = chunkManager.GetChunk(chunkPos);
            Assert.Equal(chunkEntity.Id, retrievedChunk.Id);
            
            // 获取已加载的区块
            var loadedChunks = chunkManager.GetLoadedChunks();
            Assert.Single(loadedChunks);
            
            // 获取脏区块
            var dirtyChunks = chunkManager.GetDirtyChunks();
            Assert.Single(dirtyChunks);
            
            // 标记区块为已加载
            chunkManager.MarkChunkLoaded(chunkPos);
            
            // 标记区块为脏
            chunkManager.MarkChunkDirty(chunkPos);
            
            // 获取统计信息
            var stats = chunkManager.GetStats();
            Assert.Equal(1, stats.TotalChunks);
            Assert.Equal(10, stats.RenderDistance);
        }

        [Fact]
        public void ComplexScenarioExample_ShouldWork()
        {
            // Arrange & Act - 来自文档的复杂场景示例
            var ecsWorld = new MCGame.ECS.ECSWorld();
            
            // 创建玩家
            ecsWorld.PlayerEntity = ecsWorld.Store.CreateEntity(
                new Position(0, 64, 0),
                new MCGame.ECS.Components.Rotation(0, 0, 0),
                new Velocity(0, 0, 0),
                new MCGame.ECS.Components.Player(),
                new Input(),
                new Camera(),
                new Visibility(true)
            );
            
            // 创建区块
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ecsWorld.CreateChunkEntity(new ChunkPosition(x, 0, z));
                }
            }
            
            // 创建方块
            for (int x = -10; x <= 10; x++)
            {
                for (int z = -10; z <= 10; z++)
                {
                    if (x % 2 == 0 && z % 2 == 0)
                    {
                        ecsWorld.CreateBlockEntity(BlockType.Grass, new Vector3(x, 0, z));
                    }
                }
            }
            
            // 设置视锥体
            var cameraPosition = new Vector3(0, 64, 0);
            var viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Forward, Vector3.Up);
            var projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, 16f/9f, 0.1f, 1000f);
            var frustum = new BoundingFrustum(viewMatrix * projectionMatrix);
            
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
        }

        #region Helper Methods

        private static void AssertEntityHasComponent<T>(Entity entity) where T : struct, IComponent
        {
            Assert.True(entity.HasComponent<T>(), $"Entity should have component {typeof(T).Name}");
        }

        #endregion
    }
}