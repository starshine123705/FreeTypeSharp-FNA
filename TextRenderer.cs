using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static SDLTTFSharp_FNA.FreeTypeNative;
using static SDLTTFSharp_FNA.TextRenderer.FreeTypeFont;
using SDL = SDL3.SDL;

namespace SDLTTFSharp_FNA
{
    public class TextRenderer
    {
        #region 核心数据结构

        public class FontCacheKey : IEquatable<FontCacheKey>
        {
            public string Name;
            public int Size;
            public FontStyle Style;

            public bool Equals(FontCacheKey other) =>
                Name == other.Name && Size == other.Size && Style == other.Style;

            public override int GetHashCode() =>
                HashCode.Combine(Name, Size, Style);
        }

        public class TextCacheKey : IEquatable<TextCacheKey>
        {
            public FontCacheKey FontKey;
            public string Text;
            public Color Color;

            public bool Equals(TextCacheKey other) =>
                FontKey.Equals(other.FontKey) &&
                Text == other.Text &&
                Color == other.Color;

            public override int GetHashCode() =>
                HashCode.Combine(FontKey, Text, Color);
        }

        #endregion
        #region 字体封装

        /// <summary>
        /// 封装的字体对象，支持样式设置
        /// </summary>
        public class FreeTypeFont : IDisposable
        {
            #region 核心数据结构
            private static GraphicsDevice? _graphicsDevice;
            #endregion

            #region 字体属性
            public string Name { get; private set; }
            public IntPtr Face { get; private set; }
            public int Size { get; }
            public string FontPath { get; }
            #endregion
            #region 结构体定义
            public struct StoredChar
            {
                public int atlasIndex;
                public uint width;
                public uint height;
                public uint xOffset;
                public uint yOffset;
                public uint advance;
                public uint bearingX;
                public uint bearingY;
                public int pitch;
            }
            #endregion
            #region 初始化与加载
            public FreeTypeFont(GraphicsDevice device, IntPtr library, string fontPath, int size, string name)
            {
                _graphicsDevice = device;
                FontPath = fontPath;
                Size = size;
                Face = library;
                Name = name;
            }
            public void SetSize(int size)
            {
                FT.SetFontSize(Face, (uint)size);
            }
            #endregion

