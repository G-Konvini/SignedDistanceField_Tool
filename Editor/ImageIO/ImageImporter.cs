using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace G_Konvini.SDFTools.Editor.ImageIO
{
    internal class ImageImporter
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
                    
                    if (!CheckImageFormat(data, path))
                        continue;
                    
                    var bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                    tex.LoadImage(bytes);
                    data.sectionList.Add(tex);
                    
                    if (i == 0)
                    {
                        data.savePath = Path.GetDirectoryName(path) + "/";
                    }
                }

            }
        }

        static bool CheckImageFormat(EditorCacheData data, string path)
        {
            string lowerPath = path.ToLower();
            if (lowerPath.Contains(".png") || lowerPath.Contains(".jpg") || lowerPath.Contains(".jpeg") )
                return true;

            data.inputWarning = $"{path} is not a *.jpg or *.png image";
            return false;

        }
    }
}