using System;
using System.Numerics;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Mac;
using Veldrid.SPIRV;

namespace VeldridNSViewExample.Render
{
    public class SkyboxRender : BaseRender
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Camera _camera;
        private readonly Framebuffer _framebuffer;
        private readonly DeviceBuffer _inverseModelViewMatrixBuffer;
        private readonly DeviceBuffer _inverseProjectionMatrixBuffer;
        private readonly DeviceBuffer _skyboxDiffuseBuffer;
        private readonly Sampler _linearSampler;
        private readonly ResourceSet _outputVertSet0;
        private readonly ResourceSet _outputFragSet1;
        private readonly Pipeline _pipeline;

        public SkyboxRender(GraphicsDevice graphicsDevice, Framebuffer framebuffer, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _framebuffer = framebuffer;
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            _inverseModelViewMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _inverseProjectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _skyboxDiffuseBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _linearSampler = graphicsDevice.LinearSampler;

            var textureEnvSkybox = LoadCube("irradiance").CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            var textureEnvSkyboxView = _graphicsDevice.ResourceFactory.CreateTextureView(textureEnvSkybox);

            var modelShaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("Skybox", ShaderStages.Vertex, "main"), LoadShader("Skybox", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            ResourceLayout outputVertLayout0 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("InverseModelViewMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("InverseProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout outputFragLayout1 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SkyboxDiffuse", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureEnvSkybox", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

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
                _inverseModelViewMatrixBuffer,
                _inverseProjectionMatrixBuffer));

            _outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout1,
                _skyboxDiffuseBuffer,
                textureEnvSkyboxView,
                _linearSampler));
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer)
        {
            commandList.UpdateBuffer(_inverseModelViewMatrixBuffer, 0, _camera.InverseModelViewMatrix);
            commandList.UpdateBuffer(_inverseProjectionMatrixBuffer, 0, _camera.InverseProjectionMatrix);
            commandList.UpdateBuffer(_skyboxDiffuseBuffer, 0, new Vector4(0.8f, 0.8f, 0.8f, 1));

            commandList.SetFramebuffer(_framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Clear);
            commandList.ClearDepthStencil(1f);
            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetGraphicsResourceSet(0, _outputVertSet0);
            commandList.SetGraphicsResourceSet(1, _outputFragSet1);
            commandList.DrawIndexed(6, 1, 0, 0, 0);
        }
    }
}
