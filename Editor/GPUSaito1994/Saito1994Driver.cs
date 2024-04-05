// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System;
using System.Collections.Generic;
using System.Diagnostics;
using G_Konvini.SDFTools.Editor.ShaderUtil;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace G_Konvini.SDFTools.Editor.GPUSaito1994
{
    internal class Saito1994Driver : IDisposable
    {
        private readonly ComputeShader _sdfShader;
        private readonly List<Texture2D> _rawTex;
        private readonly List<RenderTexture> _sdfTex;
        private List<RenderTexture> _rowDistances;
        private float _scale;
        private List<RenderTexture> RowDistances => _rowDistances ??= new List<RenderTexture>();
        
        private static readonly int TexSize = Shader.PropertyToID("_TexSize");
        private static readonly int DataProcess0 = Shader.PropertyToID("_DataProcess0");
        private static readonly int Raw = Shader.PropertyToID("_Raw");
        private static readonly int Result = Shader.PropertyToID("_Result");
        private static readonly int Scale = Shader.PropertyToID("_Scale");


        public Saito1994Driver(List<Texture2D> rawTextures, List<RenderTexture> sdf, float scale)
        {
            _sdfShader = ShaderManager.Saito.Shader;
            _rawTex = rawTextures;
            _sdfTex = sdf;
            _scale = scale;
        }
        
        public bool Execute()
        {
            return CalculateSDF(_rawTex);
        }
        
        bool CalculateSDF(List<Texture2D> rawTexs)
        {
            // Stopwatch stopwatch = new Stopwatch();
            // stopwatch.Restart();
            // var stepBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            // var step = new uint[]{0};
            // stepBuffer.SetData(step);
            var shader = _sdfShader;

            for (var i = 0; i < rawTexs.Count; i++)
            {
                var rawTex = rawTexs[i];
                RenderSingleSDFTexture(rawTex, shader, i);
            }

            // stepBuffer.GetData(step);
            // Debug.Log($"Generate SDF Time : {stopwatch.ElapsedMilliseconds}ms");
            // stepBuffer.Release();
            return true;
        }

        private void RenderSingleSDFTexture(Texture2D rawTex, ComputeShader shader, int sdfId)
        {
            RenderTexture rowDist = CreatRenderTexture(rawTex, GraphicsFormat.R32G32_SFloat);
            RowDistances.Add(rowDist);

            RenderTexture sdf = CreatRenderTexture(rawTex, GraphicsFormat.R16_UNorm);
            _sdfTex.Add(sdf);

            PreProcess(shader, rawTex, rowDist);
            CalculateDistancePerRows(shader, rawTex, rowDist);
            CalculateDistance(shader, rawTex, rowDist, sdf, sdfId);
        }

        RenderTexture CreatRenderTexture(Texture2D rawTex, GraphicsFormat format)
        {
            var descriptor = new RenderTextureDescriptor(rawTex.width, rawTex.height,format,0); 
            RenderTexture rt = new RenderTexture(descriptor);              
            rt.enableRandomWrite = true;                       
            rt.Create();
            rt.name = rawTex.name;
            return rt;
        }

        void PreProcess(ComputeShader shader, Texture2D rawTex, RenderTexture rowDist)
        {
            int process = shader.FindKernel("PreProcess");
            shader.SetTexture(process, DataProcess0, rowDist);
            shader.Dispatch(process, rawTex.width / 32, rawTex.height / 32, 1);
        }
        
        void CalculateDistancePerRows(ComputeShader shader, Texture2D rawTex ,RenderTexture rowDist)
        {
            int process = shader.FindKernel("Process0");
            shader.SetVector(TexSize,new Vector2(rawTex.width,rawTex.height));
            shader.SetTexture(process, DataProcess0, rowDist);
            shader.SetTexture(process, Raw, rawTex);
            shader.Dispatch(process, rawTex.height / 32, 1, 1);
        }
        void CalculateDistance(ComputeShader shader, Texture2D rawTex, RenderTexture rowDist, RenderTexture sdf, int sdfId)
        {
            int process = shader.FindKernel("Process1");
            shader.SetTexture(process, DataProcess0, rowDist);
            shader.SetTexture(process, Raw, rawTex);
            shader.SetTexture(process, Result, sdf);
            shader.SetFloat(Scale,_scale);
            shader.Dispatch(process, rawTex.width/32 , rawTex.height/32 , 1);
        }

        
        public void Clear()
        {
            RenderTexture.active = null;
            foreach (var texture in _rowDistances)
                texture.Release();
            _rowDistances = null;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}