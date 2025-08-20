using Microsoft.Xna.Framework;
using Friflo.Engine.ECS;
using MCGame.ECS.Components;
using MCGame.Blocks;
using MCGame.Core;
using System.Collections.Generic;

namespace MCGame.ECS.Managers
{
    /// <summary>
    /// ECS方块管理器
    /// 使用ECS系统管理方块实体，提供高性能的方块操作
    /// 简化实现：专注于方块的ECS化管理和批量操作
    /// </summary>
    public class ECSBlockManager
    {
        private readonly EntityStore _store;
        
        /// <summary>
        /// 获取实体存储（供其他管理器使用）
        /// </summary>
        public EntityStore Store => _store;
        private readonly ArchetypeQuery _blockQuery;
        private readonly ArchetypeQuery _chunkBlockQuery;
        private readonly Dictionary<ChunkPosition, List<Entity>> _chunkBlocks;
        private readonly Dictionary<Vector3, Entity> _blockEntities;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ECSBlockManager(EntityStore store)
        {
            _store = store;
            _blockQuery = _store.Query<Block, MCGame.ECS.Components.Position>();
            _chunkBlockQuery = _store.Query<Block, MCGame.ECS.Components.Position, Chunk>();
            _chunkBlocks = new Dictionary<ChunkPosition, List<Entity>>();
            _blockEntities = new Dictionary<Vector3, Entity>();
        }

        /// <summary>
        /// 设置方块
        /// </summary>
        public Entity SetBlock(BlockType blockType, Vector3 position)
        {
            var chunkPos = new ChunkPosition(
                (int)Math.Floor(position.X / 16f),
                0,
                (int)Math.Floor(position.Z / 16f)
            );

            // 检查是否已存在方块
            if (_blockEntities.TryGetValue(position, out var existingEntity))
            {
                // 更新现有方块
                var blockComponent = existingEntity.GetComponent<Block>();
                blockComponent.Type = blockType;
                blockComponent.Data = new BlockData(blockType);
                
                // 标记为需要重新渲染
                if (existingEntity.TryGetComponent<Visibility>(out var visibility))
                {
                    visibility.IsVisible = true;
                }
                
                return existingEntity;
            }

            // 创建新的方块实体
            var newEntity = _store.CreateEntity(
                new Block(blockType),
                new MCGame.ECS.Components.Position(position),
                new Visibility(true),
                new Collider(new BoundingBox(position, position + Vector3.One)),
                new Lighting(15)
            );

            // 添加到字典
            _blockEntities[position] = newEntity;

            // 添加到区块字典
            if (!_chunkBlocks.TryGetValue(chunkPos, out var chunkEntityList))
            {
                chunkEntityList = new List<Entity>();
                _chunkBlocks[chunkPos] = chunkEntityList;
            }
            chunkEntityList.Add(newEntity);

            return newEntity;
        }

        /// <summary>
        /// 获取方块
        /// </summary>
        public BlockType? GetBlock(Vector3 position)
        {
            if (_blockEntities.TryGetValue(position, out var entity))
            {
                return entity.GetComponent<Block>().Type;
            }
            return null;
        }

        /// <summary>
        /// 移除方块
        /// </summary>
        public bool RemoveBlock(Vector3 position)
        {
            if (_blockEntities.TryGetValue(position, out var entity))
            {
                // 从字典中移除
                _blockEntities.Remove(position);

                // 从区块字典中移除
                var chunkPos = new ChunkPosition(
                    (int)Math.Floor(position.X / 16f),
                    0,
                    (int)Math.Floor(position.Z / 16f)
                );
                if (_chunkBlocks.TryGetValue(chunkPos, out var chunkEntityList))
                {
                    chunkEntityList.Remove(entity);
                }

                // TODO: 找到Friflo ECS的正确删除方法
              // _store.DeleteEntity(entity.Id); // 方法不存在
                return true;
            }
            return false;
        }

        /// <summary>
        /// 批量设置方块（高性能）
        /// </summary>
        public void SetBlocksBatch(BlockType[] blockTypes, Vector3[] positions)
        {
            var newEntities = new List<Entity>();

            for (int i = 0; i < blockTypes.Length; i++)
            {
                var position = positions[i];
                var chunkPos = new ChunkPosition(
                    (int)Math.Floor(position.X / 16f),
                    0,
                    (int)Math.Floor(position.Z / 16f)
                );

                // 如果已存在方块，先删除
                if (_blockEntities.TryGetValue(position, out var existingEntity))
                {
                    // TODO: 找到Friflo ECS的正确删除方法
                      // _store.DeleteEntity(existingEntity.Id); // 方法不存在
                    _blockEntities.Remove(position);
                }

                // 创建新实体
                var entity = _store.CreateEntity(
                    new Block(blockTypes[i]),
                    new MCGame.ECS.Components.Position(position),
                    new Visibility(true),
                    new Collider(new BoundingBox(position, position + Vector3.One)),
                    new Lighting(15)
                );

                _blockEntities[position] = entity;
                newEntities.Add(entity);

                // 添加到区块字典
                if (!_chunkBlocks.TryGetValue(chunkPos, out var chunkEntityList))
                {
                    chunkEntityList = new List<Entity>();
                    _chunkBlocks[chunkPos] = chunkEntityList;
                }
                chunkEntityList.Add(entity);
            }
        }

