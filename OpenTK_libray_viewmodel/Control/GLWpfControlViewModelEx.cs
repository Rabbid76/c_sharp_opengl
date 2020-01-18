using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using OpenTK.Platform;
using OpenTK.Graphics;         // GraphicsMode, Context
using OpenTK.Graphics.OpenGL;
using OpenTK.Wpf;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using OpenTK_libray_viewmodel.Model;


// [jayhf/OpenTkControl](https://github.com/jayhf/OpenTkControl)
// [freakinpenguin/OpenTK-WPF](https://github.com/freakinpenguin/OpenTK-WPF)
// [varon/GLWpfControl](https://github.com/varon/GLWpfControl)

namespace OpenTK_libray_viewmodel.Control
{
    public sealed class GLWpfControlSettingsEx
    {

        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public GraphicsContext ContextToUse { get; set; }

        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Default;

        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;

        /// The number of pixel buffer objects in use for pixel transfer.
        /// Must be >= 1. Setting this higher will mean more delays between frames showing up on the WPF control
        /// in software mode, but greatly improved render performance. Defaults to 2.
        public int PixelBufferObjectCount { get; set; } = 2;

        /// If this is set to true then direct mapping between OpenGL and WPF's D3D will be performed.
        /// If this is set to false, a slower but more compatible software copy is performed.
        public bool UseHardwareRender { get; set; } = true;

        /// Creates a copy of the settings.
        internal GLWpfControlSettingsEx Copy()
        {
            var c = new GLWpfControlSettingsEx
            {
                ContextToUse = ContextToUse,
                GraphicsContextFlags = GraphicsContextFlags,
                MajorVersion = MajorVersion,
                MinorVersion = MajorVersion,
                UseHardwareRender = UseHardwareRender,
                PixelBufferObjectCount = PixelBufferObjectCount
            };
            return c;
        }

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;

    }

    internal sealed class GLWpfControlRendererEx
    {

        [DllImport("kernel32.dll")]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);

        private readonly WriteableBitmap _bitmap;
        private readonly int _drawColorRB = 0;
        private readonly int _drawDepthRB = 0;
        private readonly int _drawColorTex = 0;
        private readonly int _drawDepthTex = 0;
        private readonly int _drawFB;
        private readonly int _RB;
        private readonly int _FB;

        private readonly Image _imageControl;
        private readonly bool _isHardwareRenderer;
        private readonly int[] _pixelBuffers;
        private bool _hasRenderedAFrame = false;

        public int FrameBuffer { get => _drawFB; }

        public int Width => _bitmap.PixelWidth;
        public int Height => _bitmap.PixelHeight;
        public int PixelBufferObjectCount => _pixelBuffers.Length;

