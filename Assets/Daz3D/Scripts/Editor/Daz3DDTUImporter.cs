﻿#define USING_URP
#define USING_HARDCODED_RENDERPIPELINE


using UnityEditor;

using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;

namespace Daz3D
{

    [UnityEditor.AssetImporters.ScriptedImporter(1, "dtu", 0x7FFFFFFF)]
    public class Daz3DDTUImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        public static bool AutoImportDTUChanges = true;
        public static bool GenerateUnityPrefab = true;
        public static bool ReplaceSceneInstances = true;
        public static bool AutomateMecanimAvatarMappings = true;
        public static bool ReplaceMaterials = true;
        public static bool EnableDForceSupport = false;
#if USING_HDRP || USING_URP
        public static bool UseLegacyShaders = false;
#else
        public static bool UseLegacyShaders = true;
#endif

        public static void ResetOptions()
        {
            AutoImportDTUChanges = true;
            GenerateUnityPrefab = true;
            ReplaceSceneInstances = true;
            AutomateMecanimAvatarMappings = true;
            ReplaceMaterials = true;
            EnableDForceSupport = false;
#if USING_HDRP || USING_URP
            UseLegacyShaders = false;
#else
            UseLegacyShaders = true;
#endif
        }

        [Serializable]
        public class ImportEventRecord
        {
            public DateTime Timestamp = DateTime.Now;
            public struct Token
            {
                public string Text;
                public UnityEngine.Object Selectable;
                public bool EndLine;
            }

            public List<Token> Tokens = new List<Token>();

            public bool Unfold = true;

            internal void AddToken(string str, UnityEngine.Object obj = null, bool endline = false)
            {
                Tokens.Add(new Token() { Text = str, Selectable = obj, EndLine = endline });
            }
        }


        public static Queue<ImportEventRecord> EventQueue = new Queue<ImportEventRecord>();
        private static Dictionary<string, Material> s_StandardMaterialCollection = new Dictionary<string, Material>();
        private static MaterialMap _map = null;
        // DB (2021-05-25): dforceImport
        private static DForceMaterialMap _dforceMap = null;
        private const bool ENDLINE = true;
        
        public static void EmptyEventQueue()
        {
            EventQueue = new Queue<ImportEventRecord>();
        }

        public enum DazFigurePlatform
        {
            Genesis8,
            Genesis3,
            Genesis2,
            Victoria,
            Genesis,
            Michael,
            TheFreak,
            Victoria4,
            Victoria4Elite,
            Michael4,
            Michael4Elite,
            Stephanie4,
            Aiko4
        }

        public static void FoldAll()
        {
            foreach (var record in EventQueue)
                record.Unfold = false;
        }

        /// <summary>
        /// Method called by Unity Editor when ImportAsset event occurs.  
        /// This will probably be the first DTU Brudge code which is executed
        /// when the DTU Bridge is first installed into Unity.
        /// </summary>
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            if (Daz3DBridge.BatchConversionMode != 0) return;

