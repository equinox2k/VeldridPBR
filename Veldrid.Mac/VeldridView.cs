﻿using System;
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

        public GraphicsBackend Backend { get; protected set; }
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

        public VeldridView()
        {
            Backend = GraphicsBackend.Metal;
            _deviceOptions = new GraphicsDeviceOptions(true, PixelFormat.R32_Float, false, ResourceBindingModel.Improved, true);
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

            float dpiScale = (float)Window.BackingScaleFactor;
            _width = (uint)(Frame.Width < 0 ? 0 : Math.Ceiling(Frame.Width * dpiScale));
            _height = (uint)(Frame.Height < 0 ? 0 : Math.Ceiling(Frame.Height * dpiScale));

            _resized = true;

            if (!_initialized)
            {
                _initialized = true;

                var a = _deviceOptions.PreferDepthRangeZeroToOne;
                var b = _deviceOptions.PreferStandardClipSpaceYDirection;

                var swapchainSource = SwapchainSource.CreateNSView(Handle);
                var swapchainDescription = new SwapchainDescription(swapchainSource, Width, Height, _deviceOptions.SwapchainDepthFormat, true, true);

                //Once opengl working enable this to auto switch
                if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
                {
                    //Metal
                }
                else
                {
                    //OpenGL
                }

                if (Backend == GraphicsBackend.Metal)
                {
                    GraphicsDevice = GraphicsDevice.CreateMetal(_deviceOptions);
                    MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
                }

                if (Backend == GraphicsBackend.OpenGL)
                {
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

                    context = new NSOpenGLContext(new NSOpenGLPixelFormat(pixelAttrs), null)
                    {
                        View = this
                    };
                    context.MakeCurrentContext();

                    var platformInfo = new OpenGLPlatformInfo(
                        context.Handle, GetProcAddress, MakeCurrent, GetCurrentContext, ClearCurrentContext, DeleteContext, SwapBuffers, null
                    );

                    GraphicsDevice = GraphicsDevice.CreateOpenGL(_deviceOptions, platformInfo, Width, Height);
                    MainSwapchain = GraphicsDevice.MainSwapchain;


                }

                var c = GraphicsDevice.IsClipSpaceYInverted; // metal = false, gl = false
                var d = GraphicsDevice.IsDepthRangeZeroToOne; // metal = true, gl = false
                var e = GraphicsDevice.IsUvOriginTopLeft;

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

        public static IntPtr GetProcAddress(string function)
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
                    BeginInvokeOnMainThread(() =>
                    {
                        if (_resized)
                        {
                            _resized = false;
                            if (Backend == GraphicsBackend.OpenGL)
                            {
                                context.Update();
                            }
                            MainSwapchain.Resize(Width, Height);
                            Resized?.Invoke();
                        }
                        Rendering?.Invoke();
                    });
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
