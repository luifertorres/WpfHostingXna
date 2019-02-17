using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Xna = Microsoft.Xna.Framework;

namespace TestApplication.Helpers
{
    /// <summary>
    /// Custom control uses the XNA Framework GraphicsDevice to render onto
    /// a WPF window. Derived classes can override the Initialize and Draw
    /// methods to add their own drawing code.
    /// </summary>
    public abstract class GraphicsDeviceControl : Control
    {
        #region Fields
        // However many GraphicsDeviceControl instances you have, they all share
        // the same underlying GraphicsDevice, managed by this helper service.
        GraphicsDeviceService graphicsDeviceService;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a GraphicsDevice that can be used to draw onto this control.
        /// </summary>
        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDeviceService.GraphicsDevice; }
        }

        /// <summary>
        /// Gets an IServiceProvider containing our IGraphicsDeviceService.
        /// This can be used with components such as the ContentManager,
        /// which use this service to look up the GraphicsDevice.
        /// </summary>
        public ServiceContainer Services { get; } = new ServiceContainer();

        /// <summary>
        /// Gets a handle to this control.
        /// </summary>
        public IntPtr Handle { get; private set; }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the control.
        /// </summary>
        public override void BeginInit()
        {
            Unloaded += (sender, e) => Dispose(disposing: true);

            // Don't initialize the graphics device if we are running in the designer.
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                var window = Application.Current.MainWindow;
                var source = (HwndSource)PresentationSource.FromVisual(window);

                Handle = source.Handle;

                graphicsDeviceService = GraphicsDeviceService.AddRef(Handle,
                                                                     (int)ActualWidth,
                                                                     (int)ActualHeight);

                // Register the service, so components like ContentManager can find it.
                Services.AddService<IGraphicsDeviceService>(graphicsDeviceService);

                // Give derived classes a chance to initialize themselves.
                Initialize();
            }

            base.BeginInit();
        }

        /// <summary>
        /// Disposes the control.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (graphicsDeviceService != null)
            {
                graphicsDeviceService.Release(disposing);
                graphicsDeviceService = null;
            }
        }
        #endregion

        #region Paint
        /// <summary>
        /// Redraws the control in response to a WinForms paint message.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            string beginDrawError = BeginDraw();

            if (string.IsNullOrEmpty(beginDrawError))
            {
                // Draw the control using the GraphicsDevice.
                Draw();
                EndDraw();
            }
            else
            {
                // If BeginDraw failed, show an error message using System.Drawing.
                PaintUsingSystemDrawing(drawingContext, beginDrawError);
            }

            //base.OnRender(drawingContext);
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        string BeginDraw()
        {
            // If we have no graphics device, we must be running in the designer.
            if (graphicsDeviceService == null)
            {
                return Name + "\n\n" + GetType();
            }

            // Make sure the graphics device is big enough, and is not lost.
            string deviceResetError = HandleDeviceReset();

            if (!string.IsNullOrEmpty(deviceResetError))
            {
                return deviceResetError;
            }

            // Many GraphicsDeviceControl instances can be sharing the same
            // GraphicsDevice. The device backbuffer will be resized to fit the
            // largest of these controls. But what if we are currently drawing
            // a smaller control? To avoid unwanted stretching, we set the
            // viewport to only use the top left portion of the full backbuffer.
            Viewport viewport = new Viewport();

            viewport.X = 0;
            viewport.Y = 0;

            viewport.Width = (int)ActualWidth;
            viewport.Height = (int)ActualHeight;

            viewport.MinDepth = 0;
            viewport.MaxDepth = 1;

            GraphicsDevice.Viewport = viewport;

            return null;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method, and is responsible for presenting
        /// the finished image onto the screen, using the appropriate WPF
        /// control handle to make sure it shows up in the right place.
        /// </summary>
        void EndDraw()
        {
            try
            {
                Xna.Rectangle destinationRectangle = new Xna.Rectangle(0, 0, (int)ActualWidth,
                                                                        (int)ActualHeight);

                GraphicsDevice.Present(null, destinationRectangle, Handle);
            }
            catch
            {
                // Present might throw if the device became lost while we were
                // drawing. The lost device will be handled by the next BeginDraw,
                // so we just swallow the exception.
            }
        }

        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        string HandleDeviceReset()
        {
            bool deviceNeedsReset = false;

            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    return "Graphics device lost";

                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;

                default:
                    // If the device state is ok, check whether it is big enough.
                    PresentationParameters pp = GraphicsDevice.PresentationParameters;

                    deviceNeedsReset = ((int)ActualWidth > pp.BackBufferWidth) ||
                                       ((int)ActualHeight > pp.BackBufferHeight);
                    break;
            }

            // Do we need to reset the device?
            if (deviceNeedsReset)
            {
                try
                {
                    graphicsDeviceService.ResetDevice((int)ActualWidth,
                                                      (int)ActualHeight);
                }
                catch (Exception e)
                {
                    return "Graphics device reset failed\n\n" + e;
                }
            }

            return null;
        }

        /// <summary>
        /// If we do not have a valid graphics device (for instance if the device
        /// is lost, or if we are running inside the Form designer), we must use
        /// regular System.Windows.Media.DrawingContext to display a status message.
        /// </summary>
        protected virtual void PaintUsingSystemDrawing(DrawingContext drawingContext, string text)
        {
            Background = Brushes.CornflowerBlue;

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var formattedText = new FormattedText(text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                Brushes.Black);
            var origin = new Point(0, 0);

            drawingContext.DrawText(formattedText, origin);
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Derived classes override this to initialize their drawing code.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Derived classes override this to draw themselves using the GraphicsDevice.
        /// </summary>
        protected abstract void Draw();
        #endregion
    }
}
