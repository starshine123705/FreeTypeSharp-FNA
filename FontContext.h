// FTFontFDll.h

#pragma once

#include <ft2build.h>
#include FT_FREETYPE_H

extern "C" {
    struct FontContext {
        FT_Face face;
        int ascender;
        int descender;
    };

    struct BitmapResult {
        unsigned char* buffer;
        int width;
        int height;
    };

    __declspec(dllexport) FontContext* CreateFont(const char* path, int size);

    __declspec(dllexport) void MeasureString(
        FontContext* ctx,
        const wchar_t* text,
        int* outWidth,
        int* outHeight
    );

    __declspec(dllexport) BitmapResult RenderText(
        FontContext* ctx,
        const wchar_t* text,
        unsigned char colorR,
        unsigned char colorG,
        unsigned char colorB
    );

    __declspec(dllexport) void FreeBitmap(BitmapResult* result);

    __declspec(dllexport) void DisposeFont(FontContext* ctx);
}
