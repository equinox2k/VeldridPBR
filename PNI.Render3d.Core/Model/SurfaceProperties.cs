using System;
using System.Runtime.Serialization;

namespace PNI.Rendering.Harmony
{
    [DataContract(Name = "SurfaceType")]
    public enum SurfaceTypeEnum
    {
        [EnumMember]
        Default = 0,
        [EnumMember]
        Glass = 1,
        [EnumMember]
        Photo1 = 2,
        [EnumMember]
        Photo2 = 3
    }

    [DataContract(Name = "EffectType")]
    public enum EffectTypeEnum
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Bleached = 1,
        [EnumMember]
        MagicBlackVertical = 2
    }

    [DataContract]
    public abstract class SurfaceProperties : IDisposable
    {
        private bool _disposed;

        [DataMember]
        public bool Render { get; set; }

        [DataMember]
        public string DiffuseColor { get; set; }

        [DataMember]
        public SurfaceTypeEnum SurfaceType { get; set; }

        [DataMember]
        public EffectTypeEnum EffectType { get; set; }

        [DataMember]
        public float Metalness { get; set; }

        [DataMember]
        public float Roughness { get; set; }

        [DataMember]
        public SurfaceTextureProperty Diffuse { get; internal set; }

        [DataMember]
        public SurfaceTextureProperty Bumpmap { get; internal set; }

        [DataMember]
        public SurfaceTextureProperty Effect { get; internal set; }

        protected SurfaceProperties()
        {
            Render = true;
            DiffuseColor = "ffffffff";
            SurfaceType = SurfaceTypeEnum.Default;
            EffectType = EffectTypeEnum.None;
            Metalness = 0.05f;
            Roughness = 0.5f;
            Diffuse = new SurfaceTextureProperty();
            Bumpmap = new SurfaceTextureProperty();
            Effect = new SurfaceTextureProperty();
        }

        protected SurfaceProperties(SurfaceProperties surfaceProperties)
        {
            Render = surfaceProperties.Render;
            DiffuseColor = surfaceProperties.DiffuseColor;
            SurfaceType = surfaceProperties.SurfaceType;
            EffectType = surfaceProperties.EffectType;
            Metalness = surfaceProperties.Metalness;
            Roughness = surfaceProperties.Roughness;
            Diffuse = new SurfaceTextureProperty(surfaceProperties.Diffuse);
            Bumpmap = new SurfaceTextureProperty(surfaceProperties.Bumpmap);
            Effect = new SurfaceTextureProperty(surfaceProperties.Effect);
        }

        ~SurfaceProperties()
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
                Diffuse.Dispose();
                Bumpmap.Dispose();
                Effect.Dispose();
            }
            _disposed = true;
        }
    }
}