        /// <summary>
        /// 获取区块内的所有方块
        /// </summary>
        public Entity[] GetBlocksInChunk(ChunkPosition chunkPos)
        {
            if (_chunkBlocks.TryGetValue(chunkPos, out var entities))
            {
                return entities.ToArray();
            }
            return new Entity[0];
        }

        /// <summary>
        /// 获取指定范围内的方块
        /// </summary>
        public Entity[] GetBlocksInRange(Vector3 center, float radius)
        {
            var result = new List<Entity>();
            var radiusSquared = radius * radius;

            foreach (var block in _blockQuery.Entities)
            {
                var position = block.GetComponent<MCGame.ECS.Components.Position>().Value;
                var distanceSquared = Vector3.DistanceSquared(position, center);
                
                if (distanceSquared <= radiusSquared)
                {
                    result.Add(block);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 射线检测
        /// </summary>
        public Entity? Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            Entity closestEntity = default;
            float closestDistance = float.MaxValue;

            foreach (var block in _blockQuery.Entities)
            {
                var position = block.GetComponent<MCGame.ECS.Components.Position>().Value;
                var bounds = new BoundingBox(position, position + Vector3.One);

                var ray = new Ray(origin, direction);
                var intersects = bounds.Intersects(ray);
              if (intersects.HasValue)
                {
                    var distance = Vector3.Distance(origin, position);
                    if (distance < closestDistance && distance <= maxDistance)
                    {
                        closestDistance = distance;
                        closestEntity = block;
                    }
                }
            }

            return closestEntity;
        }

        /// <summary>
        /// 获取所有方块实体
        /// </summary>
        public Entity[] GetAllBlocks()
        {
            var blocks = new List<Entity>();
            foreach (var block in _blockQuery.Entities)
            {
                blocks.Add(block);
            }
            return blocks.ToArray();
        }

        /// <summary>
        /// 获取方块数量
        /// </summary>
        public int GetBlockCount()
        {
            return _blockQuery.Count;
        }

        /// <summary>
        /// 获取区块数量
        /// </summary>
        public int GetChunkCount()
        {
            return _chunkBlocks.Count;
        }

        /// <summary>
        /// 清空所有方块
        /// </summary>
        public void ClearAll()
        {
            foreach (var block in _blockQuery.Entities)
            {
                // TODO: 找到Friflo ECS的正确删除方法
                  // _store.DeleteEntity(block.Id); // 方法不存在
            }
            
            _blockEntities.Clear();
            _chunkBlocks.Clear();
        }

        /// <summary>
        /// 优化存储（移除空气方块）
        /// </summary>
        public void OptimizeStorage()
        {
            var positionsToRemove = new List<Vector3>();

            foreach (var block in _blockQuery.Entities)
            {
                var blockComponent = block.GetComponent<Block>();
                if (blockComponent.Type == BlockType.Air)
                {
                    var position = block.GetComponent<MCGame.ECS.Components.Position>().Value;
                    positionsToRemove.Add(position);
                    // TODO: 找到Friflo ECS的正确删除方法
                  // _store.DeleteEntity(block.Id); // 方法不存在
                }
            }

            foreach (var position in positionsToRemove)
            {
                _blockEntities.Remove(position);
                
                var chunkPos = new ChunkPosition(
                    (int)Math.Floor(position.X / 16f),
                    0,
                    (int)Math.Floor(position.Z / 16f)
                );
                if (_chunkBlocks.TryGetValue(chunkPos, out var chunkEntityList))
                {
                    chunkEntityList.RemoveAll(e => e.GetComponent<MCGame.ECS.Components.Position>().Value == position);
                }
            }
        }

        /// <summary>
        /// 获取内存使用统计
        /// </summary>
        public MemoryStats GetMemoryStats()
        {
            return new MemoryStats
            {
                TotalBlocks = _blockQuery.Count,
                TotalChunks = _chunkBlocks.Count,
                DictionaryEntries = _blockEntities.Count,
                AverageBlocksPerChunk = _chunkBlocks.Count > 0 ? 
                    (float)_blockEntities.Count / _chunkBlocks.Count : 0
            };
        }

        /// <summary>
        /// 内存统计信息
        /// </summary>
        public struct MemoryStats
        {
            public int TotalBlocks;
            public int TotalChunks;
            public int DictionaryEntries;
            public float AverageBlocksPerChunk;
        }
    }
}