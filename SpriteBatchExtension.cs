extern alias FNA;
using FNA::Microsoft.Xna.Framework;
using FNA::Microsoft.Xna.Framework.Graphics;

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
            var (texture, sourceRect) = font.RenderText(text, color);
            batch.Draw(
                texture,
                position,
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                layerDepth
            );
        }
    }
}