        public GLWpfControlRendererEx(int width, int height, Image imageControl, bool isHardwareRenderer, int pixelBufferCount)
        {

            _imageControl = imageControl;
            _isHardwareRenderer = isHardwareRenderer;
            // the bitmap we're blitting to in software mode.
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            // TODO $$$ backup texture binding, framebuffer, render buffer and pixel pack buffer

            int samples = 8;

            // set up the draw render buffers and framebuffer
            if (samples > 1)
            {
                /*
                GL.ActiveTexture(TextureUnit.Texture0);
                _drawDepthTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2DMultisample, _drawDepthTex);
                GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, samples, PixelInternalFormat.DepthStencil, width, height, false);
                GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                _drawColorTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2DMultisample, _drawColorTex);
                GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, samples, PixelInternalFormat.Rgba8, width, height, false);
                GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                _drawFB = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _drawFB);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, _drawColorTex, 0);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2DMultisample, _drawDepthTex, 0);
                */
                _drawDepthRB = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _drawDepthRB);
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Depth24Stencil8, width, height);
                _drawColorRB = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _drawColorRB);
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, RenderbufferStorage.Rgba8, width, height);
                _drawFB = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _drawFB);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _drawDepthRB);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _drawColorRB);
            }
            else
            {
                _drawDepthRB = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _drawDepthRB);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
                _drawColorRB = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _drawColorRB);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
                _drawFB = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _drawFB);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _drawDepthRB);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _drawColorRB);
            }

            // set up the draw framebuffer

            var error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
                throw new GraphicsErrorException("Error creating frame buffer: " + error);

            // generate the frame buffer

            _RB = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _RB);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
            _FB = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _FB);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _RB);

            error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
                throw new GraphicsErrorException("Error creating frame buffer: " + error);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // generate the pixel buffers

            _pixelBuffers = new int[pixelBufferCount];
            // RGBA8 buffer
            var size = sizeof(byte) * 4 * width * height;
            for (var i = 0; i < _pixelBuffers.Length; i++)
            {
                var pb = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.PixelPackBuffer, pb);
                GL.BufferData(BufferTarget.PixelPackBuffer, size, IntPtr.Zero, BufferUsageHint.StreamRead);
                _pixelBuffers[i] = pb;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }

        public void DeleteBuffers()
        {
            if (_drawDepthRB != 0)
                GL.DeleteRenderbuffer(_drawDepthRB);
            if (_drawColorRB != 0)
                GL.DeleteRenderbuffer(_drawColorRB);
            if (_drawDepthTex != 0)
                GL.DeleteTexture(_drawDepthTex);
            if (_drawColorTex != 0)
                GL.DeleteTexture(_drawColorTex);
            GL.DeleteFramebuffer(_drawFB);
            GL.DeleteRenderbuffer(_RB);
            GL.DeleteRenderbuffer(_FB);
            GL.DeleteBuffers(_pixelBuffers.Length, _pixelBuffers);
        }

        // shifts all of the PBOs along by 1.
        private void RotatePixelBuffers()
        {
            var fst = _pixelBuffers[0];
            for (var i = 1; i < _pixelBuffers.Length; i++)
            {
                _pixelBuffers[i - 1] = _pixelBuffers[i];
            }
            _pixelBuffers[_pixelBuffers.Length - 1] = fst;
        }

        public void UpdateImage()
        {
            if (false && _isHardwareRenderer)
            {
                UpdateImageHardware();
            }
            else
            {
                UpdateImageSoftware();
            }

            _hasRenderedAFrame = true;
        }

        private void UpdateImageSoftware()
        {
            // [Pixel-path performance warning: Pixel transfer is synchronized with 3D rendering](https://stackoverflow.com/questions/49368575/pixel-path-performance-warning-pixel-transfer-is-synchronized-with-3d-rendering)
            // [Optimizing Texture Transfers](http://on-demand.gputechconf.com/gtc/2012/presentations/S0356-GTC2012-Texture-Transfers.pdf)

            int width = Width;
            int height = Height;

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _drawFB);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _FB);
            GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _FB);
            // start the (async) pixel transfer.
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _pixelBuffers[0]);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.ReadPixels(0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // rotate the pixel buffers.
            if (_hasRenderedAFrame)
            {
                RotatePixelBuffers();
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _pixelBuffers[0]);

            // copy the data over from a mapped buffer.
            _bitmap.Lock();
            var data = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            CopyMemory(_bitmap.BackBuffer, data, (uint)(sizeof(byte) * 4 * Width * Height));
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            _bitmap.Unlock();
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            if (!ReferenceEquals(_imageControl.Source, _bitmap))
            {
                _imageControl.Source = _bitmap;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }
        private void UpdateImageHardware()
        {
            // There are 2 options we can use here.
            // 1. Use a D3DSurface and WGL_NV_DX_interop to perform the rendering.
            //         This is still performing RTT (render to texture) and isn't as fast as just directly drawing the stuff onto the DX buffer.
            // 2. Steal the handles using hooks into DirectX, then use that to directly render.
            //         This is the fastest possible way, but it requires a whole lot of moving parts to get anything working properly.

            // references for (2):

            // Accessing WPF's Direct3D internals.
            // note: see the WPFD3dHack.zip file on the blog post
            // http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/438/How-to-get-access-to-WPF-s-internal-Direct3D-guts.aspx

            // Using API hooks from C# to get d3d internals
            // this would have to be adapted to WPF, but should/maybe work.
            // http://spazzarama.com/2011/03/14/c-screen-capture-and-overlays-for-direct3d-9-10-and-11-using-api-hooks/
            // https://github.com/spazzarama/Direct3DHook
            throw new NotImplementedException();
        }

    }


    public sealed class GLWpfControlEx
        : FrameworkElement
    {
        private const int ResizeUpdateInterval = 100;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private long _resizeStartStamp;
        private TimeSpan _lastFrameStamp;

        private IGraphicsContext _context;
        private IWindowInfo _windowInfo;

        private GLWpfControlSettingsEx _settings;
        private GLWpfControlRendererEx _renderer;
        private HwndSource _hwnd;

        /// Called whenever rendering should occur.
        public event Action<TimeSpan> Render;

        /// <summary>
        /// Gets called after the control has finished initializing and is ready to render
        /// </summary>
        public event Action Ready;

        // The image that the control uses
        private readonly Image _image;

        // Transformations and size 
        private TranslateTransform _translateTransform;
        private Rect _imageRectangle;

        static GLWpfControlEx()
        {
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative
            });
        }

        /// The OpenGL Framebuffer Object used internally by this component.
        /// Bind to this instead of the default framebuffer when using this component along with other FrameBuffers for the final pass.
        public int Framebuffer => _renderer?.FrameBuffer ?? 0;

        /// <summary>
        ///     Used to create a new control. Before rendering can take place, <see cref="Start(GLWpfControlSettingsEx)"/> must be called.
        /// </summary>
        public GLWpfControlEx()
        {
            _image = new Image()
            {
                Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform()
                {
                    ScaleY = -1
                }
            };
        }

        /// Starts the control and rendering, using the settings provided.
        public void Start(GLWpfControlSettingsEx settings)
        {
            _settings = settings;
            IsVisibleChanged += (_, args) => {
                if ((bool)args.NewValue)
                {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else
                {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (_context != null)
            {
                return;
            }
            if (_settings.ContextToUse == null)
            {
                var window = Window.GetWindow(this);
                var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
                _hwnd = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);
                _windowInfo = Utilities.CreateWindowsWindowInfo(_hwnd.Handle);

                var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);
                _context = new GraphicsContext(mode, _windowInfo, _settings.MajorVersion, _settings.MinorVersion,
                    _settings.GraphicsContextFlags);
                _context.LoadAll();
                _context.MakeCurrent(_windowInfo);
            }
            else
            {
                _context = _settings.ContextToUse;
            }

            if (_renderer == null)
            {
                var width = (int)RenderSize.Width;
                var height = (int)RenderSize.Height;
                _renderer = new GLWpfControlRendererEx(width, height, _image, _settings.UseHardwareRender, _settings.PixelBufferObjectCount);
            }

            _imageRectangle = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
            _translateTransform = new TranslateTransform(0, RenderSize.Height);

            Ready?.Invoke();
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (_context == null)
            {
                return;
            }

            ReleaseOpenGLResources();
            _windowInfo?.Dispose();
            _hwnd?.Dispose();
        }

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            if (_context == null || _renderer == null)
            {
                return;
            }

            if (_resizeStartStamp != 0)
            {
                if (_resizeStartStamp + ResizeUpdateInterval > _stopwatch.ElapsedMilliseconds)
                {
                    return;
                }

                _renderer?.DeleteBuffers();
                var width = (int)RenderSize.Width;
                var height = (int)RenderSize.Height;
                _renderer = new GLWpfControlRendererEx(width, height, _image, _settings.UseHardwareRender, _settings.PixelBufferObjectCount);

                _resizeStartStamp = 0;
            }

            if (!ReferenceEquals(GraphicsContext.CurrentContext, _context))
            {
                _context?.MakeCurrent(_windowInfo);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _renderer?.FrameBuffer ?? 0);
            TimeSpan deltaTime = _stopwatch.Elapsed - _lastFrameStamp;
            GL.Viewport(0, 0, (int)RenderSize.Width, (int)RenderSize.Height);
            Render?.Invoke(deltaTime);
            _renderer?.UpdateImage();
            InvalidateVisual();
            _lastFrameStamp = _stopwatch.Elapsed;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Transforms are applied in reverse order
            drawingContext.PushTransform(_translateTransform);              // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
            drawingContext.PushTransform(_image.RenderTransform);           // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg

            drawingContext.DrawImage(_image.Source, _imageRectangle);       // Draw the image source 

            drawingContext.Pop();                                           // Remove the scale transform
            drawingContext.Pop();                                           // Remove the translation transform

            base.OnRender(drawingContext);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            if (_renderer == null)
            {
                return;
            }

            _resizeStartStamp = _stopwatch.ElapsedMilliseconds;

            if (info.HeightChanged)
            {
                _imageRectangle.Height = info.NewSize.Height;
                _translateTransform.Y = info.NewSize.Height;
                InvalidateVisual();
            }
            if (info.WidthChanged)
            {
                _imageRectangle.Width = info.NewSize.Width;
                InvalidateVisual();
            }
            base.OnRenderSizeChanged(info);
        }

        private void ReleaseOpenGLResources()
        {
            _renderer?.DeleteBuffers();
            if (!_settings.IsUsingExternalContext)
            {
                _context?.Dispose();
                _context = null;
            }
        }
    }

    public class GLWpfControlViewModelEx
    {
        private GraphicsContext _context;
        private IWindowInfo _windowInfo;
        private GLWpfControlEx _glc;
        private IModel _model;
        private double _cx = 0;
        private double _cy = 0;
        private bool _initiliazed = false;
        private bool _disposed = false;
        private Stopwatch _stopWatch = new Stopwatch();

        double Width { get => _cx; }
        double Height { get => _cy; }

        public GLWpfControlViewModelEx(GLWpfControlEx glc, IModel model)
        {
            _glc = glc;
            _model = model;

            // Assign Load and Paint events of GLControl.

            Window window = Window.GetWindow(_glc.Parent);
            window.Closing += new CancelEventHandler(GLC_OnDestroy);

            _glc.SizeChanged += new SizeChangedEventHandler(GLC_OnSiceChanged);
            _glc.Render += GLC_OnPaint;
            _glc.MouseDown += new MouseButtonEventHandler(GLC_OnMouseDown);
            _glc.MouseUp += new MouseButtonEventHandler(GLC_OnMouseUp);
            _glc.MouseMove += new MouseEventHandler(GLC_OnMouseMove);
            _glc.MouseWheel += new MouseWheelEventHandler(GLC_OnMouseWheel);

            GraphicsMode mode = new GraphicsMode(32, 24, 8, 8);
            var gl_ctrl = new GLControl(mode, 4, 6, GraphicsContextFlags.Default | GraphicsContextFlags.Debug);
            gl_ctrl.CreateControl();
            this._windowInfo = gl_ctrl.WindowInfo;
            this._context = new GraphicsContext(mode, this._windowInfo);

            var settings = new GLWpfControlSettingsEx();
            settings.MajorVersion = 4;
            settings.MinorVersion = 6;
            settings.GraphicsContextFlags = GraphicsContextFlags.Default | GraphicsContextFlags.Debug;
            settings.ContextToUse = _context;

            _glc.Start(settings);
        }


        protected void GLC_OnDestroy(object sender, EventArgs e)
        {
            if (_disposed)
                return;
            _disposed = true;

            if (this._model != null)
                this._model.Dispose();
        }

        protected void GLC_OnSiceChanged(object sender, SizeChangedEventArgs e)
        {
            // [...]
        }

        protected void GLC_OnPaint(System.TimeSpan timespawn)
        {
            _cx = _glc.ActualWidth;
            _cy = _glc.ActualHeight;

            if (_initiliazed == false)
            {
                _initiliazed = true;
                if (this._model != null)
                    this._model.Setup((int)_cx, (int)_cy);
                _stopWatch.Start();
            }

            var span = _stopWatch.Elapsed;
            double app_t = span.TotalMilliseconds / 1000.0;

            if (this._model != null)
                this._model.Draw((int)_cx, (int)_cy, app_t);
        }

        protected void GLC_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            int mode = e.ChangedButton == MouseButton.Left ? 0 : 1;
            controls.Start(mode, wnd_pos);
        }

        protected void GLC_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            int mode = e.ChangedButton == MouseButton.Left ? 0 : 1;
            controls.End(mode, wnd_pos);
        }

        protected void GLC_OnMouseMove(object sender, MouseEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            controls.MoveCursorTo(wnd_pos);
        }

        protected void GLC_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            float distance = _model.GetScale();
            controls.MoveWheel(wnd_pos, (float)e.Delta * 0.001f * distance);
        }
    }
}