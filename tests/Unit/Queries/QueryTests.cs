using Microsoft.Xna.Framework;
using Xunit;
using MCGame.ECS.Components;
using MCGame.Core;
using Position = MCGame.ECS.Components.Position;

namespace MCGame.Tests.Unit.Queries
{
    /// <summary>
    /// 查询系统单元测试
    /// 测试Friflo ECS的查询功能
    /// </summary>
    public class QueryTests : TestBase
    {
        [Fact]
        public void Query_ShouldFindEntitiesWithSingleComponent()
        {
            // Arrange
            var entity1 = CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0));
            var entity2 = CreateTestBlockEntity(BlockType.Stone, new Vector3(2, 0, 0));
            var entity3 = CreateTestPlayerEntity(new Vector3(0, 64, 0));
            
            // Act
            var blockQuery = Store.Query<Block>();
            
            // Assert
            AssertQueryCount<Block>(blockQuery, 2);
            
            var entities = blockQuery.Entities.ToList();
            Assert.Contains(entity1, entities);
            Assert.Contains(entity2, entities);
            Assert.DoesNotContain(entity3, entities);
        }

        [Fact]
        public void Query_ShouldFindEntitiesWithMultipleComponents()
        {
            // Arrange
            var blockEntity1 = CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0));
            var blockEntity2 = CreateTestBlockEntity(BlockType.Stone, new Vector3(2, 0, 0));
            var playerEntity = CreateTestPlayerEntity(new Vector3(0, 64, 0));
            
            // 创建一个只有Position的实体
            var positionOnlyEntity = Store.CreateEntity(new Position(new Vector3(5, 0, 0)));
            
            // Act
            var blockPositionQuery = Store.Query<Block, Position>();
            
            // Assert
            Assert.Equal(2, blockPositionQuery.Count);
            
            var entities = blockPositionQuery.Entities.ToList();
            Assert.Contains(blockEntity1, entities);
            Assert.Contains(blockEntity2, entities);
            Assert.DoesNotContain(playerEntity, entities);
            Assert.DoesNotContain(positionOnlyEntity, entities);
        }

        [Fact]
        public void Query_ShouldFindEntitiesWithThreeComponents()
        {
            // Arrange
            var entities = new List<Friflo.Engine.ECS.Entity>();
            for (int i = 0; i < 10; i++)
            {
                var entity = CreateTestBlockEntity(BlockType.Grass, new Vector3(i, 0, 0));
                entities.Add(entity);
            }
            
            // Act
            var complexQuery = Store.Query<Block, Position, Visibility>();
            
            // Assert
            Assert.Equal(10, complexQuery.Count);
            
            var foundEntities = complexQuery.Entities.ToList();
            Assert.Equal(10, foundEntities.Count);
            
            // 验证所有实体都有正确的组件
            foreach (var entity in foundEntities)
            {
                Assert.True(entity.HasComponent<Block>());
                Assert.True(entity.HasComponent<Position>());
                Assert.True(entity.HasComponent<Visibility>());
            }
        }

        [Fact]
        public void Query_ShouldReturnEmptyWhenNoMatches()
        {
            // Arrange
            CreateTestPlayerEntity(new Vector3(0, 64, 0));
            
            // Act
            var blockQuery = Store.Query<Block>();
            
            // Assert
            Assert.Equal(0, blockQuery.Count);
            Assert.Empty(blockQuery.Entities);
        }

        [Fact]
        public void Query_ShouldUpdateAfterEntityCreation()
        {
            // Arrange
            var initialQuery = Store.Query<Block>();
            Assert.Equal(0, initialQuery.Count);
            
            // Act
            var entity = CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0));
            var updatedQuery = Store.Query<Block>();
            
            // Assert
            Assert.Equal(1, updatedQuery.Count);
            Assert.Contains(entity, updatedQuery.Entities.ToList());
        }

        [Fact]
        public void Query_ShouldFilterByComponentValues()
        {
            // Arrange
            CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0));
            CreateTestBlockEntity(BlockType.Stone, new Vector3(2, 0, 0));
            CreateTestBlockEntity(BlockType.Dirt, new Vector3(3, 0, 0));
            CreateTestBlockEntity(BlockType.Grass, new Vector3(4, 0, 0));
            
            // Act
            var blockQuery = Store.Query<Block>();
            var grassBlocks = blockQuery.Entities
                .Where(e => e.GetComponent<Block>().Type == BlockType.Grass)
                .ToList();
            
            // Assert
            Assert.Equal(2, grassBlocks.Count);
            Assert.All(grassBlocks, e => Assert.Equal(BlockType.Grass, e.GetComponent<Block>().Type));
        }

        [Fact]
        public void Query_ShouldFilterByPosition()
        {
            // Arrange
            CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0));
            CreateTestBlockEntity(BlockType.Stone, new Vector3(10, 0, 0));
            CreateTestBlockEntity(BlockType.Dirt, new Vector3(20, 0, 0));
            
            // Act
            var positionQuery = Store.Query<Position>();
            var nearbyBlocks = positionQuery.Entities
                .Where(e => 
                {
                    var pos = e.GetComponent<Position>().Value;
                    return pos.X <= 5; // 距离原点5个单位以内
                })
                .ToList();
            
            // Assert
            Assert.Single(nearbyBlocks);
            Assert.Equal(1, nearbyBlocks[0].GetComponent<Position>().Value.X);
        }

        [Fact]
        public void Query_ShouldHandleLargeEntitySets()
        {
            // Arrange
            var entityCount = 1000;
            for (int i = 0; i < entityCount; i++)
            {
                CreateTestBlockEntity(BlockType.Grass, new Vector3(i, 0, 0));
            }
            
            // Act
            var blockQuery = Store.Query<Block>();
            var positionQuery = Store.Query<Position>();
            var visibilityQuery = Store.Query<Visibility>();
            
            // Assert
            Assert.Equal(entityCount, blockQuery.Count);
            Assert.Equal(entityCount, positionQuery.Count);
            Assert.Equal(entityCount, visibilityQuery.Count);
            
            // 验证查询性能（快速操作）
            var entities = blockQuery.Entities.ToList();
            Assert.Equal(entityCount, entities.Count);
        }

        [Fact]
        public void Query_ShouldSupportMultipleQueryTypes()
        {
            // Arrange
            var blockEntity = CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0));
            var playerEntity = CreateTestPlayerEntity(new Vector3(0, 64, 0));
            var chunkEntity = CreateTestChunkEntity(new ChunkPosition(0, 0, 0));
            
            // Act
            var blockQuery = Store.Query<Block>();
            var playerQuery = Store.Query<Player>();
            var chunkQuery = Store.Query<Chunk>();
            var positionQuery = Store.Query<Position>();
            
            // Assert
            Assert.Equal(1, blockQuery.Count);
            Assert.Equal(1, playerQuery.Count);
            Assert.Equal(1, chunkQuery.Count);
            Assert.Equal(3, positionQuery.Count); // 所有实体都有Position
            
            Assert.Contains(blockEntity, blockQuery.Entities.ToList());
            Assert.Contains(playerEntity, playerQuery.Entities.ToList());
            Assert.Contains(chunkEntity, chunkQuery.Entities.ToList());
        }

        [Fact]
        public void Query_ShouldWorkWithEntityIteration()
        {
            // Arrange
            var expectedPositions = new List<Vector3>
            {
                new Vector3(1, 0, 0),
                new Vector3(2, 0, 0),
                new Vector3(3, 0, 0),
                new Vector3(4, 0, 0),
                new Vector3(5, 0, 0)
            };
            
            foreach (var pos in expectedPositions)
            {
                CreateTestBlockEntity(BlockType.Grass, pos);
            }
            
            // Act
            var positionQuery = Store.Query<Position>();
            var actualPositions = positionQuery.Entities
                .Select(e => e.GetComponent<Position>().Value)
                .OrderBy(p => p.X)
                .ToList();
            
            // Assert
            Assert.Equal(expectedPositions.Count, actualPositions.Count);
            for (int i = 0; i < expectedPositions.Count; i++)
            {
                Assert.Equal(expectedPositions[i], actualPositions[i]);
            }
        }

        [Fact]
        public void Query_ShouldHandleComponentTypeQueries()
        {
            // Arrange
            var entities = new List<Friflo.Engine.ECS.Entity>
            {
                CreateTestBlockEntity(BlockType.Grass, new Vector3(1, 0, 0)),
                CreateTestPlayerEntity(new Vector3(0, 64, 0)),
                CreateTestChunkEntity(new ChunkPosition(0, 0, 0))
            };
            
            // Act
            var allEntities = Store.Query();
            var blockEntities = Store.Query<Block>();
            var playerEntities = Store.Query<Player>();
            var chunkEntities = Store.Query<Chunk>();
            
            // Assert
            Assert.Equal(entities.Count, allEntities.Count);
            Assert.Equal(1, blockEntities.Count);
            Assert.Equal(1, playerEntities.Count);
            Assert.Equal(1, chunkEntities.Count);
        }

        [Fact]
        public void Query_ShouldSupportComplexFiltering()
        {
            // Arrange
            // 创建可见的方块
            for (int i = 0; i < 5; i++)
            {
                var entity = CreateTestBlockEntity(BlockType.Grass, new Vector3(i, 0, 0));
                var visibility = entity.GetComponent<Visibility>();
                visibility.IsVisible = true;
            }
            
            // 创建不可见的方块
            for (int i = 5; i < 10; i++)
            {
                var entity = CreateTestBlockEntity(BlockType.Stone, new Vector3(i, 0, 0));
                var visibility = entity.GetComponent<Visibility>();
                visibility.IsVisible = false;
            }
            
            // Act
            var blockQuery = Store.Query<Block, Position, Visibility>();
            var visibleGrassBlocks = blockQuery.Entities
                .Where(e => 
                {
                    var block = e.GetComponent<Block>();
                    var visibility = e.GetComponent<Visibility>();
                    return block.Type == BlockType.Grass && visibility.IsVisible;
                })
                .ToList();
            
            var visibleBlocksNearOrigin = blockQuery.Entities
                .Where(e => 
                {
                    var position = e.GetComponent<Position>();
                    var visibility = e.GetComponent<Visibility>();
                    return visibility.IsVisible && position.Value.X < 3;
                })
                .ToList();
            
            // Assert
            Assert.Equal(5, visibleGrassBlocks.Count);
            Assert.Equal(3, visibleBlocksNearOrigin.Count);
        }

        [Fact]
        public void Query_ShouldMaintainConsistency()
        {
            // Arrange
            var initialCount = 10;
            var entities = new List<Friflo.Engine.ECS.Entity>();
            
            for (int i = 0; i < initialCount; i++)
            {
                entities.Add(CreateTestBlockEntity(BlockType.Grass, new Vector3(i, 0, 0)));
            }
            
            // Act - 验证初始状态
            var query = Store.Query<Block>();
            Assert.Equal(initialCount, query.Count);
            
            // 添加更多实体
            for (int i = initialCount; i < initialCount + 5; i++)
            {
                entities.Add(CreateTestBlockEntity(BlockType.Stone, new Vector3(i, 0, 0)));
            }
            
            // Assert - 验证更新后的状态
            Assert.Equal(initialCount + 5, query.Count);
            
            // 验证所有实体都可以通过查询找到
            var foundEntities = query.Entities.ToList();
            Assert.Equal(initialCount + 5, foundEntities.Count);
            
            foreach (var entity in entities)
            {
                Assert.Contains(entity, foundEntities);
            }
        }
    }
}