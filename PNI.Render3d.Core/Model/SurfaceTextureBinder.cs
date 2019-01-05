using System;
using System.Runtime.Serialization;

namespace PNI.Rendering.Harmony
{
    [DataContract]
    public class SurfaceTextureBinder : IDisposable
    {
        private bool _disposed;

        private int _id;

        public int Id => _id;

        public SurfaceTextureBinder()
        {
            _id = -1;
        }

        ~SurfaceTextureBinder()
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
                DeleteTexture();
            }
            _disposed = true;
        }

        private void DeleteTexture()
        {
            if (_id < 0)
            {
                return;
            }
        }

        public void BindTexture(SurfaceTexture surfaceTexture)
        {
            DeleteTexture();

            if (surfaceTexture == null)
            {
                return;
            }

        }

        public void BindTextureCube(SurfaceTexture[] surfaceTextures)
        {
           
        }

    }
}
