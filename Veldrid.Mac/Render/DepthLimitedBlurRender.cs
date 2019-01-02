using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;
using Veldrid.SPIRV;

namespace VeldridNSViewExample.Render
{
    public class DepthLimitedBlurRender : BaseRender
    {
        private Texture _colorTarget;
        private Framebuffer _framebuffer;

        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly DeviceBuffer _flipyBuffer;
        private readonly DeviceBuffer _sizeBuffer;
        private readonly DeviceBuffer _cameraNearBuffer;
        private readonly DeviceBuffer _cameraFarBuffer;
        private readonly DeviceBuffer _depthCutOffBuffer;
        private readonly DeviceBuffer _sampleUvOffsetsBuffer;
        private readonly DeviceBuffer _sampleWeightsBuffer;
        private readonly ResourceSet _outputVertSet0;
        private readonly ResourceSet _outputFragSet1;
        private readonly ResourceLayout _outputFragLayout2;
        private readonly Pipeline _pipeline;

        public Vector2[] OffsetsHoriz { get; set; }
        public Vector2[] OffsetsVert { get; set; }
        public float[] Weights { get; set; }

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

        private void GenerateOffsets()
        {
            const int saoKernelRadius = 8;
            const int saoBlurStdDev = 4;

            var tempOffsetsHoriz = new List<Vector2>();
            var tempOffsetsVert = new List<Vector2>();
            var tempWeights = new List<float>();
            for (var i = 0; i <= saoKernelRadius; i++)
            {
                tempOffsetsHoriz.Add(new Vector2(0, i));
                tempOffsetsVert.Add(new Vector2(i, 0));
                var gausian = Math.Exp(-(i * i) / (2.0 * (saoBlurStdDev * saoBlurStdDev))) / (Math.Sqrt(2.0 * Math.PI) * saoBlurStdDev);
                tempWeights.Add((float)gausian);
            }
            OffsetsHoriz = tempOffsetsHoriz.ToArray();
            OffsetsVert = tempOffsetsVert.ToArray();
            Weights = tempWeights.ToArray();
        }

        public DepthLimitedBlurRender(GraphicsDevice graphicsDevice, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            GenerateOffsets();

            Resize();

            _flipyBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _sizeBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraNearBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraFarBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _depthCutOffBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _sampleUvOffsetsBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(80, BufferUsage.UniformBuffer));
            _sampleWeightsBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(48, BufferUsage.UniformBuffer));

            var modelShaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("DepthLimitedBlur", ShaderStages.Vertex, "main"), LoadShader("DepthLimitedBlur", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            var outputVertLayout0 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Size", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            var outputFragLayout1 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("CameraNear", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("CameraFar", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("DepthCutOff", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SampleUvOffsets", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SampleWeights", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("PointSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _outputFragLayout2 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TextureDepthNormal", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureDiffuse", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputVertLayout0, outputFragLayout1, _outputFragLayout2 },
                _framebuffer.OutputDescription));

            _outputVertSet0 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputVertLayout0,
                _sizeBuffer));

            _outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout1,
                _cameraNearBuffer,
                _cameraFarBuffer,
                _depthCutOffBuffer,
                _sampleUvOffsetsBuffer,
                _sampleWeightsBuffer,
                _graphicsDevice.LinearSampler,
                _graphicsDevice.PointSampler));
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, Texture depthNormalTexture, Texture diffuseTexture, bool verticalPass)
        {
            using (var outputFragSet2 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _outputFragLayout2,
                _graphicsDevice.ResourceFactory.CreateTextureView(depthNormalTexture),
                _graphicsDevice.ResourceFactory.CreateTextureView(diffuseTexture))))
            {
                commandList.UpdateBuffer(_sizeBuffer, 0, new Vector2(_camera.Width, _camera.Height));
                commandList.UpdateBuffer(_cameraNearBuffer, 0, _camera.Near);
                commandList.UpdateBuffer(_cameraFarBuffer, 0, _camera.Far);
                commandList.UpdateBuffer(_depthCutOffBuffer, 0, 0.01f);
                commandList.UpdateBuffer(_sampleUvOffsetsBuffer, 0, verticalPass ? OffsetsVert : OffsetsHoriz);
                commandList.UpdateBuffer(_sampleWeightsBuffer, 0, Weights);

                commandList.SetFramebuffer(_framebuffer);
                commandList.ClearDepthStencil(1f);
                commandList.SetPipeline(_pipeline);
                commandList.SetVertexBuffer(0, vertexBuffer);
                commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
                commandList.SetGraphicsResourceSet(0, _outputVertSet0);
                commandList.SetGraphicsResourceSet(1, _outputFragSet1);
                commandList.SetGraphicsResourceSet(2, outputFragSet2);
                commandList.DrawIndexed(6, 1, 0, 0, 0);
            }
        }
    }
}
