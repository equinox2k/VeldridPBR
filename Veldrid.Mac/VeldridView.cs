using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AppKit;
using CoreVideo;
using Veldrid;
using Veldrid.OpenGL;

namespace VeldridNSViewExample
{
    public class VeldridView : NSView
    {
        private static NSOpenGLContext context;


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

        public unsafe override void Layout()
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
                    MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);

                }

                if (_backend == GraphicsBackend.OpenGL)
                {
                    var pixelAttrs = new object[] {
                            NSOpenGLPixelFormatAttribute.Accelerated,
                            NSOpenGLPixelFormatAttribute.NoRecovery,
                            NSOpenGLPixelFormatAttribute.DoubleBuffer,
                            NSOpenGLPixelFormatAttribute.OpenGLProfile, NSOpenGLProfile.VersionLegacy,
                            NSOpenGLPixelFormatAttribute.ColorSize, 24,
                            NSOpenGLPixelFormatAttribute.AlphaSize, 8,
                            NSOpenGLPixelFormatAttribute.DepthSize, 24,
                            NSOpenGLPixelFormatAttribute.StencilSize, 8,
                            NSOpenGLPixelFormatAttribute.Multisample,
                            NSOpenGLPixelFormatAttribute.SampleBuffers, 1,
                            NSOpenGLPixelFormatAttribute.Samples, 4
                         };

                    var pixelFormat = new NSOpenGLPixelFormat(pixelAttrs);

                    context = new NSOpenGLContext(pixelFormat, null);
                    context.MakeCurrentContext();

                    //Veldrid.OpenGLBinding.OpenGLNative.LoadGetString(context.Handle, GetProcAddress);
                    //Veldrid.OpenGLBinding.OpenGLNative.LoadAllFunctions(context.Handle, GetProcAddress, false);



                    var platformInfo = new OpenGLPlatformInfo(
                        context.Handle,
                        GetProcAddress,
                        MakeCurrent,
                        GetCurrentContext,
                        ClearCurrentContext,
                        DeleteContext,
                        SwapBuffers,
                        SetSyncToVerticalBlank
                    );

                    GraphicsDevice = GraphicsDevice.CreateOpenGL(_deviceOptions, platformInfo, Width, Height);


                    MainSwapchain = GraphicsDevice.MainSwapchain;
                    //MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);

                }

                DeviceReady?.Invoke();

                _displayLink = new CVDisplayLink();
                _displayLink.SetOutputCallback(HandleDisplayLinkOutputCallback);
                _displayLink.Start();
            }
        }

        public static void MakeCurrent(IntPtr handle)
        {
            context.MakeCurrentContext();
        }

        public static IntPtr GetCurrentContext()
        {
            return NSOpenGLContext.CurrentContext.Handle;
        }

        public static void ClearCurrentContext()
        {
            NSOpenGLContext.ClearCurrentContext();
        }

        public static void DeleteContext(IntPtr ptr)
        {
            Debug.Print("DeleteContext");
        }

        public static void SwapBuffers()
        {
            Debug.Print("SwapBuffers");
        }

        public static void SetSyncToVerticalBlank(bool sync)
        {
            Debug.Print("SetSyncToVerticalBlank");
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

        public static IntPtr GetProcAddress(string function)
        {
            // Instead of allocating and combining strings in managed memory
            // we do that directly in unmanaged memory. This way, we avoid
            // 2 string allocations every time this function is called.

            // must add a '_' prefix and null-terminate the function name,
            // hence we allocate +2 bytes
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

        public static IntPtr GetAddress(IntPtr function)
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
