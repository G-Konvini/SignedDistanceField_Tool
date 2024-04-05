// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System.Collections.Generic;
using UnityEngine;

namespace G_Konvini.SDFTools.Editor
{
    internal class EditorCacheData : ScriptableObject
    {
        internal List<RenderTexture> sdfList;
        internal List<Texture2D> sectionList;
        internal RenderTexture result;

        internal string savePath = "Assets/";
        internal string warnings;
        internal string messages;
        internal string inputWarning;


        internal List<float> framePositions;
        private float _distanceScale = 1;
        public float DistacneScale
        {
            get => _distanceScale;
            set => _distanceScale = Mathf.Clamp(value, 1, 10);
        }
        
        public RenderTexture preview;
        private bool _stepView;

        public bool StepView
        {
            get => _stepView;
            set
            {
                if (!value)
                    step = 0.5f;
                _stepView = value;
            }
        }
        
        public float step = 0.5f;

        public void ClearGeneratedData()
        {
            if (sdfList is { Count: > 0 })
            {
                foreach (var texture in sdfList)
                {
                    texture.Release();
                }
            }
            if (result)
            {
                result.Release();
            }
            if (preview)
            {
                preview.Release();
            }
            ClearWarningsAndMassages();
        }
        
        public void ClearAll()
        {
            ClearGeneratedData();
            savePath = "Assets/";
            if (sectionList is { Count: > 0 })
            {
                for (var i = 0; i < sectionList.Count; i++)
                {
                    var texture = sectionList[i];
                    DestroyImmediate(texture, true);
                }
            }
        }

        public void ClearWarningsAndMassages()
        {
            messages = null;
            warnings = null;
            inputWarning = null;
        }

        public void ResetFramePositions()
        {
            if (framePositions == null)
            {
                framePositions = new List<float>();
            }
            else
            {
                framePositions.Clear();
            }
            
            int count = sectionList.Count;
            float[] positions = new float[count];
            for (var i = 0; i < count; i++)
            {
                positions[i] = (float)i / (count-1);
            }
            
            framePositions.AddRange(positions);
        }
        
        public bool CheckSectionTextures()
        {
            if (sectionList is not { Count: > 0 })
            {
                return false;
            }
            
            int l = sectionList.Count;
            for (int i = 0; i < l; i++)
            {
                Texture2D tex = sectionList[i];
                int width = tex.width;
                int height = tex.height;

                if (width < 32 || height < 32)
                {
                    warnings = $"The size of Texture: {tex.name} is less than 32! please make sure that the texture size is between the 32 to 2048";
                    return false;
                }

                if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
                {
                    warnings = $"The size of Texture: {tex.name} is Non-power of 2";
                    return false;
                }
            }
            
            return true;
        }

        public bool CheckDataCombineValidate()
        {
            if (sectionList is { Count: < 1 })
            {
                return false;
            }

            if (sdfList == null)
            {
                return false;
            }
            
            if (sdfList is  { Count: < 2 })
            {
                messages = $"The count of SDF is less than 2";
                return false;
            }

            if (sdfList.Count > 32)
            {
                warnings = $"The count of Sections is greater than 32";
                return false;
            }


            if (sectionList.Count != sdfList.Count)
            {
                messages = $"The count of SDF is not match of Sections, Please regenerate SDF";
                return false;
            }
            
            int l = sectionList.Count;
            Texture2D tex = sectionList[0];
            int width = tex.width;
            int height = tex.height;
            
            for (var i = 1; i < l; i++)
            {
                var raw = sectionList[i];
                
                if (raw.width != width || raw.height != height)
                {
                    warnings = "Sections are not in the same size!";
                    return false;
                }
            }

            if (sdfList.Count != framePositions.Count)
            {
                ResetFramePositions();
            }
            
            return true;
        }
        
        public string GetRawTexturePath()
        {
            return savePath;
        }
    }
    
}