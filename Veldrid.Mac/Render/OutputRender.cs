using System;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;
using Veldrid.SPIRV;

namespace VeldridNSViewExample.Render
{
    public class OutputRender : BaseRender
    {
        private readonly Camera _camera;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Framebuffer _framebuffer;
        private readonly DeviceBuffer _opacityBuffer;
        private readonly Sampler _linearSampler;
        private readonly ResourceSet _outputFragSet0;
        private readonly ResourceLayout _outputFragLayout1;
        private readonly Pipeline _pipeline;

        public OutputRender(GraphicsDevice graphicsDevice, Framebuffer framebuffer, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _framebuffer = framebuffer;
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            _opacityBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _linearSampler = graphicsDevice.LinearSampler;

            var modelShaders = graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("Output", ShaderStages.Vertex, "main"), LoadShader("Output", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            var outputFragLayout0 = graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Opacity", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _outputFragLayout1 = graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TextureDiffuse", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureAmbientOcclusion", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            _pipeline = graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputFragLayout0, _outputFragLayout1 },
                _framebuffer.OutputDescription));

            _outputFragSet0 = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout0,
                _opacityBuffer,
                _linearSampler));
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, Texture diffuseTexture, Texture textureAmbientOcclusion)
        {
            var outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _outputFragLayout1,
                _graphicsDevice.ResourceFactory.CreateTextureView(diffuseTexture),
                _graphicsDevice.ResourceFactory.CreateTextureView(textureAmbientOcclusion)));

            commandList.UpdateBuffer(_opacityBuffer, 0, 1.0f);

            commandList.SetFramebuffer(_framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Clear);
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
