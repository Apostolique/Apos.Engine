using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public enum RectStyle { Inline = 0, Centered = 1, Outline = 2 }

    public static class SpriteBatchExtensions
    {
        public static Texture2D Pixel { get; private set; }

        public static readonly Vector2 PixelOrigin = new Vector2(.5f);

        static readonly Vector2[] _lineOrigin = { new Vector2(0, 0), new Vector2(0, .5f), new Vector2(0, 1) };

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            Pixel = new Texture2D(graphicsDevice, 1, 1);
            Pixel.SetData(new Color[] { Color.White });
        }

        public static void DrawPixel(this SpriteBatch s, Vector2 pos, Color color, float rotation = 0, float scale = 1, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, PixelOrigin, scale, 0, layer);

        public static void DrawRectangle(this SpriteBatch s, Rectangle rect, RectStyle style, Color color, float thickness = 1, float layer = 0)
        {
            DrawLine(s, (rect.Location.ToVector2(), new Vector2(rect.Right, rect.Top)), color, thickness, style, layer);
            DrawLine(s, (new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom)), color, thickness, style, layer);
            DrawLine(s, (new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom)), color, thickness, style, layer);
            DrawLine(s, (new Vector2(rect.Left, rect.Bottom), rect.Location.ToVector2()), color, thickness, style, layer);
        }
        public static void FillRectangle(this SpriteBatch s, Rectangle rect, Color color, float rotation = 0, float layer = 0) => s.Draw(Pixel, rect, null, color, rotation, Vector2.Zero, 0, layer);
        public static void FillRectangle(this SpriteBatch s, Vector2 pos, Vector2 scale, Color color, float rotation = 0, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, PixelOrigin, scale, 0, layer);
        public static void FillRectangle(this SpriteBatch s, Vector2 pos, float scale, Color color, float rotation = 0, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, PixelOrigin, scale, 0, layer);

        public static void DrawLine(this SpriteBatch s, (Vector2 A, Vector2 B) pos, Color color, float thickness = 1, RectStyle rectStyle = RectStyle.Centered, float layer = 0) => s.Draw(Pixel, pos.A, null, color, MathF.Atan2(pos.B.Y - pos.A.Y, pos.B.X - pos.A.X), _lineOrigin[(int)rectStyle], new Vector2(Vector2.Distance(pos.A, pos.B), thickness), 0, layer);
    }
}