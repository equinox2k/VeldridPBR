using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace PNI.Render3d.Core
{
    public class Mesh : IDisposable
    {
        private bool _disposed;

        public DeviceBuffer VertexBuffer { get; private set; }
        public DeviceBuffer IndexBuffer { get; private set; }

        ~Mesh()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                if (VertexBuffer != null)
                {
                    VertexBuffer.Dispose();
                    VertexBuffer = null;
                }
                if (IndexBuffer != null)
                {
                    IndexBuffer.Dispose();
                    IndexBuffer = null;
                }
            }
            _disposed = true;
        }

        public Mesh(ResourceFactory resourceFactory, GraphicsDevice graphicsDevice, int startX, int startY, int width, int height, int cols, int rows)
        {
            var vertexList = new List<Vertex>();
            var indexList = new List<uint>();
            for (var tempY = 0; tempY < rows; tempY++)
            {
                var y = startY + tempY;
                for (var tempX = 0; tempX < cols; tempX++)
                {
                    var x = startX + tempX;
                    var vertex = new Vertex
                    {
                        PositionX = x / (cols - 1.0f) * width - width / 2.0f,
                        PositionY = y / (rows - 1.0f) * height - height / 2.0f,
                        PositionZ = 0,
                        TexCoordX = x / (cols - 1.0f),
                        TexCoordY = 1.0f - y / (rows - 1.0f),
                        NormalX = 0.0f,
                        NormalY = 0.0f,
                        NormalZ = 1.0f,
                        TangentX = 1.0f,
                        TangentY = 0.0f,
                        TangentZ = 0.0f
                    };

                    //vertex.PositionY = - vertex.PositionY;

                    vertexList.Add(vertex);
                    if (x >= cols - 1 || y >= rows - 1)
                    {
                        continue;
                    }
                    indexList.Add((uint)(y * cols + x + 1));
                    indexList.Add((uint)((y + 1) * cols + x + 1));
                    indexList.Add((uint)(y * cols + x));
                    indexList.Add((uint)((y + 1) * cols + x + 1));
                    indexList.Add((uint)((y + 1) * cols + x));
                    indexList.Add((uint)(y * cols + x));
                }
            }

            var vertices = vertexList.ToArray();
            var indices = indexList.ToArray();

            VertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.SizeInBytes * vertices.Length), BufferUsage.VertexBuffer));
            graphicsDevice.UpdateBuffer(VertexBuffer, 0, vertices);
            IndexBuffer = resourceFactory.CreateBuffer(new BufferDescription(sizeof(uint) * (uint)indices.Length, BufferUsage.IndexBuffer));
            graphicsDevice.UpdateBuffer(IndexBuffer, 0, indices);
        }
    }
}
