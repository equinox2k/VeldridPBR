using System;
using System.Numerics;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;
using Veldrid.SPIRV;

namespace VeldridNSViewExample.Render
{
    public class SaoRender : BaseRender
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly Framebuffer _framebuffer;
        private readonly DeviceBuffer _cameraNearBuffer;
        private readonly DeviceBuffer _cameraFarBuffer;
        private readonly DeviceBuffer _projectionMatrixBuffer;
        private readonly DeviceBuffer _inverseProjectionMatrixBuffer;
        private readonly DeviceBuffer _scale;
        private readonly DeviceBuffer _intensity;
        private readonly DeviceBuffer _bias;
        private readonly DeviceBuffer _kernalRadius;
        private readonly DeviceBuffer _minResolution;
        private readonly DeviceBuffer _size;
        private readonly DeviceBuffer _seed;
        private readonly Texture _textureDepthNormal;
        private readonly TextureView _textureDepthNormalView;
        private readonly Sampler _pointSampler;
        private readonly ResourceSet _outputFragSet0;
        private readonly ResourceLayout _outputFragLayout1;
        private readonly Pipeline _pipeline;

        public SaoRender(GraphicsDevice graphicsDevice, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            Texture depthTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(1000, 1000, 1, 1, PixelFormat.R32_Float, TextureUsage.DepthStencil));
            Texture colorTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(1000, 1000, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _framebuffer = _graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(depthTarget, colorTarget));

            _cameraNearBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraFarBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _projectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _inverseProjectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _scale = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _intensity = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _bias = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _kernalRadius = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _minResolution = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _size = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _seed = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            _textureDepthNormal = LoadCube("irradiance").CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            _textureDepthNormalView = _graphicsDevice.ResourceFactory.CreateTextureView(_textureDepthNormal);
            _pointSampler = graphicsDevice.PointSampler;

            var modelShaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("Sao", ShaderStages.Vertex, "main"), LoadShader("Sao", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            ResourceLayout outputFragLayout0 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("CameraNear", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("CameraFar", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("ProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("InverseProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Scale", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Intensity", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Bias", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("KernelRadius", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("MinResolution", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Size", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Seed", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("PointSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _outputFragLayout1 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TextureDepthNormal", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputFragLayout0, _outputFragLayout1 },
                _framebuffer.OutputDescription));
               
            _outputFragSet0 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout0,
                _cameraNearBuffer,
                _cameraFarBuffer,
                _projectionMatrixBuffer,
                _inverseProjectionMatrixBuffer,
                _scale,
                _intensity,
                _bias,
                _kernalRadius,
                _minResolution,
                _size,
                _seed,
                _pointSampler));
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, Texture depthTexture)
        {
            var outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _outputFragLayout1,
                _graphicsDevice.ResourceFactory.CreateTextureView(depthTexture)));

            commandList.UpdateBuffer(_cameraNearBuffer, 0, _camera.Near);
            commandList.UpdateBuffer(_cameraFarBuffer, 0, _camera.Far);
            commandList.UpdateBuffer(_projectionMatrixBuffer, 0, _camera.ProjectionMatrix);
            commandList.UpdateBuffer(_inverseProjectionMatrixBuffer, 0, _camera.InverseProjectionMatrix);
            commandList.UpdateBuffer(_scale, 0, 1.0f);
            commandList.UpdateBuffer(_intensity, 0, 1.0f);
            commandList.UpdateBuffer(_bias, 0, 1.0f);
            commandList.UpdateBuffer(_kernalRadius, 0, 1.0f);
            commandList.UpdateBuffer(_minResolution, 0, 1.0f);
            commandList.UpdateBuffer(_size, 0, new Vector2(1000.0f, 1000.0f));
            commandList.UpdateBuffer(_seed, 0, 0.0f);

            commandList.SetFramebuffer(_framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Grey);
            commandList.ClearDepthStencil(1f);
            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetGraphicsResourceSet(0, _outputFragSet0);
            commandList.SetGraphicsResourceSet(1, outputFragSet1);
            commandList.DrawIndexed(6, 1, 0, 0, 0);
        }
    }
}
