using System;
using AppKit;
using Foundation;
using Veldrid;
using System.Numerics;
using PNI.Render3d.Core;
using PNI.Render3d.Core.Render;

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
    public class ViewController : NSViewController
    {

        private Camera _camera;
        private Mesh _screenMesh;
        private DepthNormalRender _depthNormalRender;
        private DepthLimitedBlurRender _depthLimitedBlurRender;
        private SaoRender _saoRender;
        private ModelRender _modelRender;
        private OutputRender _outputRender;
        private VeldridView _veldridView;
        private CommandList _commandList;

        private readonly Vertex[] _vertices;
        private readonly ushort[] _indices;
        private readonly Vertex[] _verticesMesh;
        private readonly ushort[] _indicesMesh;

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _vertexMeshBuffer;
        private DeviceBuffer _indexMeshBuffer;
        private DateTime _prevUpdateTime;

        public ViewController(IntPtr handle) : base(handle)
        {
            _vertices = GetCubeVertices();
            _indices = GetCubeIndices();

            _screenMesh = new Mesh(0, 0, 2, 2, 2, 2);
            _verticesMesh = _screenMesh.Vertices;
            _indicesMesh = _screenMesh.Indices;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var graphicsDeviceOptions = new GraphicsDeviceOptions(true, PixelFormat.R32_Float, false, ResourceBindingModel.Improved, true, true);
            _veldridView = new VeldridView(GraphicsBackend.OpenGL, graphicsDeviceOptions)
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

        private void CreateResources(ResourceFactory resourceFactory)
        {
            _camera = new Camera
            {
                Eye = new Vector3(0, 0, 5.0f)
            };

            _commandList = resourceFactory.CreateCommandList();

            _vertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.SizeInBytes * _vertices.Length), BufferUsage.VertexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);
            _indexBuffer = resourceFactory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)_indices.Length, BufferUsage.IndexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);

            _vertexMeshBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.SizeInBytes * _verticesMesh.Length), BufferUsage.VertexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_vertexMeshBuffer, 0, _verticesMesh);
            _indexMeshBuffer = resourceFactory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)_indicesMesh.Length, BufferUsage.IndexBuffer));
            _veldridView.GraphicsDevice.UpdateBuffer(_indexMeshBuffer, 0, _indicesMesh);

            var vertexLayoutDescription = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));
                
            _depthNormalRender = new DepthNormalRender(_veldridView.GraphicsDevice, _camera, vertexLayoutDescription);
            _depthLimitedBlurRender = new DepthLimitedBlurRender(_veldridView.GraphicsDevice, _camera, vertexLayoutDescription);
            _saoRender = new SaoRender(_veldridView.GraphicsDevice, _camera, vertexLayoutDescription);

            _modelRender = new ModelRender(_veldridView.GraphicsDevice, _camera, vertexLayoutDescription);

            _outputRender = new OutputRender(_veldridView.GraphicsDevice, _veldridView.MainSwapchain.Framebuffer, _camera, vertexLayoutDescription);
        }

        void VeldridView_DeviceReady()
        {
            CreateResources(_veldridView.GraphicsDevice.ResourceFactory);
        }

        void VeldridView_Resized()
        {
            _camera.Width = _veldridView.Width;
            _camera.Height = _veldridView.Height;

            _depthNormalRender.Resize();
            _depthLimitedBlurRender.Resize();
            _modelRender.Resize();
            _outputRender.Resize();
            _saoRender.Resize();
        }

        //mesh create

        void VeldridView_Rendering()
        {
            var curUpdateTime = DateTime.Now;
            if (_prevUpdateTime.Ticks == 0)
            {
                _prevUpdateTime = curUpdateTime;
            }
            var dt = (float)(curUpdateTime - _prevUpdateTime).TotalSeconds;
            if (dt <= 0)
            {
                dt = float.Epsilon;
            }

            _camera.ModelMatrix = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, dt / 2) * Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, dt);

            _commandList.Begin();

            // Render depth + model

            _depthNormalRender.Update(_commandList, _vertexBuffer, _indexBuffer);
            var depthNormalTexture = _depthNormalRender.GetColorTarget();
            _saoRender.Update(_commandList, _vertexMeshBuffer, _indexMeshBuffer, _depthNormalRender.GetColorTarget());
            _modelRender.Update(_commandList, _vertexBuffer, _indexBuffer);

            // Blur Sao

            _depthLimitedBlurRender.Update(_commandList, _vertexMeshBuffer, _indexMeshBuffer, depthNormalTexture, _saoRender.GetColorTarget(), false);
            using (var blurHorizontalSao = _veldridView.GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(_camera.Width, _camera.Height, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled)))
            {
                _commandList.CopyTexture(_depthLimitedBlurRender.GetColorTarget(), blurHorizontalSao);
                _depthLimitedBlurRender.Update(_commandList, _vertexMeshBuffer, _indexMeshBuffer, depthNormalTexture, blurHorizontalSao, true);
            }

            // Render Output with skybox

            _outputRender.Update(_commandList, _vertexMeshBuffer, _indexMeshBuffer, _modelRender.GetColorTarget(), _depthLimitedBlurRender.GetColorTarget());

            _commandList.End();

            _veldridView.GraphicsDevice.SubmitCommands(_commandList);

            if (_veldridView.Backend == GraphicsBackend.OpenGL)
            {
                _veldridView.GraphicsDevice.SwapBuffers();
            }
            else
            {
                _veldridView.GraphicsDevice.SwapBuffers(_veldridView.MainSwapchain);
            }
        }

        private static Vertex[] GetCubeVertices()
        {
            return new Vertex[]
            {
                // Top
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Bottom                                                             
                new Vertex(new Vector3(-0.5f,+0.5f, +0.5f),  new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f,+0.5f, +0.5f),  new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f,+0.5f, -0.5f),  new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f,+0.5f, -0.5f),  new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Left                                                               
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Right                                                              
                new Vertex(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Back                                                               
                new Vertex(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                // Front                                                              
                new Vertex(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0)),
                new Vertex(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 1), new Vector3(0, 0, 1), new Vector3(1, 0, 0))
            };
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
