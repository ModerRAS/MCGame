using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MCGame.Blocks;
using MCGame.Core;
using MCGame.Chunks;
using System;

namespace MCGame.Rendering
{
    /// <summary>
    /// 区块网格类
    /// 存储区块的顶点数据和索引数据
    /// 简化实现：使用简单的顶点缓冲区和索引缓冲区
    /// </summary>
    public class ChunkMesh : IDisposable
    {
        // 顶点和索引数据
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private VertexPositionNormalTexture[] _vertices;
        private ushort[] _indices;

        // 网格属性
        public int VertexCount => _vertices?.Length ?? 0;
        public int IndexCount => _indices?.Length ?? 0;
        public int TriangleCount => IndexCount / 3;
        public bool IsDisposed { get; private set; }

        // 性能优化：避免频繁重新分配
        private const int MAX_VERTICES = 65536; // 16位索引限制
        private const int MAX_INDICES = 65536;

        public ChunkMesh(GraphicsDevice graphicsDevice)
        {
            _vertices = new VertexPositionNormalTexture[1024];
            _indices = new ushort[1024];
        }

        /// <summary>
        /// 添加顶点
        /// </summary>
        public void AddVertex(Vector3 position, Vector3 normal, Vector2 textureCoord)
        {
            if (_vertices.Length >= MAX_VERTICES)
            {
                throw new InvalidOperationException("Vertex buffer overflow");
            }

            // 扩展数组如果需要
            if (_vertices.Length <= VertexCount)
            {
                Array.Resize(ref _vertices, Math.Min(_vertices.Length * 2, MAX_VERTICES));
            }

            _vertices[VertexCount] = new VertexPositionNormalTexture(position, normal, textureCoord);
        }

        /// <summary>
        /// 添加三角形
        /// </summary>
        public void AddTriangle(ushort index1, ushort index2, ushort index3)
        {
            if (_indices.Length + 3 > MAX_INDICES)
            {
                throw new InvalidOperationException("Index buffer overflow");
            }

            // 扩展数组如果需要
            if (_indices.Length <= IndexCount + 3)
            {
                Array.Resize(ref _indices, Math.Min(_indices.Length * 2, MAX_INDICES));
            }

            _indices[IndexCount] = index1;
            _indices[IndexCount + 1] = index2;
            _indices[IndexCount + 2] = index3;
        }

        /// <summary>
        /// 添加四边形（两个三角形）
        /// </summary>
        public void AddQuad(ushort[] indices)
        {
            if (indices.Length != 4)
            {
                throw new ArgumentException("Quad must have exactly 4 indices");
            }

            // 添加两个三角形
            AddTriangle(indices[0], indices[1], indices[2]);
            AddTriangle(indices[0], indices[2], indices[3]);
        }

        /// <summary>
        /// 构建GPU缓冲区
        /// </summary>
        public void BuildBuffer(GraphicsDevice graphicsDevice)
        {
            if (VertexCount == 0 || IndexCount == 0)
            {
                return;
            }

            // 创建顶点缓冲区
            _vertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionNormalTexture),
                VertexCount,
                BufferUsage.WriteOnly
            );

            // 创建索引缓冲区
            _indexBuffer = new IndexBuffer(
                graphicsDevice,
                IndexElementSize.SixteenBits,
                IndexCount,
                BufferUsage.WriteOnly
            );

