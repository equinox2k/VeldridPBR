using System;
using System.Numerics;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;
using Veldrid.SPIRV;

namespace VeldridNSViewExample
{
    public class ModelRender : BaseRender
    {
        private readonly DeviceBuffer _diffuseColorBuffer;
        private readonly DeviceBuffer _useTextureDiffuse;
        private readonly DeviceBuffer _useTextureBumpmap;
        private readonly DeviceBuffer _useTextureEffect;
        private readonly DeviceBuffer _effect;
        private readonly DeviceBuffer _lightDirection;
        private readonly DeviceBuffer _lightColor;
        private readonly DeviceBuffer _metallicRoughnessValues;
        private readonly DeviceBuffer _cameraPosition;
        private readonly Texture _textureEnvMapDiffuse;
        private readonly TextureView _textureEnvMapDiffuseView;
        private readonly Texture _textureEnvMapSpecular;
        private readonly TextureView _textureEnvMapSpecularView;
        private readonly Texture _textureEnvMapGloss;
        private readonly TextureView _textureEnvMapGlossView;
        private readonly Texture _textureBRDF;
        private readonly TextureView _textureBRDFView;
        private readonly Texture _textureDiffuse;
        private readonly TextureView _textureDiffuseView;
        private readonly Texture _textureBumpmap;
        private readonly TextureView _textureBumpmapView;
        private readonly Texture _textureEffect;
        private readonly TextureView _textureEffectView;
        private readonly Sampler _linearSampler;
        private readonly Sampler _pointSampler;
        private readonly ResourceSet _outputVertSet;
        private readonly ResourceSet _outputFragSet;
        private readonly Pipeline _pipeline;
        private readonly Framebuffer _framebuffer;

        public ModelRender(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, VertexLayoutDescription vertexLayoutDescription, Framebuffer framebuffer, DeviceBuffer modelMatrixBuffer, DeviceBuffer viewMatrixBuffer, DeviceBuffer projectionMatrixBuffer)
        {
            _framebuffer = framebuffer;

            _diffuseColorBuffer = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _useTextureDiffuse = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _useTextureBumpmap = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _useTextureEffect = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _effect = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _lightDirection = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _lightColor = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _metallicRoughnessValues = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraPosition = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));

            _textureEnvMapDiffuse = LoadCube("irradiance").CreateDeviceTexture(graphicsDevice, resourceFactory);
            _textureEnvMapDiffuseView = resourceFactory.CreateTextureView(_textureEnvMapDiffuse);
            _textureEnvMapSpecular = LoadCube("radiance").CreateDeviceTexture(graphicsDevice, resourceFactory);
            _textureEnvMapSpecularView = resourceFactory.CreateTextureView(_textureEnvMapSpecular);
            _textureEnvMapGloss = LoadCube("gloss").CreateDeviceTexture(graphicsDevice, resourceFactory);
            _textureEnvMapGlossView = resourceFactory.CreateTextureView(_textureEnvMapGloss);
            _textureBRDF = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(graphicsDevice, resourceFactory); 
            _textureBRDFView = resourceFactory.CreateTextureView(_textureBRDF);
            _textureDiffuse = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(graphicsDevice, resourceFactory); 
            _textureDiffuseView = resourceFactory.CreateTextureView(_textureDiffuse);
            _textureBumpmap = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(graphicsDevice, resourceFactory); 
            _textureBumpmapView = resourceFactory.CreateTextureView(_textureBumpmap);
            _textureEffect = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(graphicsDevice, resourceFactory); 
            _textureEffectView = resourceFactory.CreateTextureView(_textureEffect);
            _linearSampler = graphicsDevice.LinearSampler;
            _pointSampler = graphicsDevice.PointSampler;

            var modelShaders = resourceFactory.CreateFromSpirv(LoadShader(resourceFactory, "Model", ShaderStages.Vertex, "main"), LoadShader(resourceFactory, "Model", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            ResourceLayout outputVertLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ModelMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout outputFragLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("DiffuseColor", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("UseTextureDiffuse", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("UseTextureBumpmap", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("UseTextureEffect", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Effect", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LightDirection", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LightColor", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("MetallicRoughnessValues", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("CameraPosition", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureEnvMapDiffuse", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureEnvMapSpecular", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureEnvMapGloss", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureBRDF", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureDiffuse", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureBumpmap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureEffect", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("PointSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _pipeline = resourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputVertLayout, outputFragLayout },
                _framebuffer.OutputDescription));

            _outputVertSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputVertLayout,
                modelMatrixBuffer,
                viewMatrixBuffer,
                projectionMatrixBuffer));

            _outputFragSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout,
                _diffuseColorBuffer,
                _useTextureDiffuse,
                _useTextureBumpmap,
                _useTextureEffect,
                _effect,
                _lightDirection,
                _lightColor,
                _metallicRoughnessValues,
                _cameraPosition,
                _textureEnvMapDiffuseView,
                _textureEnvMapSpecularView,
                _textureEnvMapGlossView,
                _textureBRDFView,
                _textureDiffuseView,
                _textureBumpmapView,
                _textureEffectView,
                _linearSampler,
                _pointSampler));
        }

        public float ConvertToRadians(float angle)
        {
            return (float)(Math.PI / 180) * angle;
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer)
        {
            var lightRotation = ConvertToRadians(45);
            var lightPitch = ConvertToRadians(315);
            var lightDirection = new Vector3((float)(Math.Sin(lightRotation) * Math.Cos(lightPitch)), (float)Math.Sin(lightPitch), (float)(Math.Cos(lightRotation) * Math.Cos(lightPitch)));
            const float lightIntensity = 2.0f;
            var lightColor = new Vector3(lightIntensity, lightIntensity, lightIntensity);

            commandList.UpdateBuffer(_diffuseColorBuffer, 0, new Vector4(1, 1, 1, 1));
            commandList.UpdateBuffer(_useTextureDiffuse, 0, 1);
            commandList.UpdateBuffer(_useTextureBumpmap, 0, 1);
            commandList.UpdateBuffer(_useTextureEffect, 0, 1);
            commandList.UpdateBuffer(_effect, 0, lightDirection);
            commandList.UpdateBuffer(_lightDirection, 0, new Vector3(0, 0, 1));
            commandList.UpdateBuffer(_lightColor, 0, lightColor);
            commandList.UpdateBuffer(_metallicRoughnessValues, 0, new Vector2(0.5f, 0.5f));
            commandList.UpdateBuffer(_cameraPosition, 0, new Vector4(0, 0, 2.5f, 0));

            commandList.SetFramebuffer(_framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Grey);
            commandList.ClearDepthStencil(1f);
            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetGraphicsResourceSet(0, _outputVertSet);
            commandList.SetGraphicsResourceSet(1, _outputFragSet);
            commandList.DrawIndexed(36, 1, 0, 0, 0);
        }
    }
}
