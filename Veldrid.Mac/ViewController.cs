using System;
using AppKit;
using Foundation;
using Veldrid;

namespace VeldridNSViewExample
{
    [Register("ViewController")]
    public class ViewController : NSSplitViewController
    {
        private VeldridView _veldridView;
        private CommandList _commandList;

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

        void VeldridView_DeviceReady()
        {
            _commandList = _veldridView.GraphicsDevice.ResourceFactory.CreateCommandList();
        }

        void VeldridView_Resized()
        {
        }

        void VeldridView_Rendering()
        {
            _commandList.Begin();
            _commandList.SetFramebuffer(_veldridView.MainSwapchain.Framebuffer);
            _commandList.ClearColorTarget(0, _clearColors[_frameIndex / _frameRepeatCount]);
            _commandList.End();
            _veldridView.GraphicsDevice.SubmitCommands(_commandList);
            _veldridView.GraphicsDevice.SwapBuffers(_veldridView.MainSwapchain);

            _frameIndex = (_frameIndex + 1) % (_clearColors.Length * _frameRepeatCount);
        }
    }
}
