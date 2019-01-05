using System;
using PNI.Rendering.Harmony;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace PNI.Render3d.Core.Render
{
    public class DepthNormalRender : BaseRender
    {
        private Texture _colorTarget;
        private Framebuffer _framebuffer;

        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly DeviceBuffer _isUvOriginTopLeftBuffer;
        private readonly DeviceBuffer _modelMatrixBuffer;
        private readonly DeviceBuffer _viewMatrixBuffer;
        private readonly DeviceBuffer _projectionMatrixBuffer;
        private readonly DeviceBuffer _normalMatrixBuffer;
        private readonly ResourceSet _outputVertSet0;
        private readonly ResourceSet _outputFragSet1;
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

        public DepthNormalRender(GraphicsDevice graphicsDevice, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            Resize();

            _isUvOriginTopLeftBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _modelMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _projectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _normalMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            var crossCompileOptions = new CrossCompileOptions(true, false);
            var modelShaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("DepthNormal", ShaderStages.Vertex, "main"), LoadShader("DepthNormal", ShaderStages.Fragment, "main"), crossCompileOptions);

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            ResourceLayout outputVertLayout0 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ModelMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("NormalMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout outputFragLayout1 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("IsUvOriginTopLeft", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputVertLayout0, outputFragLayout1 },
                _framebuffer.OutputDescription));

            _outputVertSet0 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputVertLayout0,
                _modelMatrixBuffer,
                _viewMatrixBuffer,
                _projectionMatrixBuffer,
                _normalMatrixBuffer));

            _outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout1,
                _isUvOriginTopLeftBuffer));
        }

        public void Update(CommandList commandList, SurfaceGroups surfaceGroups, string option)
        {
            commandList.UpdateBuffer(_isUvOriginTopLeftBuffer, 0, _graphicsDevice.IsUvOriginTopLeft ? 1 : 0);
            commandList.UpdateBuffer(_modelMatrixBuffer, 0, _camera.ModelMatrix);
            commandList.UpdateBuffer(_viewMatrixBuffer, 0, _camera.ViewMatrix);
            commandList.UpdateBuffer(_projectionMatrixBuffer, 0, _camera.ProjectionMatrix);
            commandList.UpdateBuffer(_normalMatrixBuffer, 0, _camera.NormalMatrix);

            commandList.SetFramebuffer(_framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Clear);
            commandList.ClearDepthStencil(1f);
            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, surfaceGroups.VertexBuffer);
            commandList.SetIndexBuffer(surfaceGroups.IndexBuffer, IndexFormat.UInt32);
            commandList.SetGraphicsResourceSet(0, _outputVertSet0);
            commandList.SetGraphicsResourceSet(1, _outputFragSet1);

            foreach (var surfaceGroupInfo in surfaceGroups.SurfaceGroupInfos)
            {
                var properties = surfaceGroupInfo.Properties as SurfaceProperties;
                if (!option.Equals("Default", StringComparison.CurrentCultureIgnoreCase))
                {
                    var surfaceRootProperties = surfaceGroupInfo.Properties;
                    foreach (var surfaceOption in surfaceRootProperties.Options)
                    {
                        if (!surfaceOption.OptionName.Equals(option, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                        properties = surfaceOption;
                        break;
                    }
                }

                if (!properties.Render)
                {
                    continue;
                }

                commandList.DrawIndexed(surfaceGroupInfo.IndexCount, 1, surfaceGroupInfo.IndexOffset, 0, 0);
            }
        }
    }
}
