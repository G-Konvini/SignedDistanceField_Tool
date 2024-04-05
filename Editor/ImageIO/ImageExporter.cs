// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System.IO;
using UnityEditor;
using UnityEngine;

namespace G_Konvini.SDFTools.Editor.ImageIO
{
    internal static class ImageExporter
    {
        public static void ExportImage(EditorCacheData data)
        {
            if (!data.result)
                return;
            
            int width = data.result.width;
            int height = data.result.height;
            var path = EditorUtility.SaveFilePanel("Save 16bit *.PNG", data.GetRawTexturePath(), "SDF_Map", "png");
            if (path is null or "")
                return;
            
            var image = EncodeImage(width, height, data.result);
            File.WriteAllBytes(path, image);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static void SavePerSingleImages(EditorCacheData data)
        {
            if (data.sdfList is not {Count: > 0 })
                return;
            
            var path = EditorUtility.SaveFolderPanel("Save 16bit *.PNG", data.GetRawTexturePath(), null);
            if (path is null or "")
                return;

            for (var i = 0; i < data.sdfList.Count; i++)
            {
                var sdf = data.sdfList[i];
                if (!sdf)
                    continue;

                int width = sdf.width;
                int height = sdf.height;

                var image = EncodeImage(width, height, sdf);
                var sdfPath = $"{path}/SDF_{sdf.name}_{i}.png";
                File.WriteAllBytes(sdfPath, image);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static byte[] EncodeImage(int width, int height, RenderTexture sdf)
        {
            var output = RenderTextureToTexture2D(width, height, sdf);
            var data = output.EncodeToPNG();
            return data;
        }

        private static Texture2D RenderTextureToTexture2D(int width, int height, RenderTexture sdf)
        {
            Texture2D output = new Texture2D(width, height, TextureFormat.R16, false);
            RenderTexture.active = sdf;
            output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            output.Apply();
            RenderTexture.active = null;
            return output;
        }
    }
}