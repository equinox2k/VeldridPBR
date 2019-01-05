using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace PNI.Render3d.Core.Render
{
    public class DepthLimitedBlurRender : BaseRender
    {
        private Texture _colorTarget;
        private Framebuffer _framebuffer;

        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly DeviceBuffer _isUvOriginTopLeftBuffer;
        private readonly DeviceBuffer _sizeBuffer;
        private readonly DeviceBuffer _cameraNearBuffer;
        private readonly DeviceBuffer _cameraFarBuffer;
        private readonly DeviceBuffer _depthCutOffBuffer;
        private readonly DeviceBuffer _sampleUvOffsetWeightsBuffer;
        private readonly ResourceSet _outputVertSet0;
        private readonly ResourceSet _outputFragSet1;
        private readonly ResourceLayout _outputFragLayout2;
        private readonly Pipeline _pipeline;

        public Vector4[] OffsetsHoriz { get; set; }
        public Vector4[] OffsetsVert { get; set; }

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

            var tempOffsetsHoriz = new List<Vector4>();
            var tempOffsetsVert = new List<Vector4>();
            for (var i = 0; i <= saoKernelRadius; i++)
            {
                var gausian = Math.Exp(-(i * i) / (2.0 * (saoBlurStdDev * saoBlurStdDev))) / (Math.Sqrt(2.0 * Math.PI) * saoBlurStdDev);
                tempOffsetsHoriz.Add(new Vector4(0, i, (float)gausian, 0));
                tempOffsetsVert.Add(new Vector4(i, 0, (float)gausian, 0));
            }
            OffsetsHoriz = tempOffsetsHoriz.ToArray();
            OffsetsVert = tempOffsetsVert.ToArray();
        }

        public DepthLimitedBlurRender(GraphicsDevice graphicsDevice, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            GenerateOffsets();

            Resize();

            _isUvOriginTopLeftBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _sizeBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraNearBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraFarBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _depthCutOffBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _sampleUvOffsetWeightsBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(144, BufferUsage.UniformBuffer));

            var crossCompileOptions = new CrossCompileOptions(true, false);
            var modelShaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("DepthLimitedBlur", ShaderStages.Vertex, "main"), LoadShader("DepthLimitedBlur", ShaderStages.Fragment, "main"), crossCompileOptions);

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            var outputVertLayout0 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Size", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            var outputFragLayout1 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("IsUvOriginTopLeft", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("CameraNear", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("CameraFar", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("DepthCutOff", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SampleUvOffsetWeights", ResourceKind.UniformBuffer, ShaderStages.Fragment),
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
                _isUvOriginTopLeftBuffer,
                _cameraNearBuffer,
                _cameraFarBuffer,
                _depthCutOffBuffer,
                _sampleUvOffsetWeightsBuffer,
                _graphicsDevice.LinearSampler,
                _graphicsDevice.PointSampler));
        }

        public void Update(CommandList commandList, Mesh screenMesh, Texture depthNormalTexture, Texture diffuseTexture, bool verticalPass)
        {
            using (var outputFragSet2 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _outputFragLayout2,
                _graphicsDevice.ResourceFactory.CreateTextureView(depthNormalTexture),
                _graphicsDevice.ResourceFactory.CreateTextureView(diffuseTexture))))
            {
                commandList.UpdateBuffer(_isUvOriginTopLeftBuffer, 0, _graphicsDevice.IsUvOriginTopLeft ? 1 : 0);
                commandList.UpdateBuffer(_sizeBuffer, 0, new Vector2(_camera.Width, _camera.Height));
                commandList.UpdateBuffer(_cameraNearBuffer, 0, _camera.Near);
                commandList.UpdateBuffer(_cameraFarBuffer, 0, _camera.Far);
                commandList.UpdateBuffer(_depthCutOffBuffer, 0, 0.01f);
                commandList.UpdateBuffer(_sampleUvOffsetWeightsBuffer, 0, verticalPass ? OffsetsVert : OffsetsHoriz);

                commandList.SetFramebuffer(_framebuffer);
                commandList.ClearColorTarget(0, RgbaFloat.Clear);
                commandList.ClearDepthStencil(1f);
                commandList.SetPipeline(_pipeline);
                commandList.SetVertexBuffer(0, screenMesh.VertexBuffer);
                commandList.SetIndexBuffer(screenMesh.IndexBuffer, IndexFormat.UInt32);
                commandList.SetGraphicsResourceSet(0, _outputVertSet0);
                commandList.SetGraphicsResourceSet(1, _outputFragSet1);
                commandList.SetGraphicsResourceSet(2, outputFragSet2);
                commandList.DrawIndexed(6, 1, 0, 0, 0);
            }
        }
    }
}