            #region 文本渲染
            public unsafe bool RenderGlyphToAtlas(IntPtr context, int atlasIndex, char singleChar, out StoredChar? currentCharData)
            {
                if (atlasIndex < 0)
                {
                    currentCharData = null;
                    return false;
                }
                TextureAtlas atlas = _fontAtlas[atlasIndex];
                if (atlas.isFull)
                {
                    currentCharData = null;
                    return false;
                }
                IntPtr m = FT.RenderGlyph(context, singleChar);
                GlyphMetrics metrics = Marshal.PtrToStructure<GlyphMetrics>(m);
                Rectangle bestRect = new Rectangle();
                int bestShortSideFit = int.MaxValue;

                foreach (var rect in atlas.freeRects)
                {
                    int leftoverHoriz = Math.Abs((int)rect.Width - (int)metrics.Width);
                    int leftoverVert = Math.Abs((int)rect.Height - (int)metrics.Height);
                    int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

                    if (rect.Width >= metrics.Width && rect.Height >= metrics.Height)
                    {
                        if (shortSideFit < bestShortSideFit)
                        {
                            bestRect = rect;
                            bestShortSideFit = shortSideFit;
                        }
                    }
                }

                if (bestShortSideFit != int.MaxValue)
                {
                    // 分割空闲矩形
                    Rectangle newRect = new Rectangle(bestRect.X, bestRect.Y, (int)metrics.Width, (int)metrics.Height);

                    // 更新空闲矩形列表
                    if (bestRect.Width > newRect.Width)
                    {
                        new Rectangle(bestRect.X + newRect.Width, bestRect.Y, bestRect.Width - newRect.Width, bestRect.Height);
                    }
                    if (bestRect.Height > newRect.Height)
                    {
                        atlas.freeRects.Add(new Rectangle(bestRect.X, bestRect.Y + newRect.Height, bestRect.Width, bestRect.Height - newRect.Height));
                    }
                    atlas.textureRegion.SetDataPointerEXT(0, newRect, metrics.Buffer, (int)(metrics.Width * metrics.Height * 4));
                    StoredChar sc = new StoredChar
                    {
                        advance = metrics.Advance,
                        atlasIndex = atlasIndex,
                        bearingX = metrics.BearingX,
                        bearingY = metrics.BearingY,
                        xOffset = (uint)bestRect.X,
                        yOffset = (uint)bestRect.Y,
                        width = metrics.Width,
                        height = metrics.Height,
                        pitch = metrics.Pitch
                    };
                    chars.Add((singleChar, Name), sc);
                    currentCharData = sc;
                    return true;
                }
                atlas.isFull = true;
                currentCharData = null;
                FT.FreeGlyph(m);
                return false;
            }
            public unsafe void RenderString(SpriteBatch batch, string text, Color color, Vector2 pos, float scale)
            {
                int currentAdvance = 0;
                FT.MeasureString(Face, text, out int width, out int height, out int baselineY);
                Vector2 currentPos = new Vector2(pos.X, pos.Y + baselineY);
                foreach (var singleChar in text)
                {
                    if (chars.TryGetValue((singleChar, Name), out StoredChar value))
                    {
                        var atlas = _fontAtlas[value.atlasIndex];
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)value.bearingX), (int)(currentPos.Y - (int)value.bearingY), (int)value.width, (int)value.height), new Rectangle((int)value.xOffset, (int)value.yOffset, (int)value.width, (int)value.height), color);
                        currentPos.X += value.advance;
                    }
                    else if (RenderGlyphToAtlas(Face, _fontAtlas.Count - 1, singleChar, out StoredChar? currentCharData))
                    {
                        StoredChar currentChar = currentCharData.Value;
                        var atlas = _fontAtlas[^1];
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), color);
                        currentPos.X += currentChar.advance;
                    }
                    else
                    {
                        _fontAtlas.Add(new TextureAtlas());
                        RenderGlyphToAtlas(Face, _fontAtlas.Count - 1, singleChar, out StoredChar? currentCharData2);
                        StoredChar currentChar = currentCharData2.Value;
                        var atlas = _fontAtlas[^1];
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), color);
                        currentPos.X += currentChar.advance;
                    }
                }
            }
            public unsafe void RenderString(SpriteBatch batch, string text, Color color, Color borderColor, Vector2 pos, float scale)
            {
                int currentAdvance = 0;
                FT.MeasureString(Face, text, out int width, out int height, out int baselineY);
                Vector2 currentPos = new Vector2(pos.X, pos.Y + baselineY);
                foreach (var singleChar in text)
                {
                    if (chars.TryGetValue((singleChar, Name), out StoredChar value))
                    {
                        var atlas = _fontAtlas[value.atlasIndex];
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)value.bearingX), (int)(currentPos.Y - (int)value.bearingY) + 1, (int)value.width, (int)value.height), new Rectangle((int)value.xOffset, (int)value.yOffset, (int)value.width, (int)value.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)value.bearingX), (int)(currentPos.Y - (int)value.bearingY) - 1, (int)value.width, (int)value.height), new Rectangle((int)value.xOffset, (int)value.yOffset, (int)value.width, (int)value.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)value.bearingX) + 1, (int)(currentPos.Y - (int)value.bearingY), (int)value.width, (int)value.height), new Rectangle((int)value.xOffset, (int)value.yOffset, (int)value.width, (int)value.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)value.bearingX) - 1, (int)(currentPos.Y - (int)value.bearingY), (int)value.width, (int)value.height), new Rectangle((int)value.xOffset, (int)value.yOffset, (int)value.width, (int)value.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)value.bearingX), (int)(currentPos.Y - (int)value.bearingY), (int)value.width, (int)value.height), new Rectangle((int)value.xOffset, (int)value.yOffset, (int)value.width, (int)value.height), color);
                        currentPos.X += value.advance;
                    }
                    else if (RenderGlyphToAtlas(Face, _fontAtlas.Count - 1, singleChar, out StoredChar? currentCharData))
                    {
                        StoredChar currentChar = currentCharData.Value;
                        var atlas = _fontAtlas[^1];
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX) + 1, (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX) - 1, (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY) + 1, (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY) - 1, (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), color);
                        currentPos.X += currentChar.advance;
                    }
                    else
                    {
                        _fontAtlas.Add(new TextureAtlas());
                        RenderGlyphToAtlas(Face, _fontAtlas.Count - 1, singleChar, out StoredChar? currentCharData2);
                        StoredChar currentChar = currentCharData2.Value;
                        var atlas = _fontAtlas[^1];
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX) + 1, (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX) - 1, (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY) + 1, (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY) - 1, (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), borderColor);
                        batch.Draw(atlas.textureRegion, new Rectangle((int)(currentPos.X + (int)currentChar.bearingX), (int)(currentPos.Y - (int)currentChar.bearingY), (int)currentChar.width, (int)currentChar.height), new Rectangle((int)currentChar.xOffset, (int)currentChar.yOffset, (int)currentChar.width, (int)currentChar.height), color);
                        currentPos.X += currentChar.advance;
                    }
                }
            }
            #endregion

            #region 额外方法
            public Vector3 MeasureString(string text)
            {
                FT.MeasureString(Face, text, out int width, out int height, out int baselineY);
                return new Vector3(width, height, baselineY);
            }
            #endregion

            #region 资源清理
            public void Dispose()
            {

                if (Face != IntPtr.Zero)
                {
                    FT.DisposeFont(Face);
                    Face = IntPtr.Zero;
                }
            }
            #endregion
        }

        public static bool Draw = true;
        #endregion

        public static Dictionary<(char, string), StoredChar> chars = new Dictionary<(char, string), StoredChar>();
        public struct TextureAtlas
        {
            public bool isFull = false;
            public Texture2D textureRegion = new Texture2D(device, 2048, 2048);
            public List<Rectangle> freeRects = new List<Rectangle> { new Rectangle(0, 0, 2048, 2048) }; // 新增空闲矩形列表

            public TextureAtlas()
            {
            }
        }
        private static GraphicsDevice? device;
        private readonly Dictionary<string, string> _fontCache = new Dictionary<string, string>();
        private readonly Dictionary<string, FreeTypeFont> _registeredFonts = new Dictionary<string, FreeTypeFont>();
        private static List<TextureAtlas> _fontAtlas = new List<TextureAtlas>();
        public TextRenderer(GraphicsDevice device)
        {
            TextRenderer.device = device;
        }

        public void RegisterFont(string fontName, string fontPath)
        {
            var path = Path.Combine(Environment.CurrentDirectory, fontPath);
            _fontCache[fontName] = path;
        }

        public FreeTypeFont GetFont(string fontName, int size)
        {
            if (_fontCache.TryGetValue(fontName, out var path))
            {
                if (_registeredFonts.TryGetValue(fontName + size, out var font))
                {
                    return font;
                }
                var context = FT.CreateFTFont(path, size);
                var newFont = new FreeTypeFont(device, context, path, size, fontName + size);
                _registeredFonts.Add(fontName + size, newFont);
                return newFont;
            }
            throw new FreeTypeException($"字体未注册: {fontName}");
        }

        public void Dispose()
        {
            foreach (var font in _registeredFonts.Values)
            {
                font.Dispose();
            }
            _registeredFonts.Clear();
        }

        #region 错误处理

        public static string GetLastError()
        {
            return SDL.SDL_GetError();
        }

        public class FreeTypeException : Exception
        {
            public FreeTypeException(string message) : base(message) { }
        }

        #endregion
    }

    #region 辅助结构

    [Flags]
    public enum FontStyle
    {
        Normal = 0,
        Bold = 0x1,
        Italic = 0x2,
        Underline = 0x4,
        Strikethrough = 0x8
    }

    public struct TextRenderOptions
    {
        public float WrapWidth;
        public TextAlignment Alignment;
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    #endregion
}
