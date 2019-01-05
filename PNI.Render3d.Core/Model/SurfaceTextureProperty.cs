using System.Runtime.Serialization;

namespace PNI.Rendering.Harmony
{
    [DataContract]
    public class SurfaceTextureProperty : SurfaceTextureBinder
    {
        public bool NeedsRefresh { get; private set; }

        public SurfaceTexture Texture { get; set; }

        private string _file;

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

        private bool _tile;

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
            Texture = null;
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

        public void BindTexture() 
        {
            BindTexture(Texture);
            Texture = null;
        }
    }
}
