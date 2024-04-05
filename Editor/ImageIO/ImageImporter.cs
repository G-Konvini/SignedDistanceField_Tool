// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace G_Konvini.SDFTools.Editor.ImageIO
{
    internal static class ImageImporter
    {
        public static void ImportImage(EditorCacheData data)
        {
            data.ClearWarningsAndMassages();
            var objects =UnityEditor.Selection.objects;
            if (data.sectionList == null)
                data.sectionList = new List<Texture2D>();

            TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
            settings.format = TextureImporterFormat.RGBA32;
            for (var i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                if (o is Texture2D image)
                {
                    string path = AssetDatabase.GetAssetPath(image);
                    
                    var bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                    tex.name = image.name;
                    if (!tex.LoadImage(bytes))
                    {
                        data.inputWarning = $"{image.name} is not a *.jpg *.png image";
                        UnityEngine.Object.DestroyImmediate(tex, true);
                        continue;
                    }
                    
                    data.sectionList.Add(tex);
                    
                    if (i == 0)
                    {
                        data.savePath = Path.GetDirectoryName(path) + "/";
                    }
                }

            }
        }
    }
}