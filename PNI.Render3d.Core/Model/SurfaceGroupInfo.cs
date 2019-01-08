using System;
using System.Numerics;

namespace PNI.Rendering.Harmony.Model
{
    public class SurfaceGroupInfo : IDisposable
    {
        private bool _disposed;

        public uint IndexOffset { get; set; }
        public uint IndexCount { get; set; }
        public Vector3 MinDimension { get; set; }
        public Vector3 MaxDimension { get; set; }
        public Vector4 UvDimension { get; set; }
        public SurfacePropertiesRoot Properties { get; set; }

        public SurfaceGroupInfo()
        {
            Properties = new SurfacePropertiesRoot();
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
                Properties.Dispose();
            }
            _disposed = true;
        }

        ~SurfaceGroupInfo()
        {
            Dispose(false);
        }
    }
}