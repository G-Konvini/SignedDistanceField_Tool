// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System.Collections.Generic;
using G_Konvini.SDFTools.Editor.ShaderUtil;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace G_Konvini.SDFTools.Editor.CombineSDF
{
    internal class CombineSDFDriver
    {
        private List<Texture2D> _rawList;
        private List<RenderTexture> _sdfList;
        private List<float> _framePositions;
        private ComputeShader _shader;
        
        private static readonly int SDFCount = Shader.PropertyToID("_SDF_Count");
        private static readonly int Params = Shader.PropertyToID("_SDF_Params");
        private static readonly int Result = Shader.PropertyToID("_Result");


        public void Setup(List<Texture2D> rawList, List<RenderTexture> sdfList, List<float> framePositions)
        {
            _rawList = rawList;
            _sdfList = sdfList;
            _framePositions = framePositions;
            _shader = ShaderManager.CombineSDF.Shader;
        }

        public void Execute(ref RenderTexture result)
        {
            ComputeShader shader = _shader;
            var rawList = _rawList;
            var sdfList = _sdfList;

            int width = rawList[0].width;
            int height = rawList[0].height;
            
            // Debug.Log(width);

            if (result != null)
                result.Release();
            
            var descriptor = new RenderTextureDescriptor(width, height,GraphicsFormat.R16_UNorm,0); 
            result = new RenderTexture(descriptor);
            result.enableRandomWrite = true;                       
            result.Create();
            
            
            int count = sdfList.Count;
            ComputeBuffer sdfParamsBuffer = new ComputeBuffer(count - 1, sizeof(float) * 2, ComputeBufferType.Structured);
            SDFParams[] sdfParams = new SDFParams[count - 1]; 
            int kernel = shader.FindKernel("CombineSDF");
            float max = count - 1;
            for (int i = 0; i < 32; i++)
            {
                if (i < count)
                {
                    var raw = rawList[i];
                    var sdf = sdfList[i];
                    shader.SetTexture(kernel, $"_Raw_MAP_{i}", raw);
                    shader.SetTexture(kernel, $"_SDF_MAP_{i}", sdf);
                    var param = new SDFParams();
                    if (i < max)
                    {
                        // param.valueFrom = (float)i / max;
                        // param.valueTo = ((float)i + 1) / max;
                        param.valueFrom = _framePositions[i];
                        param.valueTo = _framePositions[i + 1];
                        sdfParams[i] = param;
                    }
                }
                else
                {
                    var raw = rawList[0];
                    var sdf = sdfList[0];
                    shader.SetTexture(kernel, $"_Raw_MAP_{i}", raw);
                    shader.SetTexture(kernel, $"_SDF_MAP_{i}", sdf);
                }

            }
            shader.SetInt(SDFCount, count);
            sdfParamsBuffer.SetData(sdfParams);
            shader.SetBuffer(kernel, Params, sdfParamsBuffer);
            shader.SetTexture(kernel, Result, result);
            shader.Dispatch(kernel, width / 8, height / 8, 1);
            
            sdfParamsBuffer.Release();
        }
    }
    
    public struct SDFParams
    {
        public float valueFrom;
        public float valueTo;
    };
}