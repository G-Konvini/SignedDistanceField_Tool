using UnityEditor;
using UnityEngine;

namespace G_Konvini.SDFTools.Editor.ShaderUtil
{
    internal struct ComputeShaderLoader
    {
        private string _guid;
        private ComputeShader _shader;

        public ComputeShader Shader
        {
            get
            {
                if (!_shader)
                    _shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(_guid));
                return _shader;
            }
        }

        public ComputeShaderLoader(string guid)
        {
            _guid = guid;
            _shader = null;
        }
        
    }
    
    public struct ShaderLoader
    {
        private string _guid;
        private Shader _shader;

        public Shader Shader
        {
            get
            {
                if (!_shader)
                    _shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(_guid));
                return _shader;
            }
        }

        public ShaderLoader(string guid)
        {
            _guid = guid;
            _shader = null;
        }
    }
    
}