using G_Konvini.SDFTools.Editor.ShaderUtil;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace G_Konvini.SDFTools.Editor.PreviewShader
{
    internal class PreviewDriver
    {
        private RenderTexture _source;
        private bool _stepView;
        private float _step;
        private ComputeShader _shader;
        
        private static readonly int SDFTex = Shader.PropertyToID("_Source");
        private static readonly int StepView = Shader.PropertyToID("_StepView");
        private static readonly int Step = Shader.PropertyToID("_Step");
        private static readonly int Result = Shader.PropertyToID("_Result");
        
        
        public void Setup(RenderTexture source, bool isStepView, float step)
        {
            _source = source;
            _stepView = isStepView;
            _step = step;
            _shader = ShaderManager.Preview.Shader;
        }

        public void Execute(ref RenderTexture target)
        {
            ComputeShader shader = _shader;
            RenderTexture source = _source;

            int width = source.width;
            int height = source.height;
            
            if (target != null)
                target.Release();
            
            var descriptor = new RenderTextureDescriptor(width, height,GraphicsFormat.R16G16B16A16_UNorm, 0); 
            target = new RenderTexture(descriptor);
            target.enableRandomWrite = true;                       
            target.Create();

            int kernel = shader.FindKernel("Preview");
            shader.SetTexture(kernel, Result, target);
            shader.SetTexture(kernel, SDFTex, source);
            shader.SetFloat(Step, _step);
            shader.SetBool(StepView,_stepView);
            
            shader.Dispatch(kernel, width/8,height/8,1);
            
        }
    }
}