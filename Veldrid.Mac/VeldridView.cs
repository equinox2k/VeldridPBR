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
        private bool _initialized;

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

        public uint Width => _width < 1 ? 1 : _width;

        public uint Height => _height < 1 ? 1 : _height;

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

        public override void Layout()
        {
            base.Layout();

            const double dpiScale = 1;
            _width = (uint)(Frame.Width < 0 ? 0 : Math.Ceiling(Frame.Width * dpiScale));
            _height = (uint)(Frame.Height < 0 ? 0 : Math.Ceiling(Frame.Height * dpiScale));

            _resized = true;

            if (!_initialized)
            {
                _initialized = true;

                var swapchainSource = SwapchainSource.CreateNSView(Handle);
                var swapchainDescription = new SwapchainDescription(swapchainSource, Width, Height, _deviceOptions.SwapchainDepthFormat, true, true);

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
                        MainSwapchain.Resize(Width, Height);
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
