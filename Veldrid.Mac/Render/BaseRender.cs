using System;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;

namespace VeldridNSViewExample.Render
{
    public abstract class BaseRender
    {
        protected ImageSharpCubemapTexture LoadCube(string name)
        {
            var posX = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_posx.jpg"));
            var negX = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_negx.jpg"));
            var posY = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_posy.jpg"));
            var negY = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_negY.jpg"));
            var posZ = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_posZ.jpg"));
            var negZ = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_negZ.jpg"));
            return new ImageSharpCubemapTexture(posX, negX, posY, negY, posZ, negZ);
        }

        protected ShaderDescription LoadShader(string set, ShaderStages stage, string entryPoint)
        {
            string name = $"VeldridNSViewExample.Shaders.{set}.{stage.ToString().Substring(0, 4).ToLower()}.spv";
            return new ShaderDescription(stage, ResourceLoader.GetEmbeddedResourceBytes(name), entryPoint);
        }
    }
}