            // 上传数据到GPU
            _vertexBuffer.SetData(_vertices, 0, VertexCount);
            _indexBuffer.SetData(_indices, 0, IndexCount);
        }

        /// <summary>
        /// 渲染网格
        /// </summary>
        public void Draw(GraphicsDevice graphicsDevice, Effect effect)
        {
            if (IsDisposed || VertexCount == 0 || IndexCount == 0)
            {
                return;
            }

            // 设置缓冲区
            graphicsDevice.SetVertexBuffer(_vertexBuffer);
            graphicsDevice.Indices = _indexBuffer;

            // 应用着色器
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    TriangleCount
                );
            }
        }

        /// <summary>
        /// 清空网格数据
        /// </summary>
        public void Clear()
        {
            // 重置计数器
            Array.Clear(_vertices, 0, VertexCount);
            Array.Clear(_indices, 0, IndexCount);
        }

        /// <summary>
        /// 获取当前顶点数组的起始索引
        /// </summary>
        public ushort GetCurrentVertexIndex()
        {
            return (ushort)VertexCount;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();

            _vertexBuffer = null;
            _indexBuffer = null;
            _vertices = null;
            _indices = null;

            IsDisposed = true;
        }
    }

    /// <summary>
    /// 网格生成器接口
    /// </summary>
    public interface IMeshGenerator
    {
        ChunkMesh GenerateMesh(Chunk chunk);
    }

    /// <summary>
    /// 材质结构
    /// </summary>
    public struct Material
    {
        public Texture2D Texture { get; set; }
        public Effect Effect { get; set; }
        public bool IsTransparent { get; set; }
        public string Name { get; set; }

        public static Material Default => new Material
        {
            Texture = null,
            Effect = null,
            IsTransparent = false,
            Name = "Default"
        };
    }

    /// <summary>
    /// 渲染批次结构
    /// 用于优化渲染顺序
    /// </summary>
    public struct RenderBatch
    {
        public ChunkMesh Mesh { get; set; }
        public Material Material { get; set; }
        public Matrix Transform { get; set; }
        public float Distance { get; set; }
        public BoundingBox Bounds { get; set; }

        public static RenderBatch Empty => new RenderBatch
        {
            Mesh = null,
            Material = Material.Default,
            Transform = Matrix.Identity,
            Distance = 0f,
            Bounds = new BoundingBox()
        };
    }

    /// <summary>
    /// 渲染统计信息
    /// </summary>
    public struct RenderStats
    {
        public int DrawCalls { get; set; }
        public int Triangles { get; set; }
        public int VisibleChunks { get; set; }
        public int TotalVertices { get; set; }
        public float FrameTime { get; set; }

        public void Reset()
        {
            DrawCalls = 0;
            Triangles = 0;
            VisibleChunks = 0;
            TotalVertices = 0;
            FrameTime = 0;
        }
    }

    /// <summary>
    /// 渲染阶段接口
    /// </summary>
    public interface IRenderPass
    {
        string Name { get; }
        void Execute(GraphicsDevice graphicsDevice, List<RenderBatch> batches);
        void Begin(GraphicsDevice graphicsDevice);
        void End(GraphicsDevice graphicsDevice);
    }

    /// <summary>
    /// 基础渲染阶段
    /// </summary>
    public abstract class BaseRenderPass : IRenderPass
    {
        public abstract string Name { get; }

        public virtual void Begin(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.Transparent);
        }

        public abstract void Execute(GraphicsDevice graphicsDevice, List<RenderBatch> batches);

        public virtual void End(GraphicsDevice graphicsDevice)
        {
            // 默认不做任何操作
        }
    }

    /// <summary>
    /// 不透明物体渲染阶段
    /// </summary>
    public class OpaquePass : BaseRenderPass
    {
        public override string Name => "Opaque";

        public override void Execute(GraphicsDevice graphicsDevice, List<RenderBatch> batches)
        {
            // 按距离排序（从前到后）
            batches.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            foreach (var batch in batches)
            {
                if (!batch.Material.IsTransparent && batch.Mesh != null)
                {
                    batch.Mesh.Draw(graphicsDevice, batch.Material.Effect);
                }
            }
        }
    }

    /// <summary>
    /// 透明物体渲染阶段
    /// </summary>
    public class TransparentPass : BaseRenderPass
    {
        public override string Name => "Transparent";

        public override void Execute(GraphicsDevice graphicsDevice, List<RenderBatch> batches)
        {
            // 按距离排序（从后到前）
            batches.Sort((a, b) => b.Distance.CompareTo(a.Distance));

            foreach (var batch in batches)
            {
                if (batch.Material.IsTransparent && batch.Mesh != null)
                {
                    batch.Mesh.Draw(graphicsDevice, batch.Material.Effect);
                }
            }
        }
    }
}