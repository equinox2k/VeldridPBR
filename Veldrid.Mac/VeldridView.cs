using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using AppKit;
using CoreGraphics;
using CoreVideo;
using PNI.Rendering.Harmony;
using Veldrid;
using Veldrid.OpenGL;

namespace VeldridNSViewExample
{
    public class VeldridView : NSView
    {
        public Camera Camera { get; protected set; }
        public GraphicsBackend Backend { get; protected set; }

        private NSOpenGLContext _context;
        private CVDisplayLink _displayLink;
        private bool _paused;
        private bool _resized;
        private bool _disposed;
        private bool _initialized;

        public GraphicsDevice GraphicsDevice { get; protected set; }
        public Swapchain MainSwapchain { get; protected set; }

        public event Action DeviceReady;
        public event Action Rendering;
        public event Action Resized;

        private float _rotationX;
        private float _rotationY;
        private float _rotationOffsetX;
        private float _rotationOffsetY;

        public VeldridView()
        {
            Camera = new Camera();
        }

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

            if (!_initialized)
            {
                _initialized = true;

                var graphicsDeviceOptions = new GraphicsDeviceOptions(true, PixelFormat.R32_Float, false, ResourceBindingModel.Improved, true);
                var swapchainSource = SwapchainSource.CreateNSView(Handle);
                var swapchainDescription = new SwapchainDescription(swapchainSource, Camera.Width, Camera.Height, graphicsDeviceOptions.SwapchainDepthFormat, true, true);

                //Once opengl working enable this to auto switch
                if (!GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
                {
                    Backend = GraphicsBackend.Metal;
                    GraphicsDevice = GraphicsDevice.CreateMetal(graphicsDeviceOptions);
                    MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
                }
                else
                {
                    Backend = GraphicsBackend.OpenGL;
                    WantsBestResolutionOpenGLSurface = true;

                    var pixelAttrs = new object[] {
                        NSOpenGLPixelFormatAttribute.Accelerated,
                        NSOpenGLPixelFormatAttribute.NoRecovery,
                        NSOpenGLPixelFormatAttribute.DoubleBuffer,
                        NSOpenGLPixelFormatAttribute.OpenGLProfile, NSOpenGLProfile.Version3_2Core,
                        NSOpenGLPixelFormatAttribute.ColorSize, 24,
                        NSOpenGLPixelFormatAttribute.AlphaSize, 8,
                        NSOpenGLPixelFormatAttribute.DepthSize, 24,
                        NSOpenGLPixelFormatAttribute.StencilSize, 8,
                        NSOpenGLPixelFormatAttribute.Multisample,
                        NSOpenGLPixelFormatAttribute.SampleBuffers, 1,
                        NSOpenGLPixelFormatAttribute.Samples, 4 };

                    _context = new NSOpenGLContext(new NSOpenGLPixelFormat(pixelAttrs), null)
                    {
                        View = this
                    };
                    _context.MakeCurrentContext();

                    var platformInfo = new OpenGLPlatformInfo(
                        _context.Handle, GetProcAddress, MakeCurrent, GetCurrentContext, ClearCurrentContext, DeleteContext, SwapBuffers, null
                    );

                    GraphicsDevice = GraphicsDevice.CreateOpenGL(graphicsDeviceOptions, platformInfo, Camera.Width, Camera.Height);
                    MainSwapchain = GraphicsDevice.MainSwapchain;

                }

                DeviceReady?.Invoke();

                _displayLink = new CVDisplayLink();
                _displayLink.SetOutputCallback(HandleDisplayLinkOutputCallback);
                _displayLink.Start();
            }
        }

        public void MakeCurrent(IntPtr handle)
        {
            var tempContext = (NSOpenGLContext)ObjCRuntime.Runtime.GetNSObject(handle);
            tempContext.MakeCurrentContext();
        }

        public IntPtr GetCurrentContext()
        {
            return NSOpenGLContext.CurrentContext.Handle;
        }

        public void ClearCurrentContext()
        {
            NSOpenGLContext.ClearCurrentContext();
        }

        public void DeleteContext(IntPtr handle)
        {
            var tempContext = (NSOpenGLContext)ObjCRuntime.Runtime.GetNSObject(handle);
            tempContext.Dispose();
        }

        public void SwapBuffers()
        {
            NSOpenGLContext.CurrentContext.FlushBuffer();
        }

