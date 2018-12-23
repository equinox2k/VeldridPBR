using System;
using AppKit;
using Foundation;
using Veldrid;
using Veldrid.Mac;
using Veldrid.SPIRV;
using System.Numerics;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;

namespace VeldridNSViewExample
{
    public struct Vertex
    {
        public const uint SizeInBytes = 44;

        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public float TexCoordX;
        public float TexCoordY;

        public float NormalX;
        public float NormalY;
        public float NormalZ;

        public float TangentX;
        public float TangentY;
        public float TangentZ;

        public Vertex(Vector3 position, Vector2 texCoord, Vector3 normal, Vector3 tangent)
        {
            PositionX = position.X;
            PositionY = position.Y;
            PositionZ = position.Z;
            TexCoordX = texCoord.X;
            TexCoordY = texCoord.Y;
            NormalX = normal.X;
            NormalY = normal.Y;
            NormalZ = normal.Z;
            TangentX = tangent.X;
            TangentY = tangent.Y;
            TangentZ = tangent.Z;
        }
    }

    [Register("ViewController")]
    public class ViewController : NSSplitViewController
    {
        private VeldridView _veldridView;
        private CommandList _commandList;

        private readonly Vertex[] _vertices;
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
            string name = $"VeldridNSViewExample.Shaders.{set}.{stage.ToString().Substring(0, 4).ToLower()}.spv";
            return new ShaderDescription(stage, ResourceLoader.GetEmbeddedResourceBytes(name), entryPoint);
        }

        private ImageSharpCubemapTexture LoadCube(string name)
        {
            var posX = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_posx.jpg"));
            var negX = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_negx.jpg"));
            var posY = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_posy.jpg"));
            var negY = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_negY.jpg"));
            var posZ = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_posZ.jpg"));
            var negZ = Image.Load(ResourceLoader.GetEmbeddedResourceStream($"VeldridNSViewExample.ThreeDee.{name}_negZ.jpg"));
            return new ImageSharpCubemapTexture(posX, negX, posY, negY, posZ, negZ);
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

            _textureEnvMapDiffuse = LoadCube("irradiance").CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory);
            _textureEnvMapDiffuseView = resourceFactory.CreateTextureView(_textureEnvMapDiffuse);
            _textureEnvMapSpecular = LoadCube("radiance").CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory);
            _textureEnvMapSpecularView = resourceFactory.CreateTextureView(_textureEnvMapSpecular);
            _textureEnvMapGloss = LoadCube("gloss").CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory);
            _textureEnvMapGlossView = resourceFactory.CreateTextureView(_textureEnvMapGloss);
            _textureBRDF = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory); ;
            _textureBRDFView = resourceFactory.CreateTextureView(_textureBRDF);
            _textureDiffuse = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory); ;
            _textureDiffuseView = resourceFactory.CreateTextureView(_textureDiffuse);
            _textureBumpmap = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory); ;
            _textureBumpmapView = resourceFactory.CreateTextureView(_textureBumpmap);
            _textureEffect = new ImageSharpTexture(ResourceLoader.GetEmbeddedResourceStream("VeldridNSViewExample.ThreeDee.brdf.png")).CreateDeviceTexture(_veldridView.GraphicsDevice, resourceFactory); ;
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



        private static Vertex[] GetCubeVertices()
        {
            var vertices = new Vertex[]
            {
                // Top
                new Vertex(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Bottom                                                             
                new Vertex(new Vector3(-0.5f,-0.5f, +0.5f),  new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f,-0.5f, +0.5f),  new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f,-0.5f, -0.5f),  new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f,-0.5f, -0.5f),  new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Left                                                               
                new Vertex(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Right                                                              
                new Vertex(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Back                                                               
                new Vertex(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Front                                                              
                new Vertex(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0))
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
