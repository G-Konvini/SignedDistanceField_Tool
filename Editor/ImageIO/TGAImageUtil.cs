// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace G_Konvini.SDFTools.Editor.ImageIO
{ 
    public static class TGAImageUtil
    {
        enum ImageType
        {
            NoImageDataIncluded = 0,
            UncompressedColorMappedImage = 1,
            UncompressedColorImage = 2,
            UncompressedGrayImage = 3,
            RunLengthEncodedColorMappedImage = 9,
            RunLengthEncodeColorImage = 10,
            RunLengthEncodeGrayImage = 11
        }

        static TextureFormat GetTextureFormat(byte depth)
        {
            return depth is 32 ? TextureFormat.RGBA32 : TextureFormat.RGB24;
        }

        static int GetPixelSize(byte depth)
        {
            return depth / 8;
        }
        
        public static bool Load(out Texture2D texture, string fileName)
        {
            try
            {
                BinaryReader reader = new BinaryReader(File.OpenRead(fileName));
                reader.BaseStream.Seek(2, SeekOrigin.Begin);
                ImageType imageType = (ImageType)reader.ReadByte();
                reader.BaseStream.Seek(12, SeekOrigin.Begin);
                short width = reader.ReadInt16();
                short height = reader.ReadInt16();
                byte depth = reader.ReadByte();
                reader.BaseStream.Seek(18, SeekOrigin.Begin);
                int pixelSize = GetPixelSize(depth);
                int pixelCount = width * height;
                
                byte[] source = reader.ReadBytes(pixelCount * pixelSize);
                reader.Close();
                var pixelBytes = ReorderBytes(source, pixelCount, pixelSize);
                
                texture = new Texture2D(width, height, GetTextureFormat(depth),false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.LoadRawTextureData(pixelBytes);
                texture.Apply(false);
                return true;
            }
            catch (Exception)
            {
                texture = null;
                Object.DestroyImmediate(texture);
                return false;
            }
        }

        private static byte[] ReorderBytes(byte[] source, int pixelCount, int pixelSize)
        {
            int size = pixelSize is 4 ? 4 : 3;
            byte[] bytes = new Byte[pixelCount * size];
            
            for (int i = 0, j = 0; j < bytes.Length; i += pixelSize, j += size)
            {
                switch (pixelSize)
                {
                    case 1: // R8
                    {
                        byte r = source[i];
                        
                        bytes[j] = r;
                        bytes[j + 1] = r;
                        bytes[j + 2] = r;
                        break;
                    }
                    case 2: // A1_R5_G5_B5
                    {
                        byte highByte = source[i + 1];
                        byte lowByte = source[i];
                        ushort argb16 = (ushort)((highByte << 8) | lowByte);
                        
                        byte r = (byte)((argb16 & 0x7C00) >> 10);
                        byte g = (byte)((argb16 & 0x03E0) >> 5);
                        byte b = (byte)(argb16 & 0x001F);
                        
                        // 2^8 / 2^5 = 8
                        bytes[j] = (byte)(r * 8);
                        bytes[j + 1] = (byte)(g * 8);
                        bytes[j + 2] = (byte)(b * 8);
                        break;
                    }
                    case 3: // B8_G8_R8
                    {
                        byte b = source[i];
                        byte g = source[i + 1];
                        byte r = source[i + 2];
                        
                        bytes[j] = r;
                        bytes[j + 1] = g;
                        bytes[j + 2] = b;
                        break;
                    }
                    case 4: // B8_G8_R8_A8
                    {
                        byte b = source[i];
                        byte g = source[i + 1];
                        byte r = source[i + 2];
                        byte a = source[i + 3];
                        
                        bytes[j] = r;
                        bytes[j + 1] = g;
                        bytes[j + 2] = b;
                        bytes[j + 3] = a;
                        break;
                    }
                }
            }

            return bytes;
        }
    }
}