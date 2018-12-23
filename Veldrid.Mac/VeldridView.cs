using System;
using System.Diagnostics;
using AppKit;
using CoreVideo;
using Veldrid;

namespace VeldridNSViewExample
{
    public class VeldridView : NSView
    {
        private readonly GraphicsBackend _backend;
        private readonly GraphicsDeviceOptions _deviceOptions;

        private CVDisplayLink _displayLink;
        private bool _paused;
        private bool _resized;
        private uint _width;
        private uint _height;
        private bool _disposed;

        public GraphicsDevice GraphicsDevice { get; protected set; }
        public Swapchain MainSwapchain { get; protected set; }

        public event Action DeviceReady;
        public event Action Rendering;
        public event Action Resized;

        public VeldridView(GraphicsBackend backend, GraphicsDeviceOptions deviceOptions)
        {
            if (!(backend == GraphicsBackend.Metal || backend == GraphicsBackend.OpenGL))
            {
                throw new NotSupportedException($"{backend} is not supported on windows.");
            }

            _backend = backend;
            _deviceOptions = deviceOptions;
        }

        public uint Width => _width;

        public uint Height => _height;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _displayLink.Stop();
                GraphicsDevice.Dispose();
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        public override void ViewDidMoveToWindow()
        {
            base.ViewDidMoveToWindow();

            var swapchainSource = SwapchainSource.CreateNSView(Handle);
            var swapchainDescription = new SwapchainDescription(swapchainSource, (uint)Frame.Width, (uint)Frame.Height, null, true, true);

            if (_backend == GraphicsBackend.Metal)
            {
                GraphicsDevice = GraphicsDevice.CreateMetal(_deviceOptions);
            }

            MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);

            DeviceReady?.Invoke();

            _displayLink = new CVDisplayLink();
            _displayLink.SetOutputCallback(HandleDisplayLinkOutputCallback);
            _displayLink.Start();
        }

        public override void Layout()
        {
            base.Layout();

            _resized = true;

            const double dpiScale = 1;
            _width = (uint)(Frame.Width < 0 ? 0 : Math.Ceiling(Frame.Width * dpiScale));
            _height = (uint)(Frame.Height < 0 ? 0 : Math.Ceiling(Frame.Height * dpiScale));
        }

        private CVReturn HandleDisplayLinkOutputCallback(CVDisplayLink displayLink, ref CVTimeStamp inNow, ref CVTimeStamp inOutputTime, CVOptionFlags flagsIn, ref CVOptionFlags flagsOut)
        {
            try
            {
                if (_paused)
                {
                    return CVReturn.Success;
                }
                if (GraphicsDevice != null)
                {
                    if (_resized)
                    {
                        _resized = false;
                        MainSwapchain.Resize(_width, _height);
                        Resized?.Invoke();
                    }
                    Rendering?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Encountered an error while rendering: " + e);
                throw;
            }
            return CVReturn.Success;
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }
    }
}
