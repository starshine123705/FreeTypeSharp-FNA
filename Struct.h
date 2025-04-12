#pragma once

struct fontStruct
{
public:
	fontStruct();
	unsigned int width;
	unsigned int height;
	unsigned int xMin;
	unsigned int xMax;
	unsigned int yMin;
	unsigned int yMax;
	unsigned int bearingY;
	unsigned int bearingX;
	unsigned int origin;
	unsigned int advance;
	unsigned char* bitmapBuffer;
};