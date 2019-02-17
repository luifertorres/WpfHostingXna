using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using TestApplication.Helpers;

namespace TestApplication.Controls
{
    /// <summary>
    /// Example control inherits from GraphicsDeviceControl, which allows it to
    /// render using a GraphicsDevice. This control shows how to draw
    /// graphics inside a WPF application. It hooks the ComponentDispatcher.ThreadIdle
    /// event, using this to invalidate the control, which will cause the graphics
    /// to constantly redraw.
    /// </summary>
    public class XnaControl : GraphicsDeviceControl
    {
        private SpriteBatch _spriteBatch;
        private Texture2D _redTexture, _greenTexture;

        public BlendFunction ColorBlendFunction { get; set; }
        public Blend ColorSourceBlend { get; set; }
        public Blend ColorDestinationBlend { get; set; }
        public Color BackRectColor { get; set; } = Color.DarkRed;
        public Color FrontRectColor { get; set; } = Color.DarkGreen;

        static XnaControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(XnaControl), new FrameworkPropertyMetadata(typeof(XnaControl)));
        }

        /// <summary>
        /// Initializes the control.
        /// </summary>
        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _redTexture = new Texture2D(GraphicsDevice, 300, 200);
            _redTexture.SetData(Enumerable.Repeat(Color.Red, 300 * 200).ToArray());

            _greenTexture = new Texture2D(GraphicsDevice, 300, 200);
            _greenTexture.SetData(Enumerable.Repeat(Color.Green, 300 * 200).ToArray());

            // Hook the idle event to constantly redraw our animation.
            ComponentDispatcher.ThreadIdle += (sender, e) => InvalidateVisual();
        }

        /// <summary>
        /// Draws the control.
        /// </summary>
        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.White);

            var blendState = new BlendState
            {
                AlphaBlendFunction = ColorBlendFunction,
                AlphaSourceBlend = ColorSourceBlend,
                AlphaDestinationBlend = ColorDestinationBlend,

                ColorBlendFunction = ColorBlendFunction,
                ColorSourceBlend = ColorSourceBlend,
                ColorDestinationBlend = ColorDestinationBlend
            };

            _spriteBatch.Begin();
            _spriteBatch.Draw(_redTexture, new Rectangle(1, 1, 300, 200), BackRectColor);
            _spriteBatch.Draw(_greenTexture, new Rectangle(200, 200, 300, 200), FrontRectColor);
            _spriteBatch.End();
        }
    }
}
