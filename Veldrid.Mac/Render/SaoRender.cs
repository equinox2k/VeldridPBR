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
        private Texture _colorTarget;
        private Framebuffer _framebuffer;

        private readonly float _seedValue;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly DeviceBuffer _cameraNearBuffer;
        private readonly DeviceBuffer _cameraFarBuffer;
        private readonly DeviceBuffer _projectionMatrixBuffer;
        private readonly DeviceBuffer _inverseProjectionMatrixBuffer;
        private readonly DeviceBuffer _scaleBuffer;
        private readonly DeviceBuffer _intensityBuffer;
        private readonly DeviceBuffer _biasBuffer;
        private readonly DeviceBuffer _kernalRadiusBuffer;
        private readonly DeviceBuffer _minResolutionBuffer;
        private readonly DeviceBuffer _sizeBuffer;
        private readonly DeviceBuffer _seedBuffer;
        private readonly ResourceSet _outputFragSet0;
        private readonly ResourceLayout _outputFragLayout1;
        private readonly Pipeline _pipeline;

        public Texture GetColorTarget()
        {
            return _colorTarget;
        }

        public override void Resize()
        {
            Texture depthTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(_camera.Width, _camera.Height, 1, 1, PixelFormat.R32_Float, TextureUsage.DepthStencil));
            _colorTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(_camera.Width, _camera.Height, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _framebuffer = _graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(depthTarget, _colorTarget));
        }

        public SaoRender(GraphicsDevice graphicsDevice, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _seedValue = (float)new Random().NextDouble();
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            Resize();

            _cameraNearBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraFarBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _projectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _inverseProjectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _scaleBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _intensityBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _biasBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _kernalRadiusBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _minResolutionBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _sizeBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _seedBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

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
                _scaleBuffer,
                _intensityBuffer,
                _biasBuffer,
                _kernalRadiusBuffer,
                _minResolutionBuffer,
                _sizeBuffer,
                _seedBuffer,
                _graphicsDevice.LinearSampler));
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, Texture depthNormalTexture)
        {
            var outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _outputFragLayout1,
                _graphicsDevice.ResourceFactory.CreateTextureView(depthNormalTexture)));

            commandList.UpdateBuffer(_cameraNearBuffer, 0, _camera.Near);
            commandList.UpdateBuffer(_cameraFarBuffer, 0, _camera.Far);
            commandList.UpdateBuffer(_projectionMatrixBuffer, 0, _camera.ProjectionMatrix);
            commandList.UpdateBuffer(_inverseProjectionMatrixBuffer, 0, _camera.InverseProjectionMatrix);
            commandList.UpdateBuffer(_scaleBuffer, 0, 1.0f);
            commandList.UpdateBuffer(_intensityBuffer, 0, 0.01f);
            commandList.UpdateBuffer(_biasBuffer, 0, 0.5f);
            commandList.UpdateBuffer(_kernalRadiusBuffer, 0, 10.0f);
            commandList.UpdateBuffer(_minResolutionBuffer, 0, 0.01f);
            commandList.UpdateBuffer(_sizeBuffer, 0, new Vector2(_camera.Width, _camera.Height));
            commandList.UpdateBuffer(_seedBuffer, 0, _seedValue);

            commandList.SetFramebuffer(_framebuffer);
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
