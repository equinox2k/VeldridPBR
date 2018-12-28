using System;
using System.Numerics;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;
using Veldrid.SPIRV;

namespace VeldridNSViewExample.Render
{
    public class ModelRender : BaseRender
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly Texture _colorTarget;
        private readonly Framebuffer _framebuffer;
        private readonly DeviceBuffer _modelMatrixBuffer;
        private readonly DeviceBuffer _viewMatrixBuffer;
        private readonly DeviceBuffer _projectionMatrixBuffer;
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

        public Texture GetColorTarget()
        {
            return _colorTarget;
        }

        public ModelRender(GraphicsDevice graphicsDevice, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            Texture depthTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(1000, 1000, 1, 1, PixelFormat.R32_Float, TextureUsage.DepthStencil));
            _colorTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(1000, 1000, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _framebuffer = _graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(depthTarget, _colorTarget));

            _modelMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _projectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _diffuseColorBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _useTextureDiffuse = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _useTextureBumpmap = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _useTextureEffect = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _effect = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _lightDirection = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _lightColor = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _metallicRoughnessValues = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraPosition = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));

            _textureEnvMapDiffuse = LoadCube("irradiance").CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            _textureEnvMapDiffuseView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureEnvMapDiffuse);
            _textureEnvMapSpecular = LoadCube("radiance").CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            _textureEnvMapSpecularView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureEnvMapSpecular);
            _textureEnvMapGloss = LoadCube("gloss").CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            _textureEnvMapGlossView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureEnvMapGloss);
            _textureBRDF = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory); 
            _textureBRDFView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureBRDF);
            _textureDiffuse = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory); 
            _textureDiffuseView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureDiffuse);
            _textureBumpmap = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory); 
            _textureBumpmapView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureBumpmap);
            _textureEffect = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory); 
            _textureEffectView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureEffect);
            _linearSampler = graphicsDevice.LinearSampler;
            _pointSampler = graphicsDevice.PointSampler;

            var modelShaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("Model", ShaderStages.Vertex, "main"), LoadShader("Model", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            ResourceLayout outputVertLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ModelMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout outputFragLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
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

            _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputVertLayout, outputFragLayout },
                _framebuffer.OutputDescription));

            _outputVertSet = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputVertLayout,
                _modelMatrixBuffer,
                _viewMatrixBuffer,
                _projectionMatrixBuffer));

            _outputFragSet = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
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

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer)
        {
            commandList.UpdateBuffer(_modelMatrixBuffer, 0, _camera.ModelMatrix);
            commandList.UpdateBuffer(_viewMatrixBuffer, 0, _camera.ViewMatrix);
            commandList.UpdateBuffer(_projectionMatrixBuffer, 0, _camera.ProjectionMatrix);

            var lightRotation = MathHelper.DegreesToRadians(45);
            var lightPitch = MathHelper.DegreesToRadians(315);
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
