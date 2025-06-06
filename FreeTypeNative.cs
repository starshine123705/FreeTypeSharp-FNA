﻿using System;
using System.Runtime.InteropServices;
using FT_Fixed = System.Int32;
using FT_Long = System.Int64;
using FT_Pos = System.Int64;
// 基本类型定义
using FT_UInt = System.UInt32;

namespace SDLTTFSharp_FNA
{
    public static class FreeTypeNative
    {

        // 错误代码枚举
        public enum FT_Error : int
        {
            OK = 0x00,
            // 其他错误代码可根据需要添加
        }

        // 加载标志枚举
        [Flags]
        public enum FT_Load_Flags : int
        {
            FT_LOAD_DEFAULT = 0x0,
            FT_LOAD_NO_SCALE = (1 << 0),
            FT_LOAD_NO_HINTING = (1 << 1),
            FT_LOAD_RENDER = (1 << 2),
            FT_LOAD_NO_BITMAP = (1 << 3),
            FT_LOAD_VERTICAL_LAYOUT = (1 << 4),
            FT_LOAD_FORCE_AUTOHINT = (1 << 5),
            FT_LOAD_CROP_BITMAP = (1 << 6),
            FT_LOAD_PEDANTIC = (1 << 7),
            FT_LOAD_IGNORE_GLOBAL_ADVANCE_WIDTH = (1 << 9),
            FT_LOAD_NO_RECURSE = (1 << 10),
            FT_LOAD_IGNORE_TRANSFORM = (1 << 11),
            FT_LOAD_MONOCHROME = (1 << 12),
            FT_LOAD_LINEAR_DESIGN = (1 << 13),
            FT_LOAD_NO_AUTOHINT = (1 << 14),
            FT_LOAD_COLOR = (1 << 15),
            FT_LOAD_COMPUTE_METRICS = (1 << 16)
        }

        // 字形度量结构
        [StructLayout(LayoutKind.Sequential)]
        public struct FT_Glyph_Metrics
        {
            public FT_Pos width;
            public FT_Pos height;
            public FT_Pos horiBearingX;
            public FT_Pos horiBearingY;
            public FT_Pos horiAdvance;
            public FT_Pos vertBearingX;
            public FT_Pos vertBearingY;
            public FT_Pos vertAdvance;
        }

        // 位图结构
        [StructLayout(LayoutKind.Sequential)]
        public struct FT_Bitmap
        {
            public int rows;
            public int width;
            public int pitch;
            public IntPtr buffer;
            public short num_grays;
            public byte pixel_mode;
            public byte palette_mode;
            public IntPtr palette;
        }

        // 字形槽结构
        [StructLayout(LayoutKind.Sequential)]
        public struct FT_GlyphSlotRec
        {
            public IntPtr library;
            public IntPtr face;
            public IntPtr next;
            public FT_UInt glyph_index;
            public FT_Glyph_Metrics metrics;
            public FT_Fixed linearHoriAdvance;
            public FT_Fixed linearVertAdvance;
            public FT_Vector advance;
            public FT_Bitmap bitmap;
            public int bitmap_left;
            public int bitmap_top;
            // 其他字段根据需要添加
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FT_Vector
        {
            public FT_Pos x;
            public FT_Pos y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LineInfo
        {
            public uint width;
            public uint height;
            public uint baseline;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct BitmapResult
        {
            public IntPtr buffer;
            public uint bufferWidth;
            public uint bufferHeight;
            public IntPtr lines;
            public uint numLines;
        };
        // 面结构（简化版）
        [StructLayout(LayoutKind.Sequential)]
        public struct FT_FaceRec
        {
            public FT_Long num_faces;
            public FT_Long face_index;
            public FT_Long face_flags;
            public FT_Long style_flags;
            public FT_Long num_glyphs;
            public IntPtr family_name;
            public IntPtr style_name;
            public IntPtr glyph;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct GlyphMetrics
        {
            public int Pitch;        // 字形位图行距
            public uint Width;        // 字形位图宽度
            public uint Height;       // 字形位图高度
            public uint BearingX;     // 水平起始位置偏移
            public uint BearingY;     // 垂直起始位置偏移
            public uint Advance;      // 水平步进值
            public IntPtr Buffer;       // 字形位图数据
        };
        public static class FT
        {
            private const string FreeTypeLibFNA = "FreeType-FNA";
            [DllImport(FreeTypeLibFNA, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CreateFTFont(string path, int size);

            [DllImport(FreeTypeLibFNA, CallingConvention = CallingConvention.Cdecl)]
            public static extern void MeasureString(
                IntPtr context,
                [MarshalAs(UnmanagedType.LPWStr)] string text,
                out int width,
                out int height,
                out int baselineY
            );

            [DllImport(FreeTypeLibFNA, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
            public static extern IntPtr RenderGlyph(IntPtr context, char text);

            [DllImport(FreeTypeLibFNA, CallingConvention = CallingConvention.Cdecl)]
            public static extern void DisposeFont(IntPtr context);
            [DllImport(FreeTypeLibFNA, CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreeGlyph(IntPtr context);

            [DllImport(FreeTypeLibFNA, CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetFontSize(IntPtr context, uint size);
        }
    }
}
