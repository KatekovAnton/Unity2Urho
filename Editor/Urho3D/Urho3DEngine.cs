﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Assets.Scripts.UnityToCustomEngineExporter.Editor.Urho3D;
using GLTF.Schema;
using UnityEditor;
using UnityEngine;
using Animation = UnityEngine.Animation;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Object = UnityEngine.Object;
using Scene = UnityEngine.SceneManagement.Scene;
using Texture = UnityEngine.Texture;

//using UnityEngine.ProBuilder;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public class Urho3DEngine : AbstractDestinationEngine, IDestinationEngine
    {
        private readonly string _dataFolder;
        private readonly string _subfolder;
        private readonly bool _exportUpdatedOnly;
        private readonly bool _usePhysicalValues;
        private Dictionary<Object, string> _assetPaths = new Dictionary<Object, string>();
        private readonly Dictionary<string, string> _createdFiles = new Dictionary<string, string>();
        private readonly TextureExporter _textureExporter;
        private readonly CubemapExporter _cubemapExporter;
        private readonly MeshExporter _meshExporter;
        private readonly MaterialExporter _materialExporter;
        private readonly AudioExporter _audioExporter;
        private readonly SceneExporter _sceneExporter;
        private readonly PrefabExporter _prefabExporter;
        private readonly TerrainExporter _terrainExporter;

        public Urho3DEngine(string dataFolder, string subfolder, CancellationToken cancellationToken,
            bool exportUpdatedOnly,
            bool exportSceneAsPrefab, bool skipDisabled, bool usePhysicalValues)
            : base(cancellationToken)
        {
            _dataFolder = dataFolder;
            _subfolder = subfolder ?? "";
            _exportUpdatedOnly = exportUpdatedOnly;
            _usePhysicalValues = usePhysicalValues;
            _audioExporter = new AudioExporter(this);
            _textureExporter = new TextureExporter(this);
            _cubemapExporter = new CubemapExporter(this);
            _meshExporter = new MeshExporter(this);
            _materialExporter = new MaterialExporter(this);
            _sceneExporter = new SceneExporter(this, exportSceneAsPrefab, skipDisabled);
            _prefabExporter = new PrefabExporter(this, skipDisabled);
            _terrainExporter = new TerrainExporter(this);
            RequiredResources.Copy(this);
            _createdFiles.Clear();
        }

        public string Subfolder => _subfolder;

        public bool UsePhysicalValues => _usePhysicalValues;

        public string GetTargetFilePath(string relativePath)
        {
            return Path.Combine(_dataFolder, relativePath.FixDirectorySeparator()).FixDirectorySeparator();
        }

        public void TryWriteFile(string assetGuid, string destinationFilePath, byte[] bytes, DateTime sourceLastWriteTimeUtc)
        {
            if (destinationFilePath == null)
                return;

            var targetPath = GetTargetFilePath(destinationFilePath);

            //Skip file if it already exported
            if (!CheckForFileUniqueness(targetPath, assetGuid)) return;

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
                if (File.Exists(targetPath))
                {
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceLastWriteTimeUtc <= lastWriteTimeUtc)
                        return;
                }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            File.WriteAllBytes(targetPath, bytes);
        }

        private bool CheckForFileUniqueness(string targetPath, string assetGuid)
        {
            if (!_createdFiles.TryGetValue(targetPath, out var existingAsset))
            {
                _createdFiles.Add(targetPath, assetGuid);
                return true;
            }

            if (existingAsset != assetGuid)
            {
                Debug.LogError("Asset file name collision: "+AssetDatabase.GUIDToAssetPath(assetGuid)+" and "+AssetDatabase.GUIDToAssetPath(existingAsset)+" attempt to write into same file "+targetPath);
                return false;
            }

            return false;
        }

        public void TryCopyFile(string assetPath, string destinationFilePath)
        {
            if (destinationFilePath == null)
                return;

            var sourceFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            if (!File.Exists(sourceFilePath))
                return;
            var targetPath = GetTargetFilePath(destinationFilePath);

            //Skip file if it already exported
            if (!CheckForFileUniqueness(targetPath, AssetDatabase.AssetPathToGUID(assetPath))) return;

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
                if (File.Exists(targetPath))
                {
                    var sourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFilePath);
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceLastWriteTimeUtc <= lastWriteTimeUtc)
                        return;
                }

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            File.Copy(sourceFilePath, targetPath, true);
        }

        public bool IsUpToDate(string assetGuid, string relativePath, DateTime sourceFileTimestampUTC)
        {
            if (relativePath == null) return true;
            var targetPath = GetTargetFilePath(relativePath);

            //Skip file if it already exported
            if (_createdFiles.TryGetValue(targetPath, out var existingAsset))
            {
                if (existingAsset != assetGuid)
                {
                    Debug.LogError("Asset file name collision: " + AssetDatabase.GUIDToAssetPath(assetGuid) + " and " + AssetDatabase.GUIDToAssetPath(existingAsset) + " attempt to write into same file " + targetPath);
                    return true;
                }
                return true;
            }

            //Skip file if it is already up to date
            if (_exportUpdatedOnly)
                if (File.Exists(targetPath))
                {
                    var lastWriteTimeUtc = File.GetLastWriteTimeUtc(targetPath);
                    if (sourceFileTimestampUTC <= lastWriteTimeUtc)
                        return true;
                }

            return false;
        }

        public FileStream TryCreate(string assetGuid, string relativePath, DateTime sourceFileTimestampUTC)
        {
            if (IsUpToDate(assetGuid, relativePath, sourceFileTimestampUTC)) return null;
            var targetPath = GetTargetFilePath(relativePath);

            //Skip file if it already exported
            if (!CheckForFileUniqueness(targetPath, assetGuid)) return null;

            var directoryName = Path.GetDirectoryName(targetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            return File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public XmlWriter TryCreateXml(string assetGuid, string relativePath, DateTime sourceFileTimestampUTC)
        {
            var fileStream = TryCreate(assetGuid, relativePath, sourceFileTimestampUTC);
            if (fileStream == null)
                return null;
            return new XmlTextWriter(fileStream, new UTF8Encoding(false));
        }

        public void ScheduleTexture(Texture texture, TextureReference textureReference = null)
        {
            if (texture == null) return;
            EditorTaskScheduler.Default.ScheduleForegroundTask(
                () => _textureExporter.ExportTexture(texture, textureReference),
                texture.name + " from " + AssetDatabase.GetAssetPath(texture));
        }

        public string EvaluateCubemapName(Cubemap cubemap)
        {
            return _cubemapExporter.EvaluateCubemapName(cubemap);
        }

        public string EvaluateTextrueName(Texture texture)
        {
            if (texture == null)
                return null;

            if (texture is Cubemap cubemap) return EvaluateCubemapName(cubemap);

            return EvaluateTextrueName(texture, new TextureReference(TextureSemantic.Other));
        }

        public string EvaluateTextrueName(Texture texture, TextureReference textureReference)
        {
            if (texture == null)
                return null;
            return _textureExporter.EvaluateTextrueName(texture, textureReference);
        }

        public string EvaluateMaterialName(Material skyboxMaterial)
        {
            return _materialExporter.EvaluateMaterialName(skyboxMaterial);
        }

        public string EvaluateMeshName(Mesh sharedMesh)
        {
            return _meshExporter.EvaluateMeshName(sharedMesh);
        }
        //public string EvaluateMeshName(ProBuilderMesh sharedMesh)
        //{
        //    return _meshExporter.EvaluateMeshName(sharedMesh);
        //}

        public string EvaluateTerrainHeightMap(TerrainData terrainData)
        {
            return _terrainExporter.EvaluateHeightMap(terrainData);
        }

        public string EvaluateTerrainMaterial(TerrainData terrainData)
        {
            return _terrainExporter.EvaluateMaterial(terrainData);
        }

        public void ExportScene(Scene scene)
        {
            _sceneExporter.ExportScene(scene);
        }

        public void Dispose()
        {
        }

        protected override void ExportAssetBlock(string assetPath, Type mainType, Object[] assets)
        {
            if (mainType == typeof(GameObject))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                _meshExporter.ExportMesh(prefab);
                _prefabExporter.ExportPrefab(AssetDatabase.AssetPathToGUID(assetPath), _prefabExporter.EvaluatePrefabName(assetPath), prefab);
            }
            else
            {
                foreach (var asset in assets)
                    if (asset is Mesh mesh)
                        EditorTaskScheduler.Default.ScheduleForegroundTask(
                            () => _meshExporter.ExportMeshModel(mesh, null), mesh.name + " from " + assetPath);
            }

            foreach (var asset in assets)
                if (asset is Mesh mesh)
                {
                    //We already processed all meshes.
                }
                else if (asset is GameObject gameObject)
                {
                    //We already processed prefab.
                }
                else if (asset is Transform transform)
                {
                    //Skip
                }
                else if (asset is MeshRenderer meshRenderer)
                {
                    //Skip
                }
                else if (asset is MeshFilter meshFilter)
                {
                    //Skip
                }
                else if (asset is MeshCollider meshCollider)
                {
                    //Skip
                }
                //else if (asset is ProBuilderMesh proBuilderMesh)
                //{
                //    //Skip
                //}
                else if (asset is LODGroup lodGroup)
                {
                    //Skip
                }
                else if (asset is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    //Skip
                }
                else if (asset is Animation animation)
                {
                    //Skip
                }
                else if (asset is AudioClip audioClip)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _audioExporter.ExportClip(audioClip), audioClip.name + " from " + assetPath);
                }
                else if (asset is Material material)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _materialExporter.ExportMaterial(material),
                        material.name + " from " + assetPath);
                }
                else if (asset is TerrainData terrainData)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _terrainExporter.ExportTerrain(terrainData), terrainData.name + " from " + assetPath);
                }
                else if (asset is Texture2D texture2d)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _textureExporter.ExportTexture(texture2d, new TextureReference(TextureSemantic.Other)),
                        texture2d.name + " from " + assetPath);
                }
                else if (asset is Cubemap cubemap)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(() => _cubemapExporter.Cubemap(cubemap),
                        cubemap.name + " from " + assetPath);
                }
                else if (asset is AnimationClip animationClip)
                {
                    EditorTaskScheduler.Default.ScheduleForegroundTask(
                        () => _meshExporter.ExportAnimation(animationClip), animationClip.name + " from " + assetPath);
                }
                else
                {
                    //Debug.LogWarning("UnknownAssetType " + asset.GetType().Name);
                }
        }

        public void SchedulePBRTextures(MetallicGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            EditorTaskScheduler.Default.ScheduleForegroundTask(()=>_textureExporter.ExportPBRTextures(arguments, urhoMaterial), urhoMaterial.MetallicRoughnessTexture);
        }
        public void SchedulePBRTextures(SpecularGlossinessShaderArguments arguments, UrhoPBRMaterial urhoMaterial)
        {
            EditorTaskScheduler.Default.ScheduleForegroundTask(() => _textureExporter.ExportPBRTextures(arguments, urhoMaterial), urhoMaterial.MetallicRoughnessTexture);
        }

        public string EvaluateAudioClipName(AudioClip audioClip)
        {
            return _audioExporter.EvaluateAudioClipName(audioClip);
        }
    }
}