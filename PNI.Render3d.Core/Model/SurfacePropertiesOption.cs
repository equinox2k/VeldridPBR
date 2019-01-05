using System.Runtime.Serialization;

namespace PNI.Rendering.Harmony
{
    [DataContract]
    public class SurfacePropertiesOption : SurfaceProperties
    {
        [DataMember]
        public string OptionName { get; set; }

        public SurfacePropertiesOption()
        {
        }

        public SurfacePropertiesOption(SurfacePropertiesOption surfacePropertiesOption) : base(surfacePropertiesOption)
        {
            OptionName = surfacePropertiesOption.OptionName;
        }

        public SurfacePropertiesOption(SurfaceProperties surfacePropertiesRoot)
        {
            Render = surfacePropertiesRoot.Render;
            DiffuseColor = surfacePropertiesRoot.DiffuseColor;
            SurfaceType = surfacePropertiesRoot.SurfaceType;
            EffectType = surfacePropertiesRoot.EffectType;
            Metalness = surfacePropertiesRoot.Metalness;
            Roughness = surfacePropertiesRoot.Roughness;
            Diffuse.File = surfacePropertiesRoot.Diffuse.File;
            Diffuse.Tile = surfacePropertiesRoot.Diffuse.Tile;
            Bumpmap.File = surfacePropertiesRoot.Bumpmap.File;
            Bumpmap.Tile = surfacePropertiesRoot.Bumpmap.Tile;
            Effect.File = surfacePropertiesRoot.Effect.File;
            Effect.Tile = surfacePropertiesRoot.Effect.Tile;
        }
    }
}