            if (AutoImportDTUChanges)
            {
                var dtuPath = ctx.assetPath;
                var fbxPath = dtuPath.Replace(".dtu", ".fbx");

                Import(dtuPath, fbxPath);
            }
        }

        [MenuItem("Daz3D/Create Unity Prefab from selected DTU", false, 101)]
        public static void MenuItemConvert()
        {
            var activeObject = Selection.activeObject;
            var dtuPath = AssetDatabase.GetAssetPath(activeObject);
            var fbxPath = dtuPath.Replace(".dtu", ".fbx");

            Import(dtuPath, fbxPath); 

        }

        [MenuItem("Daz3D/Create Unity Prefab from selected DTU", true)]
        [MenuItem("Daz3D/Extract materials from selected DTU", true)]
        [MenuItem("Assets/Daz3D/Create Unity Prefab", true)]
        [MenuItem("Assets/Daz3D/Extract materials", true)]
        public static bool ValidateDTUSelected()
        {
            var obj = Selection.activeObject;

            // Return false if no transform is selected.
            if (obj == null)
                return false;

            return (AssetDatabase.GetAssetPath(obj).ToLower().EndsWith(".dtu"));
        }

        public class MaterialMap
        {
            public MaterialMap(string path)
            {
                Path = path;
            }

            public void AddMaterial(string key, UnityEngine.Material material)
            {
                if (material && !Map.ContainsKey(key))
                    Map.Add(key, material);
            }
            public string Path { get; set; }
            public Dictionary<string, UnityEngine.Material> Map = new Dictionary<string, UnityEngine.Material>();
        }


        public class DForceMaterial
        {
            public DForceMaterial(DTUMaterial dtuMat) 
            {
                name = dtuMat.MaterialName;
                dtuMaterial = dtuMat;
            }

            public string name;
            public DTUMaterial dtuMaterial;

/*
            public static bool operator ==(Object x, Object y);
            public static bool operator !=(Object x, Object y);
            public static implicit operator bool(Object exists);
*/

        }

        public class DForceMaterialMap
        {
            public DForceMaterialMap(string path)
            {
                Path = path;
            }

            public void AddMaterial(DForceMaterial dforceMat)
            {
                if (dforceMat == null)
                {
                    return;
                }
                if (!Map.ContainsKey(dforceMat.name))
                    Map.Add(dforceMat.name, dforceMat);
            }
            public string Path { get; set; }
            public Dictionary<string, DForceMaterial> Map = new Dictionary<string, DForceMaterial>();

        }


        public static void Import(string dtuPath, string fbxPath)
        {
            DazCoroutine.StartCoroutine(ImportRoutine(dtuPath, fbxPath));
        }


        public static bool IsRenderPipelineDetected()
        {
#if !USING_HDRP && !USING_URP && !USING_BUILTIN
            ImportEventRecord record = new ImportEventRecord();
            EventQueue.Enqueue(record);
            record.AddToken("DTU Bridge must autodetect a RenderPipeline in order to continue.\nThis will involve updating Symbol Definitions and will trigger \nUnity Editor to recompile all scripts.");

            return false;
#else
            return true;
#endif
        }

        private static IEnumerator ImportRoutine(string dtuPath, string fbxPath)
        {
            //DEBUG
            //Debug.LogError("dtuPath = [" + dtuPath + "] " + dtuPath.Length);

            if (Daz3DBridge.BatchConversionMode == 0)
            {
                Daz3DBridge.ShowWindow();
                Daz3DBridge.CurrentToolbarMode = Daz3DBridge.ToolbarMode.History; //force into history mode during import
            }

            Daz3DBridge.Progress = .03f;
                yield return new WaitForEndOfFrame();

            if (IsRenderPipelineDetected() == false)
            {
                // DB: Write path of asset to be imported in temporary file,
                //     this will be restored and continued after global script recompilation takes place.
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(dtuPath);
                System.IO.File.WriteAllBytes("Assets/Daz3D/dtu_toload.txt", buffer);
                
                yield return new WaitForEndOfFrame();

                DetectRenderPipeline.RunOnce();

            }

            _map = new MaterialMap(dtuPath);
            _dforceMap = new DForceMaterialMap(dtuPath);

            while (!IrayShadersReady())
                yield return new WaitForEndOfFrame();

            var dtu = new DTU();
            var routine = ImportDTURoutine(dtuPath, (d => dtu = d), .8f);
            while (routine.MoveNext())
                yield return new WaitForEndOfFrame();

            if (dtu.AssetType == "Animation")
            {
                Daz3DBridge.Progress = 0;
                _map = null;
                _dforceMap = null;
                Daz3DBridge.ReadyToImport = true;
                yield break;
            }

            //ImportDTU(dtuPath);
            if (dtu.AssetType == null)
            {
                Daz3DBridge.Progress = 0;
                _map = null;
                _dforceMap = null;
                Daz3DBridge.ReadyToImport = true;
                yield break;
            }

            DazFigurePlatform platform = DiscoverFigurePlatform(dtu);

            Daz3DBridge.Progress = .9f;
                yield return new WaitForEndOfFrame();

            if (GenerateUnityPrefab)
                GeneratePrefabFromFBX(fbxPath, platform, dtu);

            Daz3DBridge.Progress = 1f;
                yield return new WaitForEndOfFrame();

            _map = null;
            _dforceMap = null;

            Daz3DBridge.Progress = 0;

            // DB 2021-09-02: Show DTUImport complete dialog
            if (Daz3DBridge.BatchConversionMode == 0)
            {
                EditorUtility.DisplayDialog("DTU Bridge Import", "Import Completed for " + dtuPath, "OK");
                Daz3DBridge.AddDiffusionProfilePrompt();
            }

            Daz3DBridge.ReadyToImport = true;

            yield break;
        }

        private static DazFigurePlatform DiscoverFigurePlatform(DTU dtu)
        {
            var token = dtu.AssetID.ToLower();

            foreach(DazFigurePlatform dfp in Enum.GetValues(typeof (DazFigurePlatform)))
            {
                if (token.Contains(dfp.ToString().ToLower()))
                    return dfp;
            }

            return DazFigurePlatform.Genesis8;//default
        }

        private static bool IrayShadersReady()
        {

#if USING_HDRP || USING_BUILTIN
            if (
                Shader.Find(DTU_Constants.shaderNameMetal) == null ||
                Shader.Find(DTU_Constants.shaderNameSpecular) == null ||
                Shader.Find(DTU_Constants.shaderNameIraySkin) == null ||
                Shader.Find(DTU_Constants.shaderNameHair) == null ||
                Shader.Find(DTU_Constants.shaderNameWet) == null ||
                Shader.Find(DTU_Constants.shaderNameInvisible) == null
            ) {
                return false;
            }

            return true;
#elif USING_URP
            if (
                Shader.Find(DTU_Constants.newShaderNameBase + "Hair") == null ||
                Shader.Find(DTU_Constants.newShaderNameBase + "SSS") == null ||
                Shader.Find(DTU_Constants.newShaderNameBase + "Specular") == null ||
                Shader.Find(DTU_Constants.newShaderNameBase + "Metallic") == null 
            )
            {
                return false;
            }

            return true;
#else
            return false;
#endif
        }

        //// unused blocking method
        //public static void ImportDTU(string path)
        //{
        //    Debug.Log("ImportDTU for " + path);

        //    FoldAll();

        //    ImportEventRecord record = new ImportEventRecord();
        //    EventQueue.Enqueue(record);

        //    var dtu = DTUConverter.ParseDTUFile(path);

        //    var dtuObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

        //    // DB (2021-05-15): skip anim DTU
        //    if (dtu.AssetType == "Animation")
        //    {
        //        record.AddToken("Skipping prefab creation for animation DTU file: " + path);
        //        return;
        //    }

        //    record.AddToken("Imported DTU file: " + path);
        //    record.AddToken(dtuObject.name, dtuObject, ENDLINE);

        //    //UnityEngine.Debug.Log("DTU: " + dtu.AssetName + " contains: " + dtu.Materials.Count + " materials");

        //    record.AddToken("Generated materials: ");
        //    foreach (var dtuMat in dtu.Materials)
        //    {
        //        var material = dtu.ConvertToUnity(dtuMat);
        //        _map.AddMaterial(material);

        //        // DB (2021-05-25): DForce import
        //        if (dtu.IsDTUMaterialDForceEnabled(dtuMat))
        //        {
        //            _dforceMap.AddMaterial(new DForceMaterial(dtuMat));
        //        }

        //        record.AddToken(material.name, material);
        //    }
        //    record.AddToken(" based on DTU file.", null, ENDLINE);


        //    Daz3DBridge bridge = EditorWindow.GetWindow(typeof(Daz3DBridge)) as Daz3DBridge;
        //    if (bridge == null)
        //    {
        //        var consoleType = Type.GetType("ConsoleWindow,UnityEditor.dll");
        //        bridge = EditorWindow.CreateWindow<Daz3DBridge>(new[] { consoleType });
        //    }

        //    bridge?.Focus();

        //    //just a safeguard to keep the history data at a managable size (100 records)
        //    while (EventQueue.Count > 100)
        //    {
        //        EventQueue.Dequeue();
        //    }

        //}


        public static IEnumerator ImportDTURoutine(string path, Action<DTU> dtuOut, float progressLimit)
        {
            Debug.Log("ImportDTU for " + path);

            FoldAll();

            ImportEventRecord record = new ImportEventRecord();
            EventQueue.Enqueue(record);

            var dtu = DTUConverter.ParseDTUFile(path);

            if (Daz3DBridge.BatchConversionMode == 1)
            {
                dtu.UseSharedMaterialDir = true;
                dtu.UseSharedTextureDir = true;
            }

            // DB (2021-05-15): skip DTU import if animation
            if (dtu.AssetType == "Animation")
            {
                record.AddToken("Skipping prefab creation for animation DTU file: " + path);
                Daz3DBridge.Progress = 0;
                yield break;
            }

            dtuOut(dtu);

            var dtuObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            record.AddToken("Imported DTU file: " + path);
            if (dtuObject != null)
                record.AddToken(dtuObject.name, dtuObject, ENDLINE);

            //UnityEngine.Debug.Log("DTU: " + dtu.AssetName + " contains: " + dtu.Materials.Count + " materials");


            record.AddToken("Generated materials: ");
            float progressIncrement = (progressLimit - Daz3DBridge.Progress) / dtu.Materials.Count;

            for (int i = 0; i < dtu.Materials.Count; i++)
            {
                DTUMaterial dtuMat = dtu.Materials[i];
                var material = dtu.ConvertToUnity(dtuMat);
                if (!material)
                {
                    continue;
                }

                var key = Utilities.ScrubKey(dtuMat.MaterialName);
                _map.AddMaterial(key, material);

                // DB (2021-05-25): DForce import
                if (dtu.IsDTUMaterialDForceEnabled(dtuMat))
                {
                    _dforceMap.AddMaterial(new DForceMaterial(dtuMat));
                }

                record.AddToken(material.name, material);

                Daz3DBridge.Progress = Mathf.MoveTowards(Daz3DBridge.Progress, progressLimit, progressIncrement);

                yield return new WaitForEndOfFrame();

            }
            record.AddToken(" based on DTU file.", null, ENDLINE);

            if (Daz3DBridge.BatchConversionMode == 0)
            {
                Daz3DBridge bridge = EditorWindow.GetWindow(typeof(Daz3DBridge)) as Daz3DBridge;
                if (bridge == null)
                {
                    var consoleType = Type.GetType("ConsoleWindow,UnityEditor.dll");
                    bridge = EditorWindow.CreateWindow<Daz3DBridge>(new[] { consoleType });
                }
                bridge?.Focus();
            }

            //just a safeguard to keep the history data at a managable size (100 records)
            while (EventQueue.Count > 100)
            {
                EventQueue.Dequeue();
            }

            yield break;
        }


        enum MaterialID //these positions map to the bitflags in the compiled HDRP lit shader
        {
            SSS = 0,
            Standard = 1,
            Anisotropy = 2,
            Iridescence = 3,
            SpecularColor = 4,
            Translucent = 5
        }

        private enum StandardMaterialType
        {
            Arms,
            Cornea,
            Ears,
            Eyelashes,
            EyeMoisture_1,
            EyeMoisture,
            EyeSocket,
            Face,
            Fingernails,
            Irises,
            Legs,
            Lips,
            Mouth,
            Pupils,
            Sclera,
            Teeth,
            Toenails,
            Torso
        }

        public static void GeneratePrefabFromFBX(string fbxPath, DazFigurePlatform platform, DTU dtu)
        {
            var fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            if (fbxPrefab == null)
            {
                Debug.LogError("no FBX model prefab found at " + fbxPath);
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(fbxPrefab) != PrefabAssetType.Model)
            {
                Debug.LogError(fbxPath + " is not a model prefab ");
                return;
            }

           

            System.Reflection.MethodInfo resetPose = null;
            System.Reflection.MethodInfo xferPose = null;

            var avatarInstance = Instantiate(fbxPrefab);
            avatarInstance.name = "AvatarInstance";

            if (AutomateMecanimAvatarMappings)
            { 
                var record = new ImportEventRecord();

                ModelImporter importer = GetAtPath(fbxPath) as ModelImporter;
                if (importer)
                {
                    var description = importer.humanDescription;
                    DescribeHumanJointsForFigure(ref description, platform);

                    importer.humanDescription = description;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

                    // Genesis 8 is modeled in A-pose, so we correct to T-pose before configuring avatar joints
                    //using Unity's internal MakePoseValid method, which does a perfect job
                    if (platform == DazFigurePlatform.Genesis8 && false)
                    {
                        //use reflection to access AvatarSetupTool;
                        var setupToolType = Type.GetType("UnityEditor.AvatarSetupTool,UnityEditor.dll");
                        var boneWrapperType = Type.GetType("UnityEditor.AvatarSetupTool+BoneWrapper,UnityEditor.dll");

                        if (boneWrapperType != null && setupToolType != null)
                        {
                            var existingMappings = new Dictionary<string, string>();
                            var human = description.human;

                            for (var i = 0; i < human.Length; ++i)
                                existingMappings[human[i].humanName] = human[i].boneName;

                            var getModelBones = setupToolType.GetMethod("GetModelBones");
                            var getHumanBones = setupToolType.GetMethod("GetHumanBones", new[] { typeof(Dictionary<string, string>), typeof(Dictionary<Transform, bool>) });
                            var makePoseValid = setupToolType.GetMethod("MakePoseValid");
                            resetPose = setupToolType.GetMethod("CopyPose");
                            xferPose = setupToolType.GetMethod("TransferPoseToDescription");

                            if (getModelBones != null && getHumanBones != null && makePoseValid != null)
                            { 
                                record.AddToken("Corrected Avatar Setup T-pose for Genesis8 figure: ", null);
                                record.AddToken(fbxPrefab.name, fbxPrefab, ENDLINE);

                                var modelBones = (Dictionary<Transform, bool>)getModelBones.Invoke(null, new object[] { avatarInstance.transform, false, null });
                                var humanBones = (ICollection<object>)getHumanBones.Invoke(null, new object[] { existingMappings, modelBones });

                                // a little dance to populate array of Unity's internal BoneWrapper type 
                                var humanBonesArray = new object[humanBones.Count];
                                humanBones.CopyTo(humanBonesArray, 0);
                                Array destinationArray = Array.CreateInstance(boneWrapperType, humanBones.Count);
                                Array.Copy(humanBonesArray, destinationArray, humanBones.Count);

                                //This mutates the transforms (modelBones) via Bonewrapper class
                                makePoseValid.Invoke(null, new[] { destinationArray });
                            }
                        }
                    }

                    AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
                    AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);


                    // i think this might unT-pose the gen8 skeleton instance
                    if (resetPose != null && xferPose != null)
                    {
                        SerializedObject modelImporterObj = new SerializedObject(importer);
                        var skeleton = modelImporterObj?.FindProperty("m_HumanDescription.m_Skeleton");

                        if (skeleton != null)
                        {
                            resetPose.Invoke(null, new object[] { avatarInstance, fbxPrefab });
                            //xferPose.Invoke(null, new object[] { skeleton, avatarInstance.transform });
                        }

                    }

                    DestroyImmediate(avatarInstance);

                    record.AddToken("Automated Mecanim avatar setup for " + fbxPrefab.name + ": ");

                    //a little dance to get the avatar just reimported
                    var allAvatars = Resources.FindObjectsOfTypeAll(typeof(Avatar));
                    var avatar = Array.Find(allAvatars, element => element.name.StartsWith(fbxPrefab.name));
                    if (avatar)
                        record.AddToken(avatar.name, avatar, ENDLINE);
                }
                else
                {
                    Debug.LogWarning("Could not acquire importer for " + fbxPath + " ...could not automatically configure humanoid avatar.");
                    record.AddToken("Could not acquire importer for " + fbxPath + " ...could not automatically configure humanoid avatar.", null, ENDLINE);
                }

                EventQueue.Enqueue(record);
            }


            //remap the materials
            var workingInstance = Instantiate(fbxPrefab);
            string workingInstance_name = fbxPrefab.name;
            if (dtu.ProductComponentName != "")
                workingInstance_name = dtu.ProductComponentName;
            else if (dtu.AssetName != "")
                workingInstance_name = dtu.AssetName;
            workingInstance.name = workingInstance_name;

            var renderers = workingInstance.GetComponentsInChildren<Renderer>();
            if (renderers?.Length == 0)
            {
                Debug.LogError("DazBridge: No renderers found for material remapping.");
                return;
            }

            var modelPath = AssetDatabase.GetAssetPath(fbxPrefab);

            if (ReplaceMaterials)
            {
                foreach (var renderer in renderers)
                {
                    var dict = new Dictionary<Material, Material>();

                    if (renderer.name.ToLower().Contains("eyelashes"))
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    // DB (2021-05-07): SANITY CHECK
                    if (renderer.sharedMaterials == null)
                    {
                        Debug.LogError("DB (2021-05-07), ERROR: GeneratePrefabFromFBX(): sharedMaterials is null!");
                    }
                    else
                    {
                        foreach (var keyMat in renderer.sharedMaterials)
                        {
                            // DB (2021-05-07): SANITY CHECK
                            if (keyMat == null)
                            {
                                Debug.LogError("DB (2021-05-07), ERROR: keyMat is NULL");
                                continue;
                            }
                            var key = keyMat.name;

                            key = Daz3D.Utilities.ScrubKey(key);

                            Material nuMat = null;

                            if (_map != null && _map.Map.ContainsKey(key))
                            {
                                nuMat = _map.Map[key];// the preferred uber/iRay based material generated by the DTUConverter

                                // DB (2021-05-25): dForce import
                                if (_dforceMap.Map.ContainsKey(key))
                                {
                                    if (EnableDForceSupport)
                                        ImportDforceToPrefab(key, renderer, workingInstance, keyMat);

                                    //DForceMaterial dforceMat = _dforceMap.Map[key];
                                    //GameObject parent = renderer.gameObject;
                                    //SkinnedMeshRenderer skinned = parent.GetComponent<SkinnedMeshRenderer>();
                                    //Cloth cloth;

                                    //// add Unity Cloth Physics component to gameobject parent of the renderer
                                    //if (parent.GetComponent<Cloth>() == null)
                                    //{
                                    //    cloth = parent.AddComponent<Cloth>();
                                    //    // assign values from dtuMat
                                    //    cloth.stretchingStiffness = dforceMat.dtuMaterial.Get("Stretch Stiffness").Float;
                                    //    cloth.bendingStiffness = dforceMat.dtuMaterial.Get("Bend Stiffness").Float;
                                    //    cloth.damping = dforceMat.dtuMaterial.Get("Damping").Float;
                                    //    cloth.friction = dforceMat.dtuMaterial.Get("Friction").Float;

                                    //    // fix SkinnedMeshRenderer boundaries bug
                                    //    skinned.updateWhenOffscreen = true;

                                    //    // Add G8F cloth collision rig
                                    //    var searchResult = workingInstance.transform.Find("Cloth Collision Rig");
                                    //    GameObject collision_instance = (searchResult != null) ? searchResult.gameObject : null;
                                    //    if (collision_instance == null)
                                    //    {
                                    //        GameObject collision_prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Daz3D/Resources/G8F Collision Rig.prefab");
                                    //        collision_instance = Instantiate<GameObject>(collision_prefab);
                                    //        collision_instance.name = "Cloth Collision Rig";
                                    //        collision_instance.transform.parent = workingInstance.transform;
                                    //        // merge cloth collision rig to figure root bone
                                    //        collision_instance.GetComponent<ClothCollisionAssigner>().mergeRig(skinned.rootBone);
                                    //    }
                                    //    ClothCollisionAssigner.ClothConfig clothConfig = new ClothCollisionAssigner.ClothConfig();
                                    //    clothConfig.m_ClothToManage = cloth;
                                    //    clothConfig.m_UpperBody = true;
                                    //    clothConfig.m_LowerBody = true;
                                    //    collision_instance.GetComponent<ClothCollisionAssigner>().addClothConfig(clothConfig);

                                    //}
                                    //else
                                    //{
                                    //    cloth = parent.GetComponent<Cloth>();
                                    //}

                                    //// add clothtools to gameobject parent of renderer
                                    //ClothTools clothTools;
                                    //if (parent.GetComponent<ClothTools>() == null)
                                    //{
                                    //    clothTools = parent.AddComponent<ClothTools>();
                                    //    clothTools.GenerateLookupTables();
                                    //}
                                    //else
                                    //{
                                    //    clothTools = parent.GetComponent<ClothTools>();
                                    //}

                                    //int matIndex = Array.IndexOf(skinned.sharedMaterials, keyMat);
                                    //// get vertex list for this material's submesh
                                    //if (matIndex >= 0)
                                    //{
                                    //    float simulation_strength;
                                    //    //// map the materical's submesh's vertices to the correct "Dynamics Strength"
                                    //    simulation_strength = dforceMat.dtuMaterial.Get("Dynamics Strength").Float;
                                    //    Debug.Log("DEBUG INFO: simulation strength: " + simulation_strength);
                                    //    //// DEBUG line to map simulation strength to material index
                                    //    //simulation_strength = matIndex;

                                    //    //// Tiered scaling function
                                    //    float adjusted_simulation_strength;
                                    //    //float strength_max = 1.0f;
                                    //    //float strength_min = 0.0f;
                                    //    float strength_scale_threshold = 0.5f;
                                    //    if (simulation_strength <= strength_scale_threshold)
                                    //    {
                                    //        //// stronger compression of values below threshold
                                    //        float scale = 0.075f;
                                    //        float offset = 0.2f;
                                    //        adjusted_simulation_strength = (simulation_strength - offset) * scale;
                                    //    }
                                    //    else
                                    //    {
                                    //        float offset = (strength_scale_threshold - 0.2f) * 0.075f; // offset = (threshold - previous tier's offset) * previous teir's scale
                                    //        float scale = 0.2f;
                                    //        adjusted_simulation_strength = (simulation_strength - offset) / (1 - offset); // apply offset, then normalize to 1.0
                                    //        adjusted_simulation_strength *= scale;
                                    //    }
                                    //    //// clamp to 0.0f to 0.2f
                                    //    float coeff_min = 0.0f;
                                    //    float coeff_max = 0.2f;
                                    //    adjusted_simulation_strength = (adjusted_simulation_strength > coeff_min) ? adjusted_simulation_strength : coeff_min;
                                    //    adjusted_simulation_strength = (adjusted_simulation_strength < coeff_max) ? adjusted_simulation_strength : coeff_max;
                                    //    //// Debug line for no scaling
                                    //    //adjusted_simulation_strength = simulation_strength;

                                    //    clothTools.SetSubMeshWeights(matIndex, adjusted_simulation_strength);

                                    //}

                                }

                            }
                            else if (s_StandardMaterialCollection.ContainsKey(key))
                            {
                                nuMat = new Material(s_StandardMaterialCollection[key]);
                                //FixupStandardBasedMaterial(ref nuMat, fbxPrefab, keyMat.name, data);
                            }
                            else
                            {
                                Debug.LogError("DazBridge: No imported materials were found for material remapping");
                                continue;

                                /****
                                 ** Everything below is old and broken.
                                 ** Fbx exported from DazBridges no longer embed textures.
                                 ****
                                var shader = Shader.Find("HDRP/Lit");

                                if (shader == null)
                                {
                                    Debug.LogWarning("couldn't find HDRP/Lit shader");
                                    continue;
                                }

                                nuMat = new Material(shader);
                                nuMat.CopyPropertiesFromMaterial(keyMat);

                                // just copy the textures, colors and scalars that are appropriate given the base material type
                                //DazMaterialPropertiesInfo info = new DazMaterialPropertiesInfo();
                                //CustomizeMaterial(ref nuMat, info);

                                var matPath = Path.GetDirectoryName(modelPath);
                                matPath = Path.Combine(matPath, fbxPrefab.name + "Daz3D_Materials");
                                matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);

                                if (!Directory.Exists(matPath))
                                    Directory.CreateDirectory(matPath);

                                //Debug.Log("obj path " + path);
                                AssetDatabase.CreateAsset(nuMat, matPath + "/Daz3D_" + keyMat.name + ".mat");
                                */
                            }

                            dict.Add(keyMat, nuMat);

                        }

                        //remap the meshes in the fbx prefab to the value materials in dict
                        var count = renderer.sharedMaterials.Length;
                        var copy = new Material[count]; //makes a copy
                        for (int i = 0; i < count; i++)
                        {
                            var key = renderer.sharedMaterials[i];
                            // DB (2021-05-07): SANITY CHECK
                            if (key == null || !dict.ContainsKey(key))
                            {
                                Debug.LogError("DB (2021-05-07), ERROR: GeneratePrefabFromFBX(): sharedMaterials[" + i + "] (" + renderer.sharedMaterials + ") returned invalid key.");
                                if (key != null)
                                    Debug.LogError(" part 2: key==" + key);
                                else
                                    Debug.LogError(" part 2: key==null");
                            }
                            else
                            {
                                Debug.Log("remapping: " + renderer.sharedMaterials[i].name + " to " + dict[key].name);
                                copy[i] = dict[key];//fill copy
                            }
                        }

                        renderer.sharedMaterials = copy;//overwrite sharedMaterials, because set indexer assigns to a copy

                    }

                }
            }

            //write the prefab to the asset database
            // Make sure the file name is unique, in case an existing Prefab has the same name.
            var nuPrefabPathPath = Path.GetDirectoryName(modelPath);

            //nuPrefabPathPath = Path.Combine(nuPrefabPathPath, fbxPrefab.name + "_Prefab");
            //nuPrefabPathPath = AssetDatabase.GenerateUniqueAssetPath(nuPrefabPathPath);
            //if (!Directory.Exists(nuPrefabPathPath))
            //    Directory.CreateDirectory(nuPrefabPathPath);

            nuPrefabPathPath = Path.Combine(nuPrefabPathPath, "Prefabs");
            if (!Directory.Exists(nuPrefabPathPath))
                Directory.CreateDirectory(nuPrefabPathPath);

            string prefabFilestem = fbxPrefab.name;
            if (dtu.ProductComponentName != "")
                prefabFilestem = Daz3D.Utilities.ScrubKey(dtu.ProductComponentName);
            else if (dtu.AssetName != "")
                prefabFilestem = Daz3D.Utilities.ScrubKey(dtu.AssetName);
            nuPrefabPathPath += "/" + prefabFilestem + "_Prefab.prefab";
            nuPrefabPathPath = AssetDatabase.GenerateUniqueAssetPath(nuPrefabPathPath);

            // For future refreshment
            var component = workingInstance.AddComponent<Daz3DInstance>();
            component.SourceFBX = fbxPrefab;


            // Create the new Prefab.
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(workingInstance, nuPrefabPathPath, InteractionMode.AutomatedAction);
            Selection.activeGameObject = prefab;

            //now, seek other instance(s) in the scene having been sourced from this fbx asset
            var otherInstances = FindObjectsOfType<Daz3DInstance>();
            int foundCount = 0;

            var resultingInstance = workingInstance;

            if (ReplaceSceneInstances)
            {
                foreach (var otherInstance in otherInstances)
                {
                    if (otherInstance == component)//ignore this working instance
                        continue;
                    if (otherInstance.SourceFBX != fbxPrefab)//ignore instances of other assets
                        continue;


                    //for any found that flag ReplaceOnImport, delete that instance and replace with a copy of 
                    //this one, at their respective transforms
                    if (otherInstance.ReplaceOnImport)
                    {
                        foundCount++;
                        var xform = otherInstance.transform;
                        var replacementInstance = PrefabUtility.InstantiatePrefab(prefab, xform.parent) as GameObject;
                        replacementInstance.transform.position = xform.position;
                        replacementInstance.transform.rotation = xform.rotation;
                        //var replacementInstance = Instantiate(prefab, xform.position, xform.rotation, xform.parent);
                        //PrefabUtility.RevertPrefabInstance(replacementInstance, InteractionMode.AutomatedAction);
                        DestroyImmediate(otherInstance.gameObject);
                        resultingInstance = replacementInstance;
                    }
                }
            }

            //if no prior instances found, then don't destroy this instance
            //since it appears to be the first one to arrive
            if (foundCount > 0)
                DestroyImmediate(workingInstance);

            ImportEventRecord pfbRecord = new ImportEventRecord();
            pfbRecord.AddToken("Created Unity Prefab: ");
            pfbRecord.AddToken(prefab.name, prefab);
            pfbRecord.AddToken(" and an instance in the scene: ");
            pfbRecord.AddToken(resultingInstance.name, resultingInstance, ENDLINE);
            EventQueue.Enqueue(pfbRecord);

            if (Daz3DBridge.BatchConversionMode == 0)
            {
                //highlight/select the object in the scene view
                Selection.activeGameObject = resultingInstance;
            }
            else if (Daz3DBridge.BatchConversionMode == 1)
            { 
                DestroyImmediate(resultingInstance);
            }

        }

        private static void ImportDforceToPrefab(string key, Renderer renderer, GameObject workingInstance, Material keyMat)
        {
            DForceMaterial dforceMat = _dforceMap.Map[key];
            GameObject parent = renderer.gameObject;
            SkinnedMeshRenderer skinned = parent.GetComponent<SkinnedMeshRenderer>();
            Cloth cloth;

            string valueLower = key.ToLower();
            string assetNameLower = parent.name.ToLower();
            string matNameLower = keyMat.name.ToLower();
            if (
                valueLower.Contains("hair") || assetNameLower.EndsWith("hair") || matNameLower.Contains("hair")
                || valueLower.Contains("moustache") || assetNameLower.EndsWith("moustache") || matNameLower.Contains("moustache")
                || valueLower.Contains("beard") || assetNameLower.EndsWith("beard") || matNameLower.Contains("beard")
            )
            {
                // TODO: implement dForce hair support
                Debug.LogWarning("Import Warning: ImportDforceToPrefab() dForce hair is currently not supported: " + parent.name);
                return;
            }

            if (skinned == null)
            {
                // TODO: check if regular mesh renderer and upgrade if appropriate
                Debug.LogWarning("Import Warning: ImportDforceToPrefab() gameojbect unsupported: it does not have a skinned mesh renderer: " + parent.name);
                return;
            }
            else if (skinned.sharedMesh.vertexCount > 40000)
            {
                int numverts = skinned.sharedMesh.vertexCount;
                Debug.LogWarning("Import Warning: ImportDforceToPrefab() gameojbect unsupported: too many vertices: " + parent.name + " (" + numverts.ToString() + ")");
                return;

            }

            // add Unity Cloth Physics component to gameobject parent of the renderer
            if (parent.GetComponent<Cloth>() == null)
            {
                cloth = parent.AddComponent<Cloth>();
                // assign values from dtuMat
                cloth.stretchingStiffness = dforceMat.dtuMaterial.Get("Stretch Stiffness").Float;
                cloth.bendingStiffness = dforceMat.dtuMaterial.Get("Bend Stiffness").Float;
                cloth.damping = dforceMat.dtuMaterial.Get("Damping").Float;
                cloth.friction = dforceMat.dtuMaterial.Get("Friction").Float;

                // fix SkinnedMeshRenderer boundaries bug
                skinned.updateWhenOffscreen = true;

                // Add G8F cloth collision rig
                var searchResult = workingInstance.transform.Find("Cloth Collision Rig");
                GameObject collision_instance = (searchResult != null) ? searchResult.gameObject : null;
                if (collision_instance == null)
                {
                    GameObject collision_prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Daz3D/Resources/G8F Collision Rig.prefab");
                    collision_instance = Instantiate<GameObject>(collision_prefab);
                    collision_instance.name = "Cloth Collision Rig";
                    collision_instance.transform.parent = workingInstance.transform;
                    // merge cloth collision rig to figure root bone
                    collision_instance.GetComponent<ClothCollisionAssigner>().mergeRig(skinned.rootBone);
                }
                ClothCollisionAssigner.ClothConfig clothConfig = new ClothCollisionAssigner.ClothConfig();
                clothConfig.m_ClothToManage = cloth;
                clothConfig.m_UpperBody = true;
                clothConfig.m_LowerBody = true;
                collision_instance.GetComponent<ClothCollisionAssigner>().addClothConfig(clothConfig);

            }
            else
            {
                cloth = parent.GetComponent<Cloth>();
            }

            // add clothtools to gameobject parent of renderer
            ClothTools clothTools;
            if (parent.GetComponent<ClothTools>() == null)
            {
                clothTools = parent.AddComponent<ClothTools>();
                clothTools.GenerateLookupTables();
            }
            else
            {
                clothTools = parent.GetComponent<ClothTools>();
            }

            int matIndex = Array.IndexOf(skinned.sharedMaterials, keyMat);
            // get vertex list for this material's submesh
            if (matIndex >= 0)
            {
                float simulation_strength;
                //// map the materical's submesh's vertices to the correct "Dynamics Strength"
                simulation_strength = dforceMat.dtuMaterial.Get("Dynamics Strength").Float;
                Debug.Log("DEBUG INFO: simulation strength: " + simulation_strength);
                //// DEBUG line to map simulation strength to material index
                //simulation_strength = matIndex;

                //// Tiered scaling function
                float adjusted_simulation_strength;
                //float strength_max = 1.0f;
                //float strength_min = 0.0f;
                float strength_scale_threshold = 0.5f;
                if (simulation_strength <= strength_scale_threshold)
                {
                    //// stronger compression of values below threshold
                    float scale = 0.075f;
                    float offset = 0.2f;
                    adjusted_simulation_strength = (simulation_strength - offset) * scale;
                }
                else
                {
                    float offset = (strength_scale_threshold - 0.2f) * 0.075f; // offset = (threshold - previous tier's offset) * previous teir's scale
                    float scale = 0.2f;
                    adjusted_simulation_strength = (simulation_strength - offset) / (1 - offset); // apply offset, then normalize to 1.0
                    adjusted_simulation_strength *= scale;
                }
                //// clamp to 0.0f to 0.2f
                float coeff_min = 0.0f;
                float coeff_max = 0.2f;
                adjusted_simulation_strength = (adjusted_simulation_strength > coeff_min) ? adjusted_simulation_strength : coeff_min;
                adjusted_simulation_strength = (adjusted_simulation_strength < coeff_max) ? adjusted_simulation_strength : coeff_max;
                //// Debug line for no scaling
                //adjusted_simulation_strength = simulation_strength;

                clothTools.SetSubMeshWeights(matIndex, adjusted_simulation_strength);

            }
        }


        private static void DescribeHumanJointsForFigure(ref HumanDescription description, DazFigurePlatform figure)
        {
            var map = GetJointMapForFigure(figure);

            HumanBone[] humanBones = new HumanBone[HumanTrait.BoneName.Length];
            int mapIdx = 0;

            foreach (var humanBoneName in HumanTrait.BoneName)
            {
                if (map.ContainsKey(humanBoneName))
                {
                    HumanBone humanBone = new HumanBone();
                    humanBone.humanName = humanBoneName;
                    humanBone.boneName = map[humanBoneName];
                    humanBone.limit.useDefaultValues = true; //todo get limits from dtu?
                    humanBones[mapIdx++] = humanBone;
                }
            }

            description.human = humanBones;
        }

        private static Dictionary<string, string> GetJointMapForFigure(DazFigurePlatform figure)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            switch (figure)
            {
                case DazFigurePlatform.Genesis8:
                case DazFigurePlatform.Genesis3:
                    ConfigureGenesisMapStandard(map);
                    break;

                case DazFigurePlatform.Genesis2:
                    ConfigureGenesisMapStandard(map);//todo account for Gen2 variances
                    break;

                case DazFigurePlatform.Victoria:
                case DazFigurePlatform.Genesis:
                case DazFigurePlatform.Michael:
                case DazFigurePlatform.TheFreak:
                case DazFigurePlatform.Victoria4:
                case DazFigurePlatform.Victoria4Elite:
                case DazFigurePlatform.Michael4:
                case DazFigurePlatform.Michael4Elite:
                case DazFigurePlatform.Stephanie4:
                case DazFigurePlatform.Aiko4:
                default:
                    //do nothing, let unity's excellent guesser handle it
                    break;

            }

            return map; 
        }

        private static void ConfigureGenesisMapStandard(Dictionary<string, string> map)
        {
            //note: Genesis 3 finger bones have "Carpal#" parents

            //Body/Body (Gen8)
            map["Hips"] = "hip";
            map["Spine"] = "abdomenUpper";
            map["Chest"] = "chestLower";
            map["UpperChest"] = "chestUpper";

            //Body/Left Arm (Gen8)
            map["LeftShoulder"] = "lCollar";
            map["LeftUpperArm"] = "lShldrBend";
            map["LeftLowerArm"] = "lForearmBend";
            map["LeftHand"] = "lHand";

            //Body/Right Arm (Gen8)
            map["RightShoulder"] = "rCollar";
            map["RightUpperArm"] = "rShldrBend";
            map["RightLowerArm"] = "rForearmBend";
            map["RightHand"] = "rHand";

            //Body/Left Leg (Gen8)
            map["LeftUpperLeg"] = "lThighBend";
            map["LeftLowerLeg"] = "lShin";
            map["LeftFoot"] = "lFoot";
            map["LeftToes"] = "lToe";

            //Body/Right Leg (Gen8)
            map["RightUpperLeg"] = "rThighBend";
            map["RightLowerLeg"] = "rShin";
            map["RightFoot"] = "rFoot";
            map["RightToes"] = "rToe";

            //Head (Gen8)
            map["Neck"] = "neckLower";
            map["Head"] = "head";
            map["LeftEye"] = "lEye";
            map["RightEye"] = "rEye";
            map["Jaw"] = "lowerJaw";

            //Left Hand (Gen8)
            map["Left Thumb Proximal"] = "lThumb1";
            map["Left Thumb Intermediate"] = "lThumb2";
            map["Left Thumb Distal"] = "lThumb3";
            map["Left Index Proximal"] = "lIndex1";
            map["Left Index Intermediate"] = "lIndex2";
            map["Left Index Distal"] = "lIndex3";
            map["Left Middle Proximal"] = "lMid1";
            map["Left Middle Intermediate"] = "lMid2";
            map["Left Middle Distal"] = "lMid3";
            map["Left Ring Proximal"] = "lRing1";
            map["Left Ring Intermediate"] = "lRing2";
            map["Left Ring Distal"] = "lRing3";
            map["Left Little Proximal"] = "lPinky1";
            map["Left Little Intermediate"] = "lPinky2";
            map["Left Little Distal"] = "lPinky3";

            //Right Hand (Gen8)
            map["Right Thumb Proximal"] = "rThumb1";
            map["Right Thumb Intermediate"] = "rThumb2";
            map["Right Thumb Distal"] = "rThumb3";
            map["Right Index Proximal"] = "rIndex1";
            map["Right Index Intermediate"] = "rIndex2";
            map["Right Index Distal"] = "rIndex3";
            map["Right Middle Proximal"] = "rMid1";
            map["Right Middle Intermediate"] = "rMid2";
            map["Right Middle Distal"] = "rMid3";
            map["Right Ring Proximal"] = "rRing1";
            map["Right Ring Intermediate"] = "rRing2";
            map["Right Ring Distal"] = "rRing3";
            map["Right Little Proximal"] = "rPinky1";
            map["Right Little Intermediate"] = "rPinky2";
            map["Right Little Distal"] = "rPinky3";
        }

        private void FixupStandardBasedMaterial(ref Material nuMat, GameObject fbxPrefab, string key/*, DTUData data*/)
        {
            ////todo need fixup missing textures from the json
            //Debug.LogWarning("dtuData has " + data.Materials.Count + " materials ");

            //var modelPath = AssetDatabase.GetAssetPath(fbxPrefab);
            //var nuTexturePath = Path.GetDirectoryName(modelPath);
            //nuTexturePath = BuildUnityPath(nuTexturePath, fbxPrefab.name + "Textures___");
            //nuTexturePath = AssetDatabase.GenerateUniqueAssetPath(nuTexturePath);

            ////walk data until find a material named with key
            //foreach (var material in data.Materials)
            //{
            //    if (material.MaterialName == key && false) //TODO hack to bypass unfinished fn
            //    {
            //        //walk properties and work on any with a texture path
            //        foreach (var property in material.Properties)
            //        {
            //            if (!string.IsNullOrEmpty(property.Texture))
            //            {
            //                //and the daz folder has that texture 
            //                if (File.Exists(property.Texture))
            //                {
            //                    //copy it into the local textures folder
            //                    if (!Directory.Exists(nuTexturePath))
            //                        Directory.CreateDirectory(nuTexturePath);

            //                    var nuTextureName = BuildUnityPath(nuTexturePath, Path.GetFileName(property.Texture));

            //                    //TODO-----------------------------
            //                    //todo some diffuse maps are jpg with no alpha channel, 
            //                    //instead use the FBX's embedded/collected texture which already has alpha channel, 
            //                    //test whether that material already has a valid diffuse color texture
            //                    //if so, reimport that with the importer options below

            //                    //copy the texture file from the daz folder to nuTexturePath
            //                    File.Copy(property.Texture, nuTextureName);

            //                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(nuTextureName);
            //                    if (importer != null)
            //                    {
            //                        //todo twiddle other switches here, before the reimport happens only once
            //                        importer.alphaIsTransparency = KeyToTransparency(key);
            //                        importer.alphaSource = KeyToAlphaSource(key);
            //                        importer.convertToNormalmap = KeyToNormalMap(key);
            //                        importer.heightmapScale = KeyToHeightmapScale(key);
            //                        importer.normalmapFilter = KeyToNormalMapFilter(key);
            //                        importer.wrapMode = KeyToWrapMode(key);

            //                        importer.SaveAndReimport();
            //                    }
            //                    else
            //                    {
            //                        Debug.LogWarning("texture " + nuTextureName + " is not a project asset.");
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }

        private TextureImporterAlphaSource KeyToAlphaSource(string key)
        {
            throw new NotImplementedException();
        }

        private TextureWrapMode KeyToWrapMode(string key)
        {
            throw new NotImplementedException();
        }

        private TextureImporterNormalFilter KeyToNormalMapFilter(string key)
        {
            throw new NotImplementedException();
        }

        private float KeyToHeightmapScale(string key)
        {
            throw new NotImplementedException();
        }

        private bool KeyToNormalMap(string key)
        {
            throw new NotImplementedException();
        }

        private bool KeyToTransparency(string key)
        {
            throw new NotImplementedException();
        }

        //private void CustomizeMaterial(ref Material material, DazMaterialPropertiesInfo info)
        //{
        //    material.SetColor("_BaseColor", info.BaseColor);
        //    material.SetFloat("_SurfaceType", info.Transparent ? 1 : 0); //0 == opaque, 1 == transparent

        //    Texture mainTexture = material.mainTexture;
        //    CustomizeTexture(ref mainTexture, info.Transparent);

        //    var normalMap = material.GetTexture("_NormalMap");
        //    if (!IsValidNormalMap(normalMap))
        //        material.SetTexture("_NormalMap", null);//nuke the invalid normal map, if its a mistake


        //    material.SetFloat("_Metallic", info.Metallic);
        //    material.SetFloat("_Smoothness", info.Smoothness);
        //    material.SetInt("_MaterialID", (int)info.MaterialType);
        //    material.SetFloat("_DoubleSidedEnable", info.DoubleSided ? 0 : 1);
        //}


        void CustomizeTexture(ref Texture texture, bool alphaIsTransparent)
        {
            if (texture != null)
            {
                var texPath = AssetDatabase.GetAssetPath(texture);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
                if (importer != null)
                {
                    if (alphaIsTransparent && importer.DoesSourceTextureHaveAlpha())
                    {
                        importer.alphaIsTransparency = true;
                    }

                    //todo twiddle other switches here, before the reimport happens only once
                    importer.SaveAndReimport();
                }
                else
                    Debug.LogWarning("texture " + texture.name + " is not a project asset.");

            }
            else
                Debug.LogWarning("null texture");
        }


        bool IsValidNormalMap(Texture normalMap)
        {
            if (normalMap == null)
                return false;

            var nmPath = AssetDatabase.GetAssetPath(normalMap);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(nmPath);
            if (importer != null)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                return settings.textureType == TextureImporterType.NormalMap;
            }
            else
                Debug.LogWarning("texture " + normalMap.name + " is not a project asset.");

            return true;
        }



        // Validated menu item.
        // Add a menu item named "Log Selected Transform Name" to MyMenu in the menu bar.
        // We use a second function to validate the menu item
        // so it will only be enabled if we have a transform selected.
        [MenuItem("Assets/Daz3D/Create Unity Prefab", false, 101)]
        static void DoStuffToSelectedDTU()
        {
            CreateDTUPrefab(Selection.activeObject);
            if (Selection.activeTransform)
                Debug.Log("Selected Transform is on " + Selection.activeTransform.gameObject.name + ".");
        }

        //// Validate the menu item defined by the function above.
        //// The menu item will be disabled if this function returns false.
        //[MenuItem("Assets/Daz3D/Create Unity Prefab", true)]
        //static bool ValidateDTUSelected2()
        //{
        //    return ValidateDTUSelected();
        //}

        private static void CreateDTUPrefab(UnityEngine.Object activeObject)
        {
            if (activeObject)
            {
                var dtuPath = AssetDatabase.GetAssetPath(activeObject);
                var fbxPath = dtuPath.Replace(".dtu", ".fbx");

                Import(dtuPath, fbxPath);
            }
        }


    }

}