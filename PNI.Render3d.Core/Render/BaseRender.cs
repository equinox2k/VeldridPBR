using System;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;

namespace PNI.Rendering.Harmony.Render
{
    public abstract class BaseRender
    {
        protected ImageSharpCubemapTexture LoadCube(string name)
        {
            var posX = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"PNI.Rendering.Harmony.Resources.{name}_posx.jpg"));
            var negX = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"PNI.Rendering.Harmony.Resources.{name}_negx.jpg"));
            var posY = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"PNI.Rendering.Harmony.Resources.{name}_posy.jpg"));
            var negY = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"PNI.Rendering.Harmony.Resources.{name}_negY.jpg"));
            var posZ = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"PNI.Rendering.Harmony.Resources.{name}_posZ.jpg"));
            var negZ = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"PNI.Rendering.Harmony.Resources.{name}_negZ.jpg"));
            return new ImageSharpCubemapTexture(posX, negX, posY, negY, posZ, negZ);
        }

        protected ShaderDescription LoadShader(string set, ShaderStages stage, string entryPoint)
        {
            string name = $"PNI.Rendering.Harmony.Shaders.{set}.{stage.ToString().Substring(0, 4).ToLower()}.spv";
            return new ShaderDescription(stage, ResourceLoader.GetEmbeddedResourceBytes(name), entryPoint);
        }

        public virtual void Resize()
        {

        }
    }
}
