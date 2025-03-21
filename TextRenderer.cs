extern alias FNA;
using FNA::Microsoft.Xna.Framework;
using FNA::Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using static SDLTTFSharp_FNA.FreeTypeNative;
using SDL = FNA::SDL3.SDL;

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

        public class TextureAtlasPage
        {
            public Texture2D Texture;
            public int CurrentY;
            public readonly List<Rectangle> Regions = new List<Rectangle>();
        }

        #endregion
        #region 字体封装

        /// <summary>
        /// 封装的字体对象，支持样式设置
        /// </summary>
        public class FreeTypeFont : IDisposable
        {
            #region 核心数据结构
            private const int AtlasPageSize = 2048;
            private readonly List<TextureAtlasPage> _atlasPages = new List<TextureAtlasPage>();
            private readonly Dictionary<string, (TextureAtlasPage page, Rectangle region)> _glyphCache = new Dictionary<string, (TextureAtlasPage, Rectangle)>();
            private readonly GraphicsDevice _graphicsDevice;

            private class TextureAtlasPage
            {
                public Texture2D Texture { get; set; }
                public int CurrentY { get; set; }
                public List<Rectangle> Regions { get; } = new List<Rectangle>();
            }
            #endregion

            #region 字体属性
            public IntPtr Face { get; private set; }
            public int Size { get; }
            public string FontPath { get; }
            #endregion

            #region 初始化与加载
            public FreeTypeFont(GraphicsDevice device, IntPtr library, string fontPath, int size)
            {
                _graphicsDevice = device;
                FontPath = fontPath;
                Size = size;
                Face = library;
            }
            public void SetSize(int size)
            {
                FT.SetFontSize(Face, (uint)size);
            }
            #endregion

            #region 文本渲染入口
            public unsafe (Texture2D texture, Rectangle sourceRect) RenderText(string text, Color color)
            {
                var cacheKey = $"{text}|{color.PackedValue}";

                if (_glyphCache.TryGetValue(cacheKey, out var cached))
                    return (cached.page.Texture, cached.region);
                var result = FT.RenderText(Face, text, color.R, color.G, color.B);
                var entry = AddToAtlas(result);
                _glyphCache[cacheKey] = entry;
                return (entry.page.Texture, entry.region);
            }
            #endregion

            private unsafe (TextureAtlasPage page, Rectangle region) AddToAtlas(BitmapResult bitmap)
            { 
                // 寻找可用页面
                foreach (var page in _atlasPages)
                {
                    if (TryAddToPage(page, bitmap.Width, bitmap.Height, out var rect))
                    {
                        UpdateTexture(page, bitmap, rect);
                        return (page, rect);
                    }
                }

                // 创建新页面
                var newPage = new TextureAtlasPage
                {
                    Texture = new Texture2D(_graphicsDevice, AtlasPageSize, AtlasPageSize, false, SurfaceFormat.Color),
                    CurrentY = 0
                };
                _atlasPages.Add(newPage);

                if (TryAddToPage(newPage, bitmap.Width, bitmap.Height, out var newRect))
                {
                    UpdateTexture(newPage, bitmap, newRect);
                    return (newPage, newRect);
                }

                throw new FreeTypeException("无法将字形添加到图集");
            }

            #region 图集管理
            private bool TryAddToPage(TextureAtlasPage page, int width, int height, out Rectangle rect)
            {
                // 简单垂直布局算法
                if (page.CurrentY + height > AtlasPageSize)
                {
                    rect = default;
                    return false;
                }

                rect = new Rectangle(0, page.CurrentY, width, height);
                page.CurrentY += height;
                page.Regions.Add(rect);
                return true;
            }

            private unsafe void UpdateTexture(TextureAtlasPage page, BitmapResult bitmap, Rectangle rect)
            {
                var data = new Color[rect.Width * rect.Height];
                Color* src = (Color*)bitmap.Buffer;
                for (int i = 0; i < rect.Width * rect.Height; i++)
                {
                    data[i] = src[i];
                }
                page.Texture.SetData(0, rect, data, 0, data.Length);
            }
            #endregion

            #region 资源清理
            public void Dispose()
            {
                foreach (var page in _atlasPages)
                {
                    page.Texture?.Dispose();
                }
                _atlasPages.Clear();
                _glyphCache.Clear();

                if (Face != IntPtr.Zero)
                {
                    FT.DisposeFont(Face);
                    Face = IntPtr.Zero;
                }
            }
            #endregion
        }

        #endregion

        private GraphicsDevice device;
        private readonly Dictionary<string, string> _fontCache = new Dictionary<string, string>();

        public TextRenderer(GraphicsDevice device)
        {
            this.device = device;
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
                var context = FT.CreateFTFont(path, size);
                var newFont = new FreeTypeFont(device, context, path, size);
                return newFont;
            }
            throw new FreeTypeException($"字体未注册: {fontName}");
        }

        public void Dispose()
        {

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
