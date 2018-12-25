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
        private ModelRender _modelRender;

        private VeldridView _veldridView;
        private CommandList _commandList;

        private readonly Vertex[] _vertices;
        private readonly ushort[] _indices;

        private DeviceBuffer _modelMatrixBuffer;
        private DeviceBuffer _viewMatrixBuffer;
        private DeviceBuffer _projectionMatrixBuffer;
        //private ResourceSet _outputFragSet;

        //private DeviceBuffer _diffuseColorBuffer;
        //private DeviceBuffer _useTextureDiffuse;
        //private DeviceBuffer _useTextureBumpmap;
        //private DeviceBuffer _useTextureEffect;
        //private DeviceBuffer _effect;
        //private DeviceBuffer _lightDirection;
        //private DeviceBuffer _lightColor;
        //private DeviceBuffer _metallicRoughnessValues;
        //private DeviceBuffer _cameraPosition;
        //private Texture _textureEnvMapDiffuse;
        //private TextureView _textureEnvMapDiffuseView;
        //private Texture _textureEnvMapSpecular;
        //private TextureView _textureEnvMapSpecularView;
        //private Texture _textureEnvMapGloss;
        //private TextureView _textureEnvMapGlossView;
        //private Texture _textureBRDF;
        //private TextureView _textureBRDFView;
        //private Texture _textureDiffuse;
        //private TextureView _textureDiffuseView;
        //private Texture _textureBumpmap;
        //private TextureView _textureBumpmapView;
        //private Texture _textureEffect;
        //private TextureView _textureEffectView;
        //private Sampler _linearSampler;
        //private Sampler _pointSampler;
        //private ResourceSet _outputVertSet;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        private float _ticks;

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
            _vertices = GetCubeVertices();
            _indices = GetCubeIndices();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var graphicsDeviceOptions = new GraphicsDeviceOptions(false, PixelFormat.R16_UNorm, false, ResourceBindingModel.Improved, true, true);
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

            _vertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.SizeInBytes * _vertices.Length), BufferUsage.VertexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);
            _indexBuffer = resourceFactory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)_indices.Length, BufferUsage.IndexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);

            var vertexLayoutDescription = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

            _modelRender = new ModelRender(_veldridView.GraphicsDevice, resourceFactory, vertexLayoutDescription, _veldridView.MainSwapchain.Framebuffer, _modelMatrixBuffer, _viewMatrixBuffer, _projectionMatrixBuffer);
           
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
            _ticks += 10f;

            Matrix4x4 rotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, (_ticks / 1000f)) * Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, (_ticks / 3000f));

            _commandList.Begin();
            _commandList.UpdateBuffer(_modelMatrixBuffer, 0, ref rotation);
            _commandList.UpdateBuffer(_viewMatrixBuffer, 0, Matrix4x4.CreateLookAt(Vector3.UnitZ * 2.5f, Vector3.Zero, Vector3.UnitY));
            _commandList.UpdateBuffer(_projectionMatrixBuffer, 0, Matrix4x4.CreatePerspectiveFieldOfView(1.0f, (float)_veldridView.Width / (float)_veldridView.Height, 0.5f, 100f));

            _modelRender.Update(_commandList, _vertexBuffer, _indexBuffer);

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
