﻿using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class MaterialDescription
    {
        public MetallicRoughnessShaderArguments MetallicRoughness { get; set; }
        public SpecularGlossinessShaderArguments SpecularGlossiness { get; set; }
        public LegacyShaderArguments Legacy { get; set; }
        public MaterialDescription(Material material)
        {
            if (material.shader.name == "Standard (Specular setup)")
            {
                SpecularGlossiness = SetupSpecularGlossinessPBR(material);
            }
            else if (material.shader.name == "Standard")
            {
                MetallicRoughness = SetupMetallicRoughnessPBR(material);
            }
            else
            {
                Legacy = SetupLegacy(material);
            }
        }

        private LegacyShaderArguments SetupLegacy(Material material)
        {
            var arguments = new LegacyShaderArguments();
            SetupFlags(material, arguments);
            var shader = material.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        {
                            var color = material.GetColor(propertyName);
                            switch (propertyName)
                            {
                                case "_MainColor":
                                case "_Color":
                                    arguments.DiffColor = color;
                                    break;
                                case "_EmissionColor":
                                    arguments.EmissiveColor = color;
                                    break;
                                case "_SpecColor":
                                    arguments.SpecColor = color;
                                    break;

                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Float:
                        {
                            var value = material.GetFloat(propertyName);
                            switch (propertyName)
                            {
                                case "BumpScale": arguments.BumpScale = value; break;
                                case "_DetailNormalMapScale": break;
                                case "_DstBlend": break;
                                case "_GlossyReflections": break;
                                case "_Mode": break;
                                case "_SmoothnessTextureChannel": break;
                                case "_SpecularHighlights": break;
                                case "_SrcBlend": break;
                                case "_UVSec": break;
                                case "_ZWrite": break;
                                case "_Alpha_1":
                                    arguments.DiffColor = new Color(arguments.DiffColor.r, arguments.DiffColor.g, arguments.DiffColor.b, value);
                                    break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Range:
                        {
                            var value = material.GetFloat(propertyName);
                            switch (propertyName)
                            {
                                case "_Cutoff": arguments.Cutoff = value; break;
                                case "_GlossMapScale": break;
                                case "_Glossiness": break;
                                case "_OcclusionStrength": break;
                                case "_Parallax": break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            var texture = material.GetTexture(propertyName);
                            switch (propertyName)
                            {
                                case "_Normal":
                                case "_NormalMapRefraction":
                                case "_BumpMap": arguments.Bump = texture; break;
                                case "_DetailMask": arguments.Detail = texture; break;
                                case "_DetailNormalMap": arguments.DetailNormal = texture; break;
                                case "_Emission":
                                case "_EmissionMap": arguments.Emission = texture; break;
                                case "_Diffuse":
                                case "_Texture":
                                case "_MainTexture":
                                case "_MainTex": arguments.Diffuse = texture; break;
                                case "_OcclusionMap": arguments.Occlusion = texture; break;
                                case "_ParallaxMap": arguments.Parallax = texture; break;
                                case "_SpecGlossMap":
                                case "_SpecularRGBGlossA": arguments.Specular = texture; break;
                            }
                            break;
                        }
                }
            }
            return arguments;
        }

        private static void LogShaderParameters(Shader shader)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Shader parameters for \"" + shader.name + "\"");
            sb.AppendLine();
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                sb.AppendFormat("{0} {1}", propertyType, propertyName);
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        private SpecularGlossinessShaderArguments SetupSpecularGlossinessPBR(Material material)
        {
            var arguments = new SpecularGlossinessShaderArguments();
            SetupFlags(material, arguments);
            var shader = material.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        {
                            var color = material.GetColor(propertyName);
                            switch (propertyName)
                            {
                                case "_Color": arguments.DiffuseColor = color; break;
                                case "_EmissionColor": if (arguments.HasEmission) arguments.EmissiveColor = color; break;
                                case "_SpecColor": arguments.SpecularColor = color; break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Float:
                        {
                            var value = material.GetFloat(propertyName);
                            switch (propertyName)
                            {
                                case "_BumpScale": arguments.BumpScale = value; break;
                                case "_DetailNormalMapScale": break;
                                case "_DstBlend": break;
                                case "_GlossyReflections": break;
                                case "_Mode": break;
                                case "_SmoothnessTextureChannel": arguments.SmoothnessTextureChannel = (SmoothnessTextureChannel)value; break;
                                case "_SpecularHighlights": break;
                                case "_SrcBlend": break;
                                case "_UVSec": break;
                                case "_ZWrite": break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Range:
                        {
                            var value = material.GetFloat(propertyName);
                            switch (propertyName)
                            {
                                case "_Cutoff": arguments.Cutoff = value; break;
                                case "_GlossMapScale": break;
                                case "_Glossiness": break;
                                case "_OcclusionStrength": break;
                                case "_Parallax": break;
                            }
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            var texture = material.GetTexture(propertyName);
                            switch (propertyName)
                            {
                                case "_BumpMap": arguments.Bump = texture; break;
                                case "_DetailAlbedoMap": arguments.DetailAlbedo = texture; break;
                                case "_DetailMask": arguments.Detail = texture; break;
                                case "_DetailNormalMap": arguments.DetailNormal = texture; break;
                                case "_EmissionMap": arguments.Emission = texture; break;
                                case "_MainTex": arguments.Albedo = texture; break;
                                case "_OcclusionMap": arguments.Occlusion = texture; break;
                                case "_ParallaxMap": arguments.Parallax = texture; break;
                                case "_SpecGlossMap": arguments.PBRSpecular = texture; break;
                            }
                            break;
                        }
                }
            }
            return arguments;
        }

        private void SetupFlags(Material material, ShaderArguments arguments)
        {
            arguments.Transparent = material.renderQueue == (int)RenderQueue.Transparent;
            arguments.AlphaTest = material.renderQueue == (int)RenderQueue.AlphaTest;
            arguments.HasEmission = material.IsKeywordEnabled("_EMISSION");
        }

        private MetallicRoughnessShaderArguments SetupMetallicRoughnessPBR(Material material)
        {
            var arguments = new MetallicRoughnessShaderArguments();
            SetupFlags(material, arguments);
            var shader = material.shader;
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                    {
                        var color = material.GetColor(propertyName);
                        switch (propertyName)
                        {
                            case "_Color": arguments.AlbedoColor = color; break;
                            case "_EmissionColor": if (arguments.HasEmission) arguments.EmissiveColor = color; break;
                        }
                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Float:
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "_BumpScale": arguments.BumpScale = value; break;
                            case "_DetailNormalMapScale": break;
                            case "_DstBlend": break;
                            case "_GlossyReflections": break;
                            case "_Mode": break;
                            case "_SmoothnessTextureChannel": arguments.SmoothnessTextureChannel = (SmoothnessTextureChannel) value; break;
                            case "_SpecularHighlights": break;
                            case "_SrcBlend": break;
                            case "_UVSec": break;
                            case "_ZWrite": break;
                        }
                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.Range:
                    {
                        var value = material.GetFloat(propertyName);
                        switch (propertyName)
                        {
                            case "_Cutoff": arguments.Cutoff = value; break;
                            case "_GlossMapScale": break;
                            case "_Glossiness": arguments.Glossiness = value; break;
                            case "_Metallic": arguments.Metallic = value; break;
                            case "_OcclusionStrength": break;
                            case "_Parallax": break;
                        }
                        break;
                    }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                    {
                        var texture = material.GetTexture(propertyName);
                        if (texture != null)
                        {
                            switch (propertyName)
                            {
                                case "_BumpMap": arguments.Bump = texture; break;
                                case "_DetailAlbedoMap": arguments.DetailAlbedo = texture; break;
                                case "_DetailMask": arguments.Detail = texture; break;
                                case "_DetailNormalMap": arguments.DetailNormal = texture; break;
                                case "_EmissionMap": arguments.Emission = texture; break;
                                case "_MainTex": arguments.Albedo = texture; break;
                                case "_MetallicGlossMap": arguments.MetallicGloss = texture; break;
                                case "_OcclusionMap": arguments.Occlusion = texture; break;
                                case "_ParallaxMap": arguments.Parallax = texture; break;
                            }
                        }

                        break;
                }
                }
            }
            return arguments;
        }
    }
}
