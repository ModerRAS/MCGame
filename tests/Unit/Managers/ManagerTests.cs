using Microsoft.Xna.Framework;
using Xunit;
using MCGame.ECS.Managers;
using MCGame.ECS.Components;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.Tests.Unit.Managers
{
    /// <summary>
    /// 管理器系统单元测试
    /// 测试ECS管理器的基本功能
    /// </summary>
    public class ManagerTests : TestBase
    {
        [Fact]
        public void ECSBlockManager_ShouldCreateBlockEntity()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var blockType = BlockType.Grass;
            var position = new Vector3(1, 2, 3);
            
            // Act
            var entity = blockManager.SetBlock(blockType, position);
            
            // Assert
            Assert.NotEqual(0, entity.Id);
            AssertEntityHasComponent<Block>(entity);
            AssertEntityHasComponent<Position>(entity);
            AssertEntityHasComponent<Visibility>(entity);
            AssertEntityHasComponent<Collider>(entity);
            AssertEntityHasComponent<Lighting>(entity);
            
            // 验证组件值
            var block = entity.GetComponent<Block>();
            var pos = entity.GetComponent<Position>();
            var visibility = entity.GetComponent<Visibility>();
            var collider = entity.GetComponent<Collider>();
            var lighting = entity.GetComponent<Lighting>();
            
            Assert.Equal(blockType, block.Type);
            Assert.Equal(position, pos.Value);
            Assert.True(visibility.IsVisible);
            Assert.Equal(new BoundingBox(position, position + Vector3.One), collider.Bounds);
            Assert.True(collider.IsSolid);
            Assert.Equal(15, lighting.Brightness);
        }

        [Fact]
        public void ECSBlockManager_ShouldGetBlockType()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var blockType = BlockType.Stone;
            var position = new Vector3(5, 10, 15);
            
            blockManager.SetBlock(blockType, position);
            
            // Act
            var retrievedType = blockManager.GetBlock(position);
            
            // Assert
            Assert.NotNull(retrievedType);
            Assert.Equal(blockType, retrievedType.Value);
        }

        [Fact]
        public void ECSBlockManager_ShouldReturnNullForNonExistentBlock()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var position = new Vector3(999, 999, 999);
            
            // Act
            var blockType = blockManager.GetBlock(position);
            
            // Assert
            Assert.Null(blockType);
        }

        [Fact]
        public void ECSBlockManager_ShouldUpdateExistingBlock()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var position = new Vector3(1, 2, 3);
            var initialType = BlockType.Grass;
            var newType = BlockType.Stone;
            
            blockManager.SetBlock(initialType, position);
            
            // Act
            var entity = blockManager.SetBlock(newType, position);
            
            // Assert
            var block = entity.GetComponent<Block>();
            Assert.Equal(newType, block.Type);
            
            // 验证仍然只有一个实体
            var allBlocks = blockManager.GetAllBlocks();
            Assert.Single(allBlocks);
        }

        [Fact]
        public void ECSBlockManager_ShouldCreateMultipleBlocks()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var blockCount = 10;
            
            // Act
            var entities = new List<Friflo.Engine.ECS.Entity>();
            for (int i = 0; i < blockCount; i++)
            {
                var entity = blockManager.SetBlock(BlockType.Grass, new Vector3(i, 0, 0));
                entities.Add(entity);
            }
            
            // Assert
            Assert.Equal(blockCount, entities.Count);
            Assert.Equal(blockCount, blockManager.GetBlockCount());
            
            var allBlocks = blockManager.GetAllBlocks();
            Assert.Equal(blockCount, allBlocks.Length);
            
            // 验证所有方块都在正确位置
            for (int i = 0; i < blockCount; i++)
            {
                var position = new Vector3(i, 0, 0);
                var blockType = blockManager.GetBlock(position);
                Assert.NotNull(blockType);
                Assert.Equal(BlockType.Grass, blockType.Value);
            }
        }

        [Fact]
        public void ECSBlockManager_ShouldSetBlocksBatch()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var blockCount = 50;
            
            var blockTypes = new BlockType[blockCount];
            var positions = new Vector3[blockCount];
            
            for (int i = 0; i < blockCount; i++)
            {
                blockTypes[i] = BlockType.Grass;
                positions[i] = new Vector3(i, 0, 0);
            }
            
            // Act
            blockManager.SetBlocksBatch(blockTypes, positions);
            
            // Assert
            Assert.Equal(blockCount, blockManager.GetBlockCount());
            
            var allBlocks = blockManager.GetAllBlocks();
            Assert.Equal(blockCount, allBlocks.Length);
        }

        [Fact]
        public void ECSBlockManager_ShouldGetBlocksInChunk()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkPos = new ChunkPosition(0, 0, 0);
            
            // 在同一个区块内创建多个方块
            for (int x = 0; x < 5; x++)
            {
                for (int z = 0; z < 5; z++)
                {
                    var position = new Vector3(x, 0, z);
                    blockManager.SetBlock(BlockType.Grass, position);
                }
            }
            
            // Act
            var chunkBlocks = blockManager.GetBlocksInChunk(chunkPos);
            
            // Assert
            Assert.Equal(25, chunkBlocks.Length);
            
            // 验证所有方块都在正确的区块内
            foreach (var entity in chunkBlocks)
            {
                var position = entity.GetComponent<Position>().Value;
                var calculatedChunkPos = new ChunkPosition(
                    (int)Math.Floor(position.X / 16f),
                    0,
                    (int)Math.Floor(position.Z / 16f)
                );
                Assert.Equal(chunkPos, calculatedChunkPos);
            }
        }

        [Fact]
        public void ECSBlockManager_ShouldGetBlocksInRange()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var center = new Vector3(5, 0, 5);
            var radius = 3f;
            
            // 创建一些方块
            for (int x = 0; x < 10; x++)
            {
                for (int z = 0; z < 10; z++)
                {
                    var position = new Vector3(x, 0, z);
                    blockManager.SetBlock(BlockType.Grass, position);
                }
            }
            
            // Act
            var blocksInRange = blockManager.GetBlocksInRange(center, radius);
            
            // Assert
            Assert.NotEmpty(blocksInRange);
            
            // 验证所有方块都在范围内
            foreach (var entity in blocksInRange)
            {
                var position = entity.GetComponent<Position>().Value;
                var distance = Vector3.Distance(position, center);
                Assert.True(distance <= radius, $"Block at {position} is outside range {radius}");
            }
        }

        [Fact]
        public void ECSBlockManager_ShouldPerformRaycast()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var blockPosition = new Vector3(5, 5, 5);
            var rayOrigin = new Vector3(0, 5, 5);
            var rayDirection = new Vector3(1, 0, 0);
            var maxDistance = 10f;
            
            blockManager.SetBlock(BlockType.Grass, blockPosition);
            
            // Act
            var hitEntity = blockManager.Raycast(rayOrigin, rayDirection, maxDistance);
            
            // Assert
            Assert.NotEqual(default(Entity), hitEntity);
            
            var hitPosition = hitEntity.GetComponent<Position>().Value;
            Assert.Equal(blockPosition, hitPosition);
        }

        [Fact]
        public void ECSBlockManager_ShouldReturnNullForMissedRaycast()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var rayOrigin = new Vector3(0, 5, 5);
            var rayDirection = new Vector3(1, 0, 0);
            var maxDistance = 10f;
            
            // 不创建任何方块
            
            // Act
            var hitEntity = blockManager.Raycast(rayOrigin, rayDirection, maxDistance);
            
            // Assert
            Assert.Null(hitEntity);
        }

        [Fact]
        public void ECSBlockManager_ShouldGetMemoryStats()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            
            // 创建一些方块
            for (int i = 0; i < 10; i++)
            {
                blockManager.SetBlock(BlockType.Grass, new Vector3(i, 0, 0));
            }
            
            // Act
            var stats = blockManager.GetMemoryStats();
            
            // Assert
            Assert.Equal(10, stats.TotalBlocks);
            Assert.Equal(1, stats.TotalChunks); // 所有方块都在同一个区块
            Assert.Equal(10, stats.DictionaryEntries);
            Assert.Equal(10, stats.AverageBlocksPerChunk);
        }

        [Fact]
        public void ECSChunkManager_ShouldCreateChunk()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            var chunkPos = new ChunkPosition(1, 0, 2);
            
            // Act
            var entity = chunkManager.CreateChunk(chunkPos);
            
            // Assert
            Assert.NotEqual(0, entity.Id);
            AssertEntityHasComponent<Chunk>(entity);
            AssertEntityHasComponent<Position>(entity);
            AssertEntityHasComponent<Mesh>(entity);
            AssertEntityHasComponent<Visibility>(entity);
            AssertEntityHasComponent<Collider>(entity);
            
            // 验证组件值
            var chunk = entity.GetComponent<Chunk>();
            var position = entity.GetComponent<Position>();
            var mesh = entity.GetComponent<Mesh>();
            var collider = entity.GetComponent<Collider>();
            
            Assert.Equal(chunkPos, chunk.Position);
            Assert.Equal(ChunkState.Loading, chunk.State);
            Assert.True(chunk.IsDirty);
            
            var expectedWorldPos = chunkPos.ToWorldPosition(16);
            Assert.Equal(expectedWorldPos, position.Value);
            
            var expectedBounds = new BoundingBox(expectedWorldPos, expectedWorldPos + new Vector3(16, 256, 16));
            Assert.Equal(expectedBounds, mesh.Bounds);
            Assert.Equal(expectedBounds, collider.Bounds);
            Assert.False(collider.IsSolid);
        }

        [Fact]
        public void ECSChunkManager_ShouldGetExistingChunk()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            var chunkPos = new ChunkPosition(1, 0, 2);
            
            var createdEntity = chunkManager.CreateChunk(chunkPos);
            
            // Act
            var retrievedEntity = chunkManager.GetChunk(chunkPos);
            
            // Assert
            Assert.Equal(createdEntity.Id, retrievedEntity.Id);
        }

        [Fact]
        public void ECSChunkManager_ShouldReturnNullForNonExistentChunk()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            var chunkPos = new ChunkPosition(999, 0, 999);
            
            // Act
            var entity = chunkManager.GetChunk(chunkPos);
            
            // Assert
            Assert.Equal(default(Entity), entity);
        }

        [Fact]
        public void ECSChunkManager_ShouldUpdateChunkLoading()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager, 5);
            var initialPlayerPos = new ChunkPosition(0, 0, 0);
            var newPlayerPos = new ChunkPosition(3, 0, 3);
            
            // 创建初始区块
            chunkManager.UpdateChunkLoading(initialPlayerPos);
            var initialChunkCount = chunkManager.GetChunkCount();
            
            // Act
            chunkManager.UpdateChunkLoading(newPlayerPos);
            var finalChunkCount = chunkManager.GetChunkCount();
            
            // Assert
            Assert.True(finalChunkCount > initialChunkCount);
            
            // 验证新区块被创建
            var nearbyChunks = new List<ChunkPosition>();
            for (int x = newPlayerPos.X - 5; x <= newPlayerPos.X + 5; x++)
            {
                for (int z = newPlayerPos.Z - 5; z <= newPlayerPos.Z + 5; z++)
                {
                    var chunkPos = new ChunkPosition(x, 0, z);
                    if (chunkManager.GetChunk(chunkPos).Id != 0)
                    {
                        nearbyChunks.Add(chunkPos);
                    }
                }
            }
            
            Assert.NotEmpty(nearbyChunks);
        }

        [Fact]
        public void ECSChunkManager_ShouldGetLoadedChunks()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            var chunkPos = new ChunkPosition(0, 0, 0);
            
            var chunkEntity = chunkManager.CreateChunk(chunkPos);
            var chunkComponent = chunkEntity.GetComponent<Chunk>();
            chunkComponent.IsLoaded = true;
            chunkComponent.State = ChunkState.Loaded;
            
            // 创建另一个未加载的区块
            var unloadedChunkPos = new ChunkPosition(1, 0, 0);
            chunkManager.CreateChunk(unloadedChunkPos);
            
            // Act
            var loadedChunks = chunkManager.GetLoadedChunks();
            
            // Assert
            Assert.Single(loadedChunks);
            Assert.Equal(chunkEntity.Id, loadedChunks[0].Id);
        }

        [Fact]
        public void ECSChunkManager_ShouldGetDirtyChunks()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            
            // 创建脏区块
            var dirtyChunkPos = new ChunkPosition(0, 0, 0);
            var dirtyChunkEntity = chunkManager.CreateChunk(dirtyChunkPos);
            var dirtyChunk = dirtyChunkEntity.GetComponent<Chunk>();
            dirtyChunk.IsDirty = true;
            
            // 创建干净区块
            var cleanChunkPos = new ChunkPosition(1, 0, 0);
            var cleanChunkEntity = chunkManager.CreateChunk(cleanChunkPos);
            var cleanChunk = cleanChunkEntity.GetComponent<Chunk>();
            cleanChunk.IsDirty = false;
            var cleanMesh = cleanChunkEntity.GetComponent<Mesh>();
            cleanMesh.IsDirty = false;
            
            // Act
            var dirtyChunks = chunkManager.GetDirtyChunks();
            
            // Assert
            Assert.Single(dirtyChunks);
            Assert.Equal(dirtyChunkEntity.Id, dirtyChunks[0].Id);
        }

        [Fact]
        public void ECSChunkManager_ShouldMarkChunkDirty()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            var chunkPos = new ChunkPosition(0, 0, 0);
            
            var chunkEntity = chunkManager.CreateChunk(chunkPos);
            var chunk = chunkEntity.GetComponent<Chunk>();
            var mesh = chunkEntity.GetComponent<Mesh>();
            
            // 重置脏标记
            chunk.IsDirty = false;
            mesh.IsDirty = false;
            
            // Act
            chunkManager.MarkChunkDirty(chunkPos);
            
            // Assert
            Assert.True(chunk.IsDirty);
            Assert.True(mesh.IsDirty);
        }

        [Fact]
        public void ECSChunkManager_ShouldMarkChunkLoaded()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager);
            var chunkPos = new ChunkPosition(0, 0, 0);
            
            var chunkEntity = chunkManager.CreateChunk(chunkPos);
            var chunk = chunkEntity.GetComponent<Chunk>();
            
            // 重置状态
            chunk.IsLoaded = false;
            chunk.State = ChunkState.Loading;
            
            // Act
            chunkManager.MarkChunkLoaded(chunkPos);
            
            // Assert
            Assert.True(chunk.IsLoaded);
            Assert.Equal(ChunkState.Loaded, chunk.State);
        }

        [Fact]
        public void ECSChunkManager_ShouldGetStats()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            var chunkManager = new ECSChunkManager(Store, blockManager, 10);
            
            // 创建一些区块
            for (int x = 0; x < 5; x++)
            {
                for (int z = 0; z < 5; z++)
                {
                    var chunkPos = new ChunkPosition(x, 0, z);
                    var chunkEntity = chunkManager.CreateChunk(chunkPos);
                    
                    // 标记一些为已加载
                    if (x < 3 && z < 3)
                    {
                        var chunk = chunkEntity.GetComponent<Chunk>();
                        chunk.IsLoaded = true;
                    }
                    
                    // 标记一些为脏
                    if (x == 0 && z == 0)
                    {
                        var chunk = chunkEntity.GetComponent<Chunk>();
                        chunk.IsDirty = true;
                    }
                }
            }
            
            // Act
            var stats = chunkManager.GetStats();
            
            // Assert
            Assert.Equal(25, stats.TotalChunks);
            Assert.Equal(9, stats.LoadedChunks); // 3x3 已加载
            Assert.Equal(1, stats.DirtyChunks); // 1个脏区块
            Assert.Equal(10, stats.RenderDistance);
        }

        [Fact]
        public void ECSBlockManager_ShouldOptimizeStorage()
        {
            // Arrange
            var blockManager = new ECSBlockManager(Store);
            
            // 创建一些方块，包括空气方块
            blockManager.SetBlock(BlockType.Grass, new Vector3(0, 0, 0));
            blockManager.SetBlock(BlockType.Stone, new Vector3(1, 0, 0));
            blockManager.SetBlock(BlockType.Air, new Vector3(2, 0, 0)); // 空气方块
            blockManager.SetBlock(BlockType.Dirt, new Vector3(3, 0, 0));
            
            var initialCount = blockManager.GetBlockCount();
            Assert.Equal(4, initialCount);
            
            // Act
            blockManager.OptimizeStorage();
            
            // Assert
            var finalCount = blockManager.GetBlockCount();
            Assert.Equal(3, finalCount); // 空气方块应该被移除
            
            // 验证剩余的方块
            Assert.NotNull(blockManager.GetBlock(new Vector3(0, 0, 0)));
            Assert.NotNull(blockManager.GetBlock(new Vector3(1, 0, 0)));
            Assert.Null(blockManager.GetBlock(new Vector3(2, 0, 0))); // 空气方块被移除
            Assert.NotNull(blockManager.GetBlock(new Vector3(3, 0, 0)));
        }
    }
}