using System;
using AppKit;
using Foundation;
using Veldrid;
using Veldrid.Mac;
using Veldrid.SPIRV;
using System.Numerics;

namespace VeldridNSViewExample
{
    public struct VertexPositionTexture
    {
        public const uint SizeInBytes = 20;

        public float PosX;
        public float PosY;
        public float PosZ;

        public float TexU;
        public float TexV;

        public VertexPositionTexture(Vector3 pos, Vector2 uv)
        {
            PosX = pos.X;
            PosY = pos.Y;
            PosZ = pos.Z;
            TexU = uv.X;
            TexV = uv.Y;
        }
    }

    [Register("ViewController")]
    public class ViewController : NSSplitViewController
    {
        private VeldridView _veldridView;
        private CommandList _commandList;

        private readonly ProcessedTexture _stoneTexData;
        private readonly VertexPositionTexture[] _vertices;
        private readonly ushort[] _indices;

        private DeviceBuffer _modelMatrixBuffer;
        private DeviceBuffer _viewMatrixBuffer;
        private DeviceBuffer _projectionMatrixBuffer;
        private ResourceSet _outputFragSet;

        private DeviceBuffer _diffuseColorBuffer;
        private DeviceBuffer _useTextureDiffuse;
        private DeviceBuffer _useTextureBumpmap;
        private DeviceBuffer _useTextureEffect;
        private DeviceBuffer _effect;
        private DeviceBuffer _lightDirection;
        private DeviceBuffer _lightColor;
        private DeviceBuffer _metallicRoughnessValues;
        private DeviceBuffer _cameraPosition;
        private Texture _textureEnvMapDiffuse;
        private TextureView _textureEnvMapDiffuseView;
        private Texture _textureEnvMapSpecular;
        private TextureView _textureEnvMapSpecularView;
        private Texture _textureEnvMapGloss;
        private TextureView _textureEnvMapGlossView;
        private Texture _textureBRDF;
        private TextureView _textureBRDFView;
        private Texture _textureDiffuse;
        private TextureView _textureDiffuseView;
        private Texture _textureBumpmap;
        private TextureView _textureBumpmapView;
        private Texture _textureEffect;
        private TextureView _textureEffectView;
        private Sampler _linearSampler;
        private Sampler _pointSampler;
        private ResourceSet _outputVertSet;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        private Pipeline _pipeline;

        private int _frameIndex = 0;
        private RgbaFloat[] _clearColors =
        {
            RgbaFloat.Red,
            RgbaFloat.Orange,
            RgbaFloat.Yellow,
            RgbaFloat.Green,
            RgbaFloat.Blue,
            new RgbaFloat(0.8f, 0.1f, 0.3f, 1f),
            new RgbaFloat(0.8f, 0.1f, 0.9f, 1f),
        };
        private readonly int _frameRepeatCount = 20;

        public ViewController(IntPtr handle) : base(handle)
        {
           // _stoneTexData = LoadEmbeddedAsset<ProcessedTexture>("spnza_bricks_a_diff.binary");
            _vertices = GetCubeVertices();
            _indices = GetCubeIndices();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var graphicsDeviceOptions = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true, true);

            _veldridView = new VeldridView(GraphicsBackend.Metal, graphicsDeviceOptions)
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(_veldridView);
            _veldridView.LeftAnchor.ConstraintEqualToAnchor(View.LeftAnchor, 16).Active = true;
            _veldridView.RightAnchor.ConstraintEqualToAnchor(View.RightAnchor, -16).Active = true;
            _veldridView.TopAnchor.ConstraintEqualToAnchor(View.TopAnchor, 16).Active = true;
            _veldridView.BottomAnchor.ConstraintEqualToAnchor(View.BottomAnchor, -16).Active = true;

            _veldridView.DeviceReady += VeldridView_DeviceReady;
            _veldridView.Resized += VeldridView_Resized;
            _veldridView.Rendering += VeldridView_Rendering;
        }

        public ShaderDescription LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
        {
            string name = $"{set}.{stage.ToString().Substring(0, 4).ToLower()}.spv";
            return new ShaderDescription(stage, ResourceLoader.GetEmbeddedResourceBytes(name), entryPoint);
        }

