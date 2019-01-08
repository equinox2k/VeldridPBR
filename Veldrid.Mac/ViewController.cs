using System;
using AppKit;
using Foundation;
using Veldrid;
using System.Numerics;
using PNI.Rendering.Harmony;
using PNI.Rendering.Harmony.Render;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PNI.Rendering.Harmony.Model;

namespace VeldridNSViewExample
{
    [Register("ViewController")]
    public class ViewController : NSViewController
    {

        private string _option;
        private SurfaceGroups _surfaceGroups;

        private Mesh _screenMesh;
        private ModelRender _modelRender;
        private OutputRender _outputRender;
        private VeldridView _veldridView;
        private CommandList _commandList;
        private float _effectValue;

        private DateTime _prevUpdateTime;

        private Task<byte[]> ReadModel(string retailerCode, string vendor, string assetCode)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var apiClient = new HttpClient
            {
                BaseAddress = new Uri("https://pni-dev-render3d.pnidev.com/api/")
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"package/{retailerCode}/{vendor}/{assetCode}");
            var result = apiClient.SendAsync(requestMessage, cancellationTokenSource.Token).Result;
            if (result.IsSuccessStatusCode)
            {
                var data = result.Content.ReadAsByteArrayAsync();
                return data;
            }
            return null;
        }

        public ViewController(IntPtr handle) : base(handle)
        {
            //var data = ReadModel("samus", "photoapp", "acrylicprint20x30h").Result;
            var data = ReadModel("samus", "photoapp", "canvasprint11x14v").Result;
            //var data = ReadModel("pni", "pni", "pni_cubev2").Result;
            LoadModelData(data);
        }

        public void LoadModelData(byte[] data, string option = null)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                if (!ModelReader.Unpack(memoryStream, out var surfaceGroups))
                {
                    return;
                }
                _option = option;
                _surfaceGroups = surfaceGroups;
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _veldridView = new VeldridView
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


        private void CreateResourceProperties(ResourceFactory resourceFactory, SurfaceProperties surfaceProperties, Vector4 uvDimension, Image<Rgba32> photo1, Image<Rgba32> photo2)
        {
            if (surfaceProperties.SurfaceType == SurfaceTypeEnum.Photo1)
            {
                surfaceProperties.Diffuse.File = "[Photo1]";
            }
            else if (surfaceProperties.SurfaceType == SurfaceTypeEnum.Photo2)
            {
                surfaceProperties.Diffuse.File = "[Photo2]";
            }
            if (surfaceProperties.Diffuse.File.Length > 0)
            {
                var image = Imaging.GetModelTexture(surfaceProperties, surfaceProperties.Diffuse, uvDimension, _surfaceGroups.ModelResources, photo1, photo2);
                surfaceProperties.Diffuse.TextureView = Imaging.ImageToTextureView(image, _veldridView.GraphicsDevice, resourceFactory);
                surfaceProperties.Diffuse.ResetNeedsRefresh();
            }
            if (surfaceProperties.Bumpmap.File.Length > 0)
            {
                var image = Imaging.GetModelTexture(surfaceProperties, surfaceProperties.Bumpmap, uvDimension, _surfaceGroups.ModelResources, photo1, photo2);
                surfaceProperties.Bumpmap.TextureView = Imaging.ImageToTextureView(image, _veldridView.GraphicsDevice, resourceFactory);
                surfaceProperties.Bumpmap.ResetNeedsRefresh();
            }
            if (surfaceProperties.Effect.File.Length > 0)
            {
                var image = Imaging.GetModelTexture(surfaceProperties, surfaceProperties.Effect, uvDimension, _surfaceGroups.ModelResources, photo1, photo2);
                surfaceProperties.Effect.TextureView = Imaging.ImageToTextureView(image, _veldridView.GraphicsDevice, resourceFactory);
                surfaceProperties.Effect.ResetNeedsRefresh();
            }
        }

        Image<Rgba32> photo1;
        Image<Rgba32> photo2;

        private void CreateResources(ResourceFactory resourceFactory)
        {
            using (var imageStream = ResourceLoader.GetEmbeddedResourceStream("PNI.Rendering.Harmony.Resources.gloss_negx.jpg"))
            {
                photo1 = Image.Load(imageStream);
            }

            using (var imageStream = ResourceLoader.GetEmbeddedResourceStream("PNI.Rendering.Harmony.Resources.gloss_negx.jpg"))
            {
                photo2 = Image.Load(imageStream);
            }

            _veldridView.Camera.ModelScale = _surfaceGroups.Scale;
            _veldridView.Camera.Eye = new Vector3(0, 0, _veldridView.Camera.CalcZoom());

            _commandList = resourceFactory.CreateCommandList();

            _screenMesh = new Mesh(resourceFactory, _veldridView.GraphicsDevice, 0, 0, 2, 2, 2, 2);

            _surfaceGroups.CreateBuffers(_veldridView.GraphicsDevice, resourceFactory);

            foreach (var surfaceGroupInfo in _surfaceGroups.SurfaceGroupInfos)
            {
                CreateResourceProperties(resourceFactory, surfaceGroupInfo.Properties, surfaceGroupInfo.UvDimension, photo1, photo2);
                foreach (var option in surfaceGroupInfo.Properties.Options)
                {
                    CreateResourceProperties(resourceFactory, option, surfaceGroupInfo.UvDimension, photo1, photo2);
                }
            }

            var vertexLayoutDescription = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

            _modelRender = new ModelRender(_veldridView.GraphicsDevice, _veldridView.Camera, vertexLayoutDescription);

            _outputRender = new OutputRender(_veldridView.GraphicsDevice, _veldridView.MainSwapchain.Framebuffer, _veldridView.Camera, vertexLayoutDescription);
        }

        void VeldridView_DeviceReady()
        {
            CreateResources(_veldridView.GraphicsDevice.ResourceFactory);
        }

        void VeldridView_Resized()
        {
            _modelRender.Resize();
            _outputRender.Resize();
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

            _effectValue += 1.0f;
            if (_effectValue >= 360)
            {
                _effectValue = _effectValue - 360;
            }

            _commandList.Begin();

            // Render model

            _modelRender.Update(_commandList, _surfaceGroups, _option, _effectValue);

            // Render Output with skybox

            _outputRender.Update(_commandList, _screenMesh, _modelRender.GetColorTarget());

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

    }
}