        private const string Library = "libdl.dylib";

        [DllImport(Library, EntryPoint = "NSIsSymbolNameDefined")]
        private static extern bool NSIsSymbolNameDefined(string s);

        [DllImport(Library, EntryPoint = "NSIsSymbolNameDefined")]
        private static extern bool NSIsSymbolNameDefined(IntPtr s);

        [DllImport(Library, EntryPoint = "NSLookupAndBindSymbol")]
        private static extern IntPtr NSLookupAndBindSymbol(string s);

        [DllImport(Library, EntryPoint = "NSLookupAndBindSymbol")]
        private static extern IntPtr NSLookupAndBindSymbol(IntPtr s);

        [DllImport(Library, EntryPoint = "NSAddressOfSymbol")]
        private static extern IntPtr NSAddressOfSymbol(IntPtr symbol);

        private IntPtr GetProcAddress(string function)
        {
            var ptr = Marshal.AllocHGlobal(function.Length + 2);
            try
            {
                Marshal.WriteByte(ptr, (byte)'_');
                for (var i = 0; i < function.Length; i++)
                {
                    Marshal.WriteByte(ptr, i + 1, (byte)function[i]);
                }

                Marshal.WriteByte(ptr, function.Length + 1, 0); // null-terminate

                var symbol = GetAddress(ptr);
                return symbol;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private IntPtr GetAddress(IntPtr function)
        {
            var symbol = IntPtr.Zero;
            if (NSIsSymbolNameDefined(function))
            {
                symbol = NSLookupAndBindSymbol(function);
                if (symbol != IntPtr.Zero)
                {
                    symbol = NSAddressOfSymbol(symbol);
                }
            }

            return symbol;
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
                    BeginInvokeOnMainThread(() => {
                        var rect = ConvertRectToBacking(Bounds).Size;
                        if ((uint)rect.Width != Camera.Width || (uint)rect.Height != Camera.Height)
                        {
                            Camera.Width = (uint)rect.Width;
                            Camera.Height = (uint)rect.Height;
                            _resized = true;
                        }
                    });

                    if (_resized)
                    {
                        _resized = false;
                        if (Backend == GraphicsBackend.OpenGL)
                        {
                            _context.Update();
                        }
                        MainSwapchain.Resize(Camera.Width, Camera.Height);
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

        // Tracking

        private CGPoint _mouseStart;
        private NSTrackingArea _trackingArea;

        public override void UpdateTrackingAreas()
        {
            base.UpdateTrackingAreas();
            if (_trackingArea != null)
            {
                RemoveTrackingArea(_trackingArea);
            }
            var options = NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveWhenFirstResponder;
            _trackingArea = new NSTrackingArea(Bounds, options, this, null);
            AddTrackingArea(_trackingArea);
        }

        public override void MouseDown(NSEvent theEvent)
        {
            base.MouseDown(theEvent);
            _mouseStart = theEvent.LocationInWindow;
        }

        public override void MouseUp(NSEvent theEvent)
        {
            base.MouseUp(theEvent);
            _rotationX = _rotationX + _rotationOffsetX;
            _rotationY = _rotationY + _rotationOffsetY;
            _rotationOffsetX = 0;
            _rotationOffsetY = 0;
            Camera.RotationX = _rotationX + _rotationOffsetX;
            Camera.RotationY = _rotationY + _rotationOffsetY;
        }

        public override void MouseDragged(NSEvent theEvent)
        {
            base.MouseDragged(theEvent);
            _rotationOffsetX = (float)_mouseStart.Y - (float)theEvent.LocationInWindow.Y;
            _rotationOffsetY = (float)theEvent.LocationInWindow.X - (float)_mouseStart.X;
            Camera.RotationX = _rotationX + _rotationOffsetX;
            Camera.RotationY = _rotationY + _rotationOffsetY;
        }

        public float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public override void ScrollWheel(NSEvent theEvent)
        {
            base.ScrollWheel(theEvent);
            var near = Camera.Near + Camera.ModelScale;
            var far = Camera.Far - Camera.ModelScale;
            var step = (far - near) / 50.0f;
            var result = (float)theEvent.ScrollingDeltaY > 0 ? (Camera.Eye.Z + step) : (Camera.Eye.Z - step);
            Camera.Eye = new Vector3(0, 0, Clamp(result, near, far));
        }
    }
}
