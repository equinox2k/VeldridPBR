using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Veldrid;

namespace PNI.Rendering.Harmony.Model
{
    [DataContract]
    public class SurfaceTextureProperty : IDisposable
    {
        private bool _disposed;

        private TextureView _textureView;
        private string _file;
        private bool _tile;

        public bool NeedsRefresh { get; private set; }

        [JsonIgnore]
        public TextureView TextureView
        {
            get => _textureView;
            set
            {
                if (_textureView != null)
                {
                    _textureView.Dispose();
                }
                _textureView = value;
                NeedsRefresh = true;
            }
        }

        [DataMember]
        public string File
        {
            get => _file;
            set
            {
                _file = value;
                NeedsRefresh = true;
            }
        }

        [DataMember]
        public bool Tile
        {
            get => _tile;
            set
            {
                _tile = value;
                NeedsRefresh = true;
            }
        }

        public SurfaceTextureProperty()
        {
            TextureView = null;
            _file = "";
            _tile = false;
            NeedsRefresh = false;
        }

        public SurfaceTextureProperty(SurfaceTextureProperty surfaceTextureProperty)
        {
            File = surfaceTextureProperty.File;
            Tile = surfaceTextureProperty.Tile;
        }

        public void ResetNeedsRefresh()
        {
            NeedsRefresh = false;
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
                _textureView.Dispose();
            }
            _disposed = true;
        }
    }
}