        private void CreateResources(ResourceFactory resourceFactory)
        {
            _commandList = resourceFactory.CreateCommandList();

            _modelMatrixBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewMatrixBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _projectionMatrixBuffer = resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _diffuseColorBuffer = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _useTextureDiffuse = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _useTextureBumpmap = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _useTextureEffect = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _effect = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _lightDirection = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _lightColor = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _metallicRoughnessValues = resourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _cameraPosition = resourceFactory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer));
            _textureEnvMapDiffuse = null;
            _textureEnvMapDiffuseView = resourceFactory.CreateTextureView(_textureEnvMapDiffuse);
            _textureEnvMapSpecular = null;
            _textureEnvMapSpecularView = resourceFactory.CreateTextureView(_textureEnvMapSpecular);
            _textureEnvMapGloss = null;
            _textureEnvMapGlossView = resourceFactory.CreateTextureView(_textureEnvMapGloss);
            _textureBRDF = null;
            _textureBRDFView = resourceFactory.CreateTextureView(_textureBRDF);
            _textureDiffuse = null;
            _textureDiffuseView = resourceFactory.CreateTextureView(_textureDiffuse);
            _textureBumpmap = null;
            _textureBumpmapView = resourceFactory.CreateTextureView(_textureBumpmap);
            _textureEffect = null;
            _textureEffectView = resourceFactory.CreateTextureView(_textureEffect);
            _linearSampler = _veldridView.GraphicsDevice.Aniso4xSampler;
            _pointSampler = _veldridView.GraphicsDevice.Aniso4xSampler;

            _vertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(VertexPositionTexture.SizeInBytes * _vertices.Length), BufferUsage.VertexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);
            _indexBuffer = resourceFactory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)_indices.Length, BufferUsage.IndexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);

    //        _surfaceTexture = _stoneTexData.CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory, TextureUsage.Sampled);
     //       _surfaceTextureView = resourceFactory.CreateTextureView(_surfaceTexture);



            var modelShaders = resourceFactory.CreateFromSpirv(LoadShader(resourceFactory, "Model", ShaderStages.Vertex, "main"), LoadShader(resourceFactory, "Model", ShaderStages.Fragment, "main"));

            var vertexLayoutDescription = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

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
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { outputVertLayout, outputFragLayout },
                _veldridView.MainSwapchain.Framebuffer.OutputDescription));

            _outputVertSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(
                outputVertLayout,
                _modelMatrixBuffer,
                _viewMatrixBuffer,
                _projectionMatrixBuffer));

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

        void VeldridView_DeviceReady()
        {
            CreateResources(_veldridView.GraphicsDevice.ResourceFactory);
        }

        void VeldridView_Resized()
        {
        }

        void VeldridView_Rendering()
        {
            _commandList.Begin();

    //        _commandList.UpdateBuffer()
    //        _commandList.UpdateBuffer(_projectionBuffer, 0, Matrix4x4.CreatePerspectiveFieldOfView(
    //1.0f,
    //(float)Window.Width / Window.Height,
    //0.5f,
    //100f));

            _commandList.SetFramebuffer(_veldridView.MainSwapchain.Framebuffer);
            _commandList.ClearColorTarget(0, _clearColors[_frameIndex / _frameRepeatCount]);
            _commandList.End();
            _veldridView.GraphicsDevice.SubmitCommands(_commandList);
            _veldridView.GraphicsDevice.SwapBuffers(_veldridView.MainSwapchain);

            _frameIndex = (_frameIndex + 1) % (_clearColors.Length * _frameRepeatCount);
        }



        private static VertexPositionTexture[] GetCubeVertices()
        {
            var vertices = new VertexPositionTexture[]
            {
                // Top
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 1)),
                // Bottom                                                             
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, +0.5f),  new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f,-0.5f, +0.5f),  new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f,-0.5f, -0.5f),  new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, -0.5f),  new Vector2(0, 1)),
                // Left                                                               
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Right                                                              
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
                // Back                                                               
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Front                                                              
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
            };

            return vertices;
        }

        private static ushort[] GetCubeIndices()
        {
            ushort[] indices =
            {
                0,1,2, 0,2,3,
                4,5,6, 4,6,7,
                8,9,10, 8,10,11,
                12,13,14, 12,14,15,
                16,17,18, 16,18,19,
                20,21,22, 20,22,23,
            };

            return indices;
        }


    }
}
