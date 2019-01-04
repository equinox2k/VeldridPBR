using System;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace PNI.Render3d.Core.Render
{
    public class OutputRender : BaseRender
    {
        private readonly Camera _camera;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Framebuffer _framebuffer;
        private readonly DeviceBuffer _opacityBuffer;
        private readonly DeviceBuffer _skyboxDiffuseBuffer;
        private readonly DeviceBuffer _inverseModelViewMatrixBuffer;
        private readonly DeviceBuffer _inverseProjectionMatrixBuffer;
        private readonly ResourceSet _outputVertSet0;
        private readonly ResourceSet _outputFragSet1;
        private readonly ResourceLayout _outputFragLayout2;
        private readonly Pipeline _pipeline;

        public OutputRender(GraphicsDevice graphicsDevice, Framebuffer framebuffer, Camera camera, VertexLayoutDescription vertexLayoutDescription)
        {
            _framebuffer = framebuffer;
            _graphicsDevice = graphicsDevice;
            _camera = camera;

            _opacityBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _skyboxDiffuseBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _inverseModelViewMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _inverseProjectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            var textureEnvSkybox = LoadCube("skybox").CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            var textureEnvSkyboxView = _graphicsDevice.ResourceFactory.CreateTextureView(textureEnvSkybox);

            var modelShaders = graphicsDevice.ResourceFactory.CreateFromSpirv(LoadShader("Output", ShaderStages.Vertex, "main"), LoadShader("Output", ShaderStages.Fragment, "main"));

            var shaderSet = new ShaderSetDescription(new[] { vertexLayoutDescription }, modelShaders);

            var outputVertLayout0 = _graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("InverseModelViewMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("InverseProjectionMatrix", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            var outputFragLayout1 = graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Opacity", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SkyboxDiffuse", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureEnvSkybox", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("LinearSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
                    
            _outputFragLayout2 = graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TextureDiffuse", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureAmbientOcclusion", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            _pipeline = graphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputVertLayout0, outputFragLayout1, _outputFragLayout2 },
                _framebuffer.OutputDescription));

            _outputVertSet0 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                 outputVertLayout0,
                 _inverseModelViewMatrixBuffer,
                 _inverseProjectionMatrixBuffer));
                 
            _outputFragSet1 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputFragLayout1,
                _opacityBuffer,
                _skyboxDiffuseBuffer,
                textureEnvSkyboxView,
                graphicsDevice.LinearSampler));
        }

        public void Update(CommandList commandList, DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, Texture diffuseTexture, Texture textureAmbientOcclusion)
        {
            using (var outputFragSet2 = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _outputFragLayout2,
                _graphicsDevice.ResourceFactory.CreateTextureView(diffuseTexture),
                _graphicsDevice.ResourceFactory.CreateTextureView(textureAmbientOcclusion))))
            {
                commandList.UpdateBuffer(_opacityBuffer, 0, 1.0f);
                commandList.UpdateBuffer(_skyboxDiffuseBuffer, 0, new Vector4(0.8f, 0.8f, 0.8f, 1));
                commandList.UpdateBuffer(_inverseModelViewMatrixBuffer, 0, _camera.InverseModelViewMatrix);
                commandList.UpdateBuffer(_inverseProjectionMatrixBuffer, 0, _camera.InverseProjectionMatrix);

                commandList.SetFramebuffer(_framebuffer);
                commandList.ClearColorTarget(0, RgbaFloat.Clear);
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
