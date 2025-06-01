using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SDLTTFSharp_FNA
{
    public static class SpriteBatchExtension
    {
        /// <summary>
        /// TTF文本绘制方法
        /// </summary>
        public static void DrawString(
            this SpriteBatch batch,
            TextRenderer.FreeTypeFont font,
            string text,
            Vector2 position,
            Color color,
            float scale = 1.0f,
            float layerDepth = 0f)
        {
            font.RenderString(batch, text, color, position, scale);
        }
        public static void DrawString(
            this SpriteBatch batch,
            TextRenderer.FreeTypeFont font,
            string text,
            Vector2 position,
            Color color,
            Color borderColor,
            float scale = 1.0f,
            float layerDepth = 0f)
        {
            font.RenderString(batch, text, color, borderColor, position, scale);
        }
    }
}
