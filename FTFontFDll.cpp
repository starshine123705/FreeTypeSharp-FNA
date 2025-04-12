#define _CRT_SECURE_NO_WARNINGS
#include "pch.h"
#include <ft2build.h>
#include FT_FREETYPE_H
#include <vector>
#include <map>
#include <stdexcept>
#include <cmath>
#include <combaseapi.h>
#include <algorithm>

extern "C" {
#define _CRT_SECURE_NO_WARNINGS
    struct FontContext {
        FT_Face face;
        unsigned int ascender;
        unsigned int descender;
    };


    // 新增字符度量信息结构体
    struct GlyphMetrics {
        int pitch;
        unsigned int width; 
        unsigned int height;
        unsigned int bearingX; 
        unsigned int bearingY;  
        unsigned int advance; 
        unsigned char* buffer; 
    }; 
    struct Rect {
        unsigned int x, y;
        unsigned int width, height;
        bool operator==(const Rect& other) const {
            return x == other.x && y == other.y &&
                width == other.width && height == other.height;
        }
    };
    __declspec(dllexport) void SaveGlyphBitmap(const FT_Bitmap* bitmap, const char* filename) {
#pragma pack(push, 1)
        struct BMPHeader {
            uint16_t file_type = 0x4D42;
            uint32_t file_size = 0;
            uint16_t reserved1 = 0;
            uint16_t reserved2 = 0;
            uint32_t offset = 54;
            uint32_t header_size = 40;
            int32_t width = 0;
            int32_t height = 0;
            uint16_t planes = 1;
            uint16_t bit_count = 32;
            uint32_t compression = 0;
            uint32_t image_size = 0;
            int32_t x_pixels_per_meter = 2835;
            int32_t y_pixels_per_meter = 2835;
            uint32_t colors_used = 0;
            uint32_t colors_important = 0;
        };
#pragma pack(pop)

        BMPHeader header;
        header.width = bitmap->width;
        header.height = bitmap->rows;
        header.file_size = sizeof(BMPHeader) + bitmap->width * bitmap->rows * 4;
        header.image_size = bitmap->width * bitmap->rows * 4;

        FILE* file = nullptr;
        fopen_s(&file, filename, "wb");
        if (!file) return;

        fwrite(&header, 1, sizeof(BMPHeader), file);

        std::vector<unsigned char> buffer(bitmap->width * bitmap->rows * 4, 0);
        for (int y = 0; y < bitmap->rows; ++y) {
            for (int x = 0; x < bitmap->width; ++x) {
                unsigned char alpha = bitmap->buffer[y * bitmap->pitch + x];
                int idx = (y * bitmap->width + x) * 4;
                buffer[idx] = alpha;     // R
                buffer[idx + 1] = alpha; // G
                buffer[idx + 2] = alpha; // B
                buffer[idx + 3] = alpha; // A
            }
        }

        // BMP要求从下到上写入
        for (int y = bitmap->rows - 1; y >= 0; --y) {
            const unsigned char* line = buffer.data() + y * bitmap->width * 4;
            fwrite(line, 1, bitmap->width * 4, file);
        }

        fclose(file);
    }
    __declspec(dllexport) GlyphMetrics* RenderGlyph(FontContext* content, const wchar_t singleChar)
    {
        FT_UInt index = FT_Get_Char_Index(content->face, singleChar);
        if (index == 0)
        {
            throw;
            return new GlyphMetrics();
        }
        if (FT_Load_Glyph(content->face, index, FT_LOAD_RENDER) != 0)
        {
            throw;
            return new GlyphMetrics();
        }
        FT_GlyphSlot slot = content->face->glyph;
        auto bitmap = &slot->bitmap;
        unsigned char* buffer = (unsigned char*)malloc(bitmap->width * bitmap->rows * 4);
        if (buffer == nullptr) {
            return new GlyphMetrics;
        }

        for (int y = 0; y < bitmap->rows; ++y) {
            for (int x = 0; x < bitmap->width; ++x) {
                unsigned char alpha = bitmap->buffer[y * bitmap->pitch + x];
                int idx = (y * bitmap->width + x) * 4;
                buffer[idx] = alpha;     // R
                buffer[idx + 1] = alpha; // G
                buffer[idx + 2] = alpha; // B
                buffer[idx + 3] = alpha; // A
            }
        }
        GlyphMetrics* gm = new GlyphMetrics();

        gm->advance = slot->advance.x >> 6;
        gm->bearingX = slot->metrics.horiBearingX >> 6;
        gm->bearingY = slot->metrics.horiBearingY >> 6;
        gm->buffer = buffer;
        gm->height = slot->bitmap.rows;
        gm->width = slot->bitmap.width;
        gm->pitch = slot->bitmap.pitch;
        return gm;
    } 
    __declspec(dllexport) FontContext* CreateFTFont(const char* path, int size) {
        static FT_Library library;
        static bool initialized = false;

        if (!initialized) {
            if (FT_Init_FreeType(&library))
                throw std::runtime_error("FreeType初始化失败");
            initialized = true;
        }

        FontContext* ctx = new FontContext();
		FT_Face face;
        if (FT_New_Face(library, path, 0, &face))
            throw std::runtime_error("字体加载失败");

        FT_Set_Pixel_Sizes(face, 0, size);
		ctx->face = face;
        ctx->ascender = face->size->metrics.ascender >> 6;
        ctx->descender = std::abs(face->size->metrics.descender >> 6);

        return ctx;
    }
    
    __declspec(dllexport) void SetFontSize(FontContext* font, FT_UInt size) {
		FT_Set_Pixel_Sizes(font->face, size, size);
    }

    __declspec(dllexport) void MeasureString(
        FontContext* ctx,
        const wchar_t* text,
        int* outWidth,
        int* outHeight,
        int* outBaseLineHeight
    ) {
        int totalWidth = 0;
        int MaxAsender = 0;
        int MaxDesender = 0;
        *outBaseLineHeight = 0;
        uint32_t prevGlyph = 0;

        for (const wchar_t* p = text; *p; ++p) {
            FT_UInt glyphIndex = FT_Get_Char_Index(ctx->face, *p);

            // 处理字距
            if (prevGlyph && glyphIndex) {
                FT_Vector delta;
                FT_Get_Kerning(ctx->face, prevGlyph, glyphIndex,
                    FT_KERNING_DEFAULT, &delta);
                totalWidth += delta.x >> 6;
            }

            if (FT_Load_Glyph(ctx->face, glyphIndex, FT_LOAD_DEFAULT))
                continue;

            totalWidth += ctx->face->glyph->advance.x >> 6;
            *outBaseLineHeight =  max(*outBaseLineHeight, ctx->face->glyph->metrics.horiBearingY >> 6);
            MaxAsender = max(MaxAsender, ctx->face->glyph->metrics.horiBearingY);
            MaxDesender = max(MaxDesender, (ctx->face->glyph->metrics.height - ctx->face->glyph->metrics.horiBearingY));
            prevGlyph = glyphIndex;
        }
        *outWidth = totalWidth;
        *outHeight = (MaxAsender + MaxDesender) >> 6;
    }
   
	__declspec(dllexport) void FreeGlyph(GlyphMetrics* metrics) {
		CoTaskMemFree(metrics->buffer);
	}


    __declspec(dllexport) void DisposeFont(FontContext* ctx) {
        FT_Done_Face(ctx->face);
        delete ctx;
    }
}