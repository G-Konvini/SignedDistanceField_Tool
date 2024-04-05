// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

namespace G_Konvini.SDFTools.Editor.ShaderUtil
{
    internal static class ShaderManager
    {
        internal static readonly ComputeShaderLoader Saito = new ComputeShaderLoader("eff0496bebb188045aee14ca997d97e5");
        internal static readonly ComputeShaderLoader CombineSDF = new ComputeShaderLoader("1049cfc25d06f104ba940cab4b9a2a48");
        internal static readonly ComputeShaderLoader Preview = new ComputeShaderLoader("6efc0bae61b1adf4faf5ca953cb4bf55");
    }
    
}
