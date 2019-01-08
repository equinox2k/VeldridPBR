using System;
using System.Numerics;
using PNI.Rendering.Harmony;
using Veldrid;

namespace PNI.Rendering.Harmony.Model
{
    public class SurfaceGroups : IDisposable
    {
        private bool _disposed;

        public DeviceBuffer VertexBuffer { get; private set; }
        public DeviceBuffer IndexBuffer { get; private set; }

        public float Scale { get; set; }
        public Vector3 MinDimension { get; set; }
        public Vector3 MaxDimension { get; set; }
        public uint[] Indices { get; set; }
        public Vertex[] Vertices { get; set; }
        public SurfaceGroupInfo[] SurfaceGroupInfos { get; set; }
        public ModelResource[] ModelResources { get; set; }

        public SurfaceGroups()
        {
            Scale = 1.0f;
            SurfaceGroupInfos = new SurfaceGroupInfo[] { };
            Indices = new uint[] { };
            Vertices = new Vertex[] { };
        }

        public void CreateBuffers(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            DeleteBuffers();
            VertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.SizeInBytes * Vertices.Length), BufferUsage.VertexBuffer));
            graphicsDevice.UpdateBuffer(VertexBuffer, 0, Vertices);
            IndexBuffer = resourceFactory.CreateBuffer(new BufferDescription(sizeof(uint) * (uint)Indices.Length, BufferUsage.IndexBuffer));
            graphicsDevice.UpdateBuffer(IndexBuffer, 0, Indices);
        }

        public void DeleteBuffers()
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

        ~SurfaceGroups()
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
                foreach (var surfaceGroupInfo in SurfaceGroupInfos)
                {
                    surfaceGroupInfo.Dispose();
                }
                DeleteBuffers();
            }            
            _disposed = true;
        }
    }
}
