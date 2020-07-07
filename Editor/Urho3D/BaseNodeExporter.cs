﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityToCustomEngineExporter.Urho3D;
using Object = UnityEngine.Object;
//using UnityEngine.ProBuilder;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class BaseNodeExporter
    {
        protected Urho3DEngine _engine;
        protected int _id;
        protected EditorTaskScheduler BackgroundEditorTasks = new EditorTaskScheduler();
        private readonly bool _skipDisabled;

        public BaseNodeExporter(Urho3DEngine engine, bool skipDisabled)
        {
            _engine = engine;
            _skipDisabled = skipDisabled;
        }

        public static string Format(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.r, pos.g, pos.b, pos.a);
        }

        public static string FormatRGB(Color pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.r, pos.g, pos.b);
        }

        public static string Format(float pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", pos);
        }

        public static string Format(Vector4 pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.x, pos.y, pos.z, pos.w);
        }


        protected void WriteAttribute(XmlWriter writer, string prefix, string name, float pos)
        {
            WriteAttribute(writer, prefix, name, string.Format(CultureInfo.InvariantCulture, "{0}", pos));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector3 pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", pos.x, pos.y, pos.z));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Vector4 pos)
        {
            WriteAttribute(writer, prefix, name, Format(pos));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Quaternion pos)
        {
            WriteAttribute(writer, prefix, name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", pos.w, pos.x, pos.y, pos.z));
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, Color pos)
        {
            WriteAttribute(writer, prefix, name, Format(pos));
        }

        protected void EndElement(XmlWriter writer, string prefix)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        protected void StartComponent(XmlWriter writer, string prefix, string type)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteStartElement("component");
            writer.WriteAttributeString("type", type);
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");
        }

        protected void WriteAttribute(XmlWriter writer, string prefix, string name, string vaue)
        {
            writer.WriteWhitespace(prefix);
            writer.WriteStartElement("attribute");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", vaue);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        protected void WriteObject(XmlWriter writer, string prefix, GameObject obj, HashSet<Renderer> excludeList,
            bool parentEnabled)
        {
            var isEnabled = obj.activeSelf && parentEnabled;
            if (_skipDisabled && !isEnabled) return;

            var localExcludeList = new HashSet<Renderer>(excludeList);
            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");

            var subPrefix = prefix + "\t";
            var subSubPrefix = subPrefix + "\t";

            WriteAttribute(writer, subPrefix, "Is Enabled", isEnabled);
            WriteAttribute(writer, subPrefix, "Name", obj.name);
            WriteAttribute(writer, subPrefix, "Tags", obj.tag);
            WriteAttribute(writer, subPrefix, "Position", obj.transform.localPosition);
            WriteAttribute(writer, subPrefix, "Rotation", obj.transform.localRotation);
            WriteAttribute(writer, subPrefix, "Scale", obj.transform.localScale);

            foreach (var component in obj.GetComponents<Component>())
                if (component is IUrho3DComponent customComponent)
                {
                    ExportCustomComponent(writer, subPrefix, customComponent);
                }
                else if (component is Camera camera)
                {
                    if (camera != null)
                    {
                        StartComponent(writer, subPrefix, "Camera");

                        WriteAttribute(writer, subSubPrefix, "Near Clip", camera.nearClipPlane);
                        WriteAttribute(writer, subSubPrefix, "Far Clip", camera.farClipPlane);

                        EndElement(writer, subPrefix);
                    }
                }
                else if (component is Light light)
                {
                    if (light != null && light.type != LightType.Area)
                    {
                        StartComponent(writer, subPrefix, "Light");
                        if (light.type == LightType.Directional)
                        {
                            WriteAttribute(writer, subSubPrefix, "Light Type", "Directional");
                            WriteAttribute(writer, subSubPrefix, "CSM Splits", "2 16 128 1024");
                        }
                        else if (light.type == LightType.Spot)
                        {
                            WriteAttribute(writer, subSubPrefix, "Light Type", "Spot");
                        }
                        else if (light.type == LightType.Point)
                        {
                            WriteAttribute(writer, subSubPrefix, "Range", light.range);
                        }

                        WriteAttribute(writer, subSubPrefix, "Color", light.color);
                        WriteAttribute(writer, subSubPrefix, "Brightness Multiplier", light.intensity * 981.75f);
                        WriteAttribute(writer, subSubPrefix, "Use Physical Values", "true");
                        WriteAttribute(writer, subSubPrefix, "Depth Constant Bias", 0.0001f);
                        WriteAttribute(writer, subSubPrefix, "Cast Shadows", light.shadows != LightShadows.None);
                        if (light.cookie != null)
                        {
                            _engine.ScheduleTexture(light.cookie);
                            WriteAttribute(writer, subSubPrefix, "Light Shape Texture",
                                "Texture2D;" + _engine.EvaluateTextrueName(light.cookie));
                        }

                        EndElement(writer, subPrefix);
                    }
                }
                else if (component is AudioSource audioSource)
                {
                    ExportAudioSource(writer, audioSource, subPrefix);
                }
                else if (component is Terrain terrain)
                {
                    ExportTerrain(writer, terrain?.terrainData, obj.GetComponent<TerrainCollider>(), subPrefix);
                }
                else if (component is Rigidbody rigidbody)
                {
                    StartComponent(writer, subPrefix, "RigidBody");
                    var localToWorldMatrix = obj.transform.localToWorldMatrix;
                    var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
                    WriteAttribute(writer, subSubPrefix, "Physics Position", pos);
                    WriteAttribute(writer, subSubPrefix, "Mass", rigidbody.mass);
                    EndElement(writer, subPrefix);
                }
                else if (component is MeshCollider meshCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape");
                    WriteAttribute(writer, subSubPrefix, "Shape Type", "TriangleMesh");
                    if (meshCollider.sharedMesh != null)
                    {
                        var sharedMesh = meshCollider.sharedMesh;
                        _engine.ScheduleAssetExport(sharedMesh);
                        var meshPath = _engine.EvaluateMeshName(sharedMesh);
                        if (!string.IsNullOrWhiteSpace(meshPath))
                            WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);
                    }

                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, obj, subPrefix, subSubPrefix);
                }
                else if (component is BoxCollider boxCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape");
                    WriteAttribute(writer, subSubPrefix, "Size", boxCollider.size);
                    WriteAttribute(writer, subSubPrefix, "Offset Position", boxCollider.center);
                    //WriteAttribute(writer, subSubPrefix, "Offset Rotation", new Quaternion(0,0,0, 1));
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, obj, subPrefix, subSubPrefix);
                }
                else if (component is TerrainCollider terrainCollider)
                {
                    //Skip terrain collider as the actual terrain is in another node
                }
                else if (component is SphereCollider sphereCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape");
                    WriteAttribute(writer, subSubPrefix, "Shape Type", "Sphere");
                    WriteAttribute(writer, subSubPrefix, "Offset Position", sphereCollider.center);
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, obj, subPrefix, subSubPrefix);
                }
                else if (component is CapsuleCollider capsuleCollider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape");
                    if (component.name == "Cylinder")
                        WriteAttribute(writer, subSubPrefix, "Shape Type", "Cylinder");
                    else
                        WriteAttribute(writer, subSubPrefix, "Shape Type", "Capsule");
                    var d = capsuleCollider.radius * 2.0f;
                    WriteAttribute(writer, subSubPrefix, "Size", new Vector3(d, capsuleCollider.height, d));
                    WriteAttribute(writer, subSubPrefix, "Offset Position", capsuleCollider.center);
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, obj, subPrefix, subSubPrefix);
                }
                else if (component is Skybox skybox)
                {
                    var skyboxMaterial = skybox.material;
                    WriteSkyboxComponent(writer, subPrefix, skyboxMaterial);
                }
                else if (component is Collider collider)
                {
                    StartComponent(writer, subPrefix, "CollisionShape");
                    EndElement(writer, subPrefix);
                    WriteStaticRigidBody(writer, obj, subPrefix, subSubPrefix);
                }
                else if (component is ReflectionProbe reflectionProbe)
                {
                    switch (reflectionProbe.mode)
                    {
                        case ReflectionProbeMode.Baked:
                            ExportZone(writer, subPrefix, reflectionProbe, reflectionProbe.bakedTexture as Cubemap);
                            break;
                        case ReflectionProbeMode.Custom:
                            ExportZone(writer, subPrefix, reflectionProbe,
                                reflectionProbe.customBakedTexture as Cubemap);
                            break;
                    }
                }

            var meshFilter = obj.GetComponent<MeshFilter>();
            //var proBuilderMesh = obj.GetComponent<ProBuilderMesh>();
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
            var lodGroup = obj.GetComponent<LODGroup>();

            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                foreach (var lod in lods.Skip(1))
                foreach (var renderer in lod.renderers)
                    localExcludeList.Add(renderer);
            }

            if (meshRenderer != null && !localExcludeList.Contains(meshRenderer))
                if (meshFilter != null) // || proBuilderMesh != null)
                {
                    StartComponent(writer, subPrefix, "StaticModel");

                    string meshPath;
                    //if (proBuilderMesh != null)
                    //{
                    //    _engine.ScheduleAssetExport(proBuilderMesh);
                    //    meshPath = _engine.EvaluateMeshName(proBuilderMesh);
                    //}
                    //else
                    {
                        var sharedMesh = meshFilter.sharedMesh;
                        _engine.ScheduleAssetExport(sharedMesh);
                        meshPath = _engine.EvaluateMeshName(sharedMesh);
                    }
                    if (!string.IsNullOrWhiteSpace(meshPath))
                        WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                    var materials = "Material";
                    foreach (var material in meshRenderer.sharedMaterials)
                    {
                        _engine.ScheduleAssetExport(material);
                        var path = _engine.EvaluateMaterialName(material);
                        materials += ";" + path;
                    }

                    WriteAttribute(writer, subSubPrefix, "Material", materials);

                    WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                        meshRenderer.shadowCastingMode != ShadowCastingMode.Off);

                    EndElement(writer, subPrefix);
                }

            if (skinnedMeshRenderer != null && !localExcludeList.Contains(skinnedMeshRenderer))
            {
                StartComponent(writer, subPrefix, "AnimatedModel");


                var sharedMesh = skinnedMeshRenderer.sharedMesh;
                _engine.ScheduleAssetExport(sharedMesh);
                var meshPath = _engine.EvaluateMeshName(sharedMesh);
                if (!string.IsNullOrWhiteSpace(meshPath))
                    WriteAttribute(writer, subSubPrefix, "Model", "Model;" + meshPath);

                var materials = "Material";
                foreach (var material in skinnedMeshRenderer.sharedMaterials)
                {
                    _engine.ScheduleAssetExport(material);
                    var path = _engine.EvaluateMaterialName(material);
                    materials += ";" + path;
                }

                WriteAttribute(writer, subSubPrefix, "Material", materials);

                WriteAttribute(writer, subSubPrefix, "Cast Shadows",
                    skinnedMeshRenderer.shadowCastingMode != ShadowCastingMode.Off);

                EndElement(writer, subPrefix);
            }

            foreach (Transform childTransform in obj.transform)
                if (childTransform.parent.gameObject == obj)
                    WriteObject(writer, subPrefix, childTransform.gameObject, localExcludeList, isEnabled);

            if (!string.IsNullOrEmpty(prefix))
                writer.WriteWhitespace(prefix);
            writer.WriteEndElement();
            writer.WriteWhitespace("\n");
        }

        private void ExportAudioSource(XmlWriter writer, AudioSource audioSource, string subPrefix)
        {
            string subSubPrefix = subPrefix + "\t";
            StartComponent(writer, subPrefix, "SoundSource3D");
            if (audioSource.clip != null)
            {
                var name = _engine.EvaluateAudioClipName(audioSource.clip);
                _engine.ScheduleAssetExport(audioSource.clip);
                WriteAttribute(writer, subSubPrefix, "Sound", "Sound;" + name);
                WriteAttribute(writer, subSubPrefix, "Frequency", audioSource.clip.frequency);
                WriteAttribute(writer, subSubPrefix, "Is Playing", audioSource.playOnAwake);
                WriteAttribute(writer, subSubPrefix, "Play Position", 0);
            }
            EndElement(writer, subPrefix);
        }

        protected void WriteSkyboxComponent(XmlWriter writer, string subPrefix, Material skyboxMaterial)
        {
            string subSubPrefix = subPrefix + "\t";
            StartComponent(writer, subPrefix, "Skybox");
            {
                // Export cube
                var gameObject = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
                var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(gameObject);
                //var sharedMeshName = "UnityBuiltIn/Cube.mdl";
                _engine.ScheduleAssetExport(mesh);
                WriteAttribute(writer, subSubPrefix, "Model", "Model;" + _engine.EvaluateMeshName(mesh));
            }

            _engine.ScheduleAssetExport(skyboxMaterial);
            var materials = "Material;" + _engine.EvaluateMaterialName(skyboxMaterial);
            WriteAttribute(writer, subSubPrefix, "Material", materials);
            EndElement(writer, subPrefix);
        }

        private void WriteStaticRigidBody(XmlWriter writer, GameObject obj, string subPrefix, string subSubPrefix)
        {
            if (obj.GetComponent<Rigidbody>() == null)
            {
                StartComponent(writer, subPrefix, "RigidBody");
                var localToWorldMatrix = obj.transform.localToWorldMatrix;
                var pos = new Vector3(localToWorldMatrix.m03, localToWorldMatrix.m13, localToWorldMatrix.m23);
                WriteAttribute(writer, subSubPrefix, "Physics Position", pos);
                EndElement(writer, subPrefix);
            }
        }

        private (float min, float max, Vector2 size) GetTerrainSize(TerrainData terrain)
        {
            var w = terrain.heightmapResolution;
            var h = terrain.heightmapResolution;
            var max = float.MinValue;
            var min = float.MaxValue;
            var heights = terrain.GetHeights(0, 0, w, h);
            foreach (var height in heights)
            {
                if (height > max) max = height;
                if (height < min) min = height;
            }

            return (min, max, new Vector2(w, h));
        }

        private void ExportZone(XmlWriter writer, string subPrefix, ReflectionProbe reflectionProbe, Cubemap cubemap)
        {
            if (cubemap == null) return;

            var assetPath = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            _engine.ScheduleAssetExport(cubemap);
            var texName = _engine.EvaluateCubemapName(cubemap);

            StartComponent(writer, subPrefix, "Zone");

            var subSubPrefix = subPrefix + "\t";
            WriteAttribute(writer, subSubPrefix, "Ambient Color", RenderSettings.ambientLight.linear);
            WriteAttribute(writer, subSubPrefix, "Override Mode", false);
            if (RenderSettings.fog)
            {
                WriteAttribute(writer, subSubPrefix, "Fog Color", RenderSettings.fogColor.linear);
                WriteAttribute(writer, subSubPrefix, "Fog Start", RenderSettings.fogStartDistance);
                WriteAttribute(writer, subSubPrefix, "Fog End", RenderSettings.fogEndDistance);
                //switch (RenderSettings.fogMode)
                //{
                //    case FogMode.Linear:
                //        break;
                //    case FogMode.Exponential:
                //        break;
                //    case FogMode.ExponentialSquared:
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}
            }

            WriteAttribute(writer, subSubPrefix, "Bounding Box Min", -(reflectionProbe.size * 0.5f));
            WriteAttribute(writer, subSubPrefix, "Bounding Box Max", reflectionProbe.size * 0.5f);

            var volume = reflectionProbe.size.x * reflectionProbe.size.y * reflectionProbe.size.z;
            if (volume != 0)
            {
                var priority = int.MaxValue / (volume * 2);
                WriteAttribute(writer, subSubPrefix, "Priority", (int) priority);
            }

            WriteAttribute(writer, subSubPrefix, "Zone Texture", "TextureCube;" + texName);
            EndElement(writer, subPrefix);
        }

        private void ExportCustomComponent(XmlWriter writer, string subPrefix, IUrho3DComponent customComponent)
        {
            if (customComponent == null) return;

            var subSubPrefix = subPrefix + "\t";
            StartComponent(writer, subPrefix, customComponent.GetUrho3DComponentName());
            foreach (var keyValuePair in customComponent.GetUrho3DComponentAttributes())
                WriteAttribute(writer, subSubPrefix, keyValuePair.Name, keyValuePair.Value);
            EndElement(writer, subPrefix);
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, bool flag)
        {
            WriteAttribute(writer, prefix, name, flag ? "true" : "false");
        }

        private void WriteAttribute(XmlWriter writer, string prefix, string name, int flag)
        {
            WriteAttribute(writer, prefix, name, flag.ToString(CultureInfo.InvariantCulture));
        }

        private void ExportTerrain(XmlWriter writer, TerrainData terrainData, TerrainCollider terrainCollider,
            string subPrefix)
        {
            if (terrainData == null) return;

            var subSubPrefix = subPrefix + "\t";

            var terrainSize = terrainData.size;
            writer.WriteWhitespace(subPrefix);
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", (++_id).ToString());
            writer.WriteWhitespace("\n");

            _engine.ScheduleAssetExport(terrainData);

            var (min, max, size) = GetTerrainSize(terrainData);

            var offset = new Vector3(terrainSize.x * 0.5f, -min, terrainSize.z * 0.5f);
            WriteAttribute(writer, subPrefix, "Position", offset);
            StartComponent(writer, subPrefix, "Terrain");

            WriteAttribute(writer, subSubPrefix, "Height Map",
                "Image;" + _engine.EvaluateTerrainHeightMap(terrainData));
            WriteAttribute(writer, subSubPrefix, "Material",
                "Material;" + _engine.EvaluateTerrainMaterial(terrainData));
            //WriteTerrainMaterial(terrainData, materialFileName, "Textures/Terrains/" + folderAndName + ".Weights.tga");
            WriteAttribute(writer, subSubPrefix, "Vertex Spacing",
                new Vector3(terrainSize.x / size.x, 2.0f * (max - min), terrainSize.z / size.y));
            EndElement(writer, subPrefix);
            if (terrainCollider != null)
            {
                StartComponent(writer, subPrefix, "CollisionShape");
                WriteAttribute(writer, subPrefix, "Shape Type", "Terrain");
                EndElement(writer, subPrefix);
                StartComponent(writer, subPrefix, "RigidBody");
                var localToWorldMatrix = terrainCollider.transform.localToWorldMatrix;
                var pos = localToWorldMatrix.MultiplyPoint(offset);
                WriteAttribute(writer, subPrefix, "Physics Position", pos);
                EndElement(writer, subPrefix);
            }

            EndElement(writer, subPrefix);
        }

        public class Element : IDisposable
        {
            private readonly XmlWriter _writer;

            public Element(XmlWriter writer)
            {
                _writer = writer;
            }

            public static IDisposable Start(XmlWriter writer, string localName)
            {
                writer.WriteStartElement(localName);
                return new Element(writer);
            }

            public void Dispose()
            {
                _writer.WriteEndElement();
            }
        }
    }
}