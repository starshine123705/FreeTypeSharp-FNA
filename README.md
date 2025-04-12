# FreeTypeSharp-FNA
This is an text drawing lib based on freetype lib for FNA,provides easy ways to draw an text to the screen.
if there's any problem,please open an issue,or send me a email.
My address:anilstar06@outlook.com
# Usage
```csharp
//You need to initialize the font first
TextRenderer.RegisterFont(string fontName, string fontPath);//wont create a font actually
TextRenderer.GetFont(sting fontName, int size);//the name must be registered before, or it will throw an error,same font will automatic use the cache
//then use the follow to draw, ensure your SpriteBatch BlendState has been set to AlphaBlend
//No Border
SpriteBatch.DrawString(FreeTypeFont context, string text, Vector2 position, Color color);
//With Border
SpriteBatch.DrawString(FreeTypeFont context, string text, Vector2 position, Color color, Color borderColor);
