#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MoonBase.Modules;

namespace MoonBase.Editor
{
    /// <summary>
    /// Creates placeholder prefabs and ModuleDefinition ScriptableObjects for the
    /// three starter module types: SolarArray, Habitat, MiningRig.
    ///
    /// Run via: MoonBase > Create Default Module Assets
    /// 
    /// Each module is a primitive cube with the correct script components.
    /// Replace the geometry with proper art assets later.
    /// </summary>
    public static class ModuleAssetBootstrapper
    {
        private const string PrefabDir   = "Assets/Prefabs/Modules";
        private const string DefsDir     = "Assets/ScriptableObjects/Modules";
        private const string MaterialDir = "Assets/Materials/Modules";

        [MenuItem("MoonBase/Create Default Module Assets")]
        public static void CreateAllModuleAssets()
        {
            EnsureFolders();
            CreateGhostMaterials();
            CreateSolarArrayAssets();
            CreateHabitatAssets();
            CreateMiningRigAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ModuleBootstrap] All starter module assets created. " +
                      "Check Assets/Prefabs/Modules and Assets/ScriptableObjects/Modules.");
        }

        // ── Folder Scaffolding ─────────────────────────────────────────────────
        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Modules");
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder("Assets/ScriptableObjects/Modules");
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Materials/Modules");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int lastSlash = path.LastIndexOf('/');
                string parent = path.Substring(0, lastSlash);
                string folder = path.Substring(lastSlash + 1);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        // ── Ghost Materials ────────────────────────────────────────────────────
        private static void CreateGhostMaterials()
        {
            CreateGhostMaterial("GhostValid",   new Color(0.2f, 1f, 0.2f, 0.4f));
            CreateGhostMaterial("GhostInvalid", new Color(1f, 0.15f, 0.1f, 0.4f));
        }

        private static Material CreateGhostMaterial(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetFloat("_Surface", 1); // transparent
            mat.SetFloat("_Blend", 0);   // alpha
            mat.color = color;
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ── Solar Array ────────────────────────────────────────────────────────
        private static void CreateSolarArrayAssets()
        {
            // ScriptableObject
            var def = CreateOrLoad<ModuleDefinition>($"{DefsDir}/SolarArray_Def.asset");
            def.moduleTypeId             = "module_solar_array_01";
            def.moduleName               = "Solar Array";
            def.description              = "High-efficiency photovoltaic panels. Generates up to 10 kW at full sun exposure. Deploy on high ridges for maximum illumination.";
            def.footprintMeters          = new Vector2(12f, 8f);
            def.heightMeters             = 3f;
            def.powerGenerationKW        = 10f;
            def.powerConsumptionKW       = 0f;
            def.crewCapacity             = 0;
            def.category                 = ModuleCategory.Power;
            def.prefersSunlight          = true;
            EditorUtility.SetDirty(def);

            // Prefab
            string prefabPath = $"{PrefabDir}/SolarArray.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var root = BuildModulePrimitive("SolarArray",
                    new Color(0.1f, 0.3f, 0.7f),   // dark blue-grey (panel color)
                    new Vector3(12f, 0.5f, 8f));     // flat panel shape

                // Add scripts
                root.AddComponent<SolarArrayModule>().peakOutputKW = 10f;
                root.AddComponent<BaseModule>();

                // Save prefab
                bool success;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
                Object.DestroyImmediate(root);
                if (success) Debug.Log("[ModuleBootstrap] Created SolarArray prefab.");
            }

            // Assign prefab to def
            def.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(def);
        }

        // ── Habitat ────────────────────────────────────────────────────────────
        private static void CreateHabitatAssets()
        {
            var def = CreateOrLoad<ModuleDefinition>($"{DefsDir}/Habitat_Def.asset");
            def.moduleTypeId             = "module_habitat_01";
            def.moduleName               = "Habitat Module";
            def.description              = "Pressurized living quarters for 4 crew. Includes life support, sleeping quarters, and workspace. Critical power consumer.";
            def.footprintMeters          = new Vector2(10f, 10f);
            def.heightMeters             = 5f;
            def.powerGenerationKW        = 0f;
            def.powerConsumptionKW       = 8f;
            def.crewCapacity             = 4;
            def.category                 = ModuleCategory.Habitat;
            EditorUtility.SetDirty(def);

            string prefabPath = $"{PrefabDir}/Habitat.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var root = BuildModulePrimitive("Habitat",
                    new Color(0.6f, 0.55f, 0.45f),  // tan/sand color
                    new Vector3(10f, 5f, 10f));

                var habitat = root.AddComponent<HabitatModule>();
                habitat.basePowerDrawKW = 4f;
                habitat.powerPerCrewKW  = 1f;
                habitat.crewCount       = 4;
                root.AddComponent<BaseModule>();

                bool success;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
                Object.DestroyImmediate(root);
                if (success) Debug.Log("[ModuleBootstrap] Created Habitat prefab.");
            }

            def.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(def);
        }

        // ── Mining Rig ─────────────────────────────────────────────────────────
        private static void CreateMiningRigAssets()
        {
            var def = CreateOrLoad<ModuleDefinition>($"{DefsDir}/MiningRig_Def.asset");
            def.moduleTypeId                 = "module_mining_rig_01";
            def.moduleName                   = "ISRU Mining Rig";
            def.description                  = "In-Situ Resource Utilization drill and electrolysis unit. Extracts water ice from permanently shadowed regolith. Deploy in PSRs for best yield.";
            def.footprintMeters              = new Vector2(8f, 8f);
            def.heightMeters                 = 6f;
            def.powerGenerationKW            = 0f;
            def.powerConsumptionKW           = 8f;
            def.waterExtractionLitersPerDay  = 200f;
            def.category                     = ModuleCategory.Mining;
            def.prefersPermanentShadow       = true;
            EditorUtility.SetDirty(def);

            string prefabPath = $"{PrefabDir}/MiningRig.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var root = BuildModulePrimitive("MiningRig",
                    new Color(0.4f, 0.35f, 0.3f),   // dark grey-brown (industrial)
                    new Vector3(8f, 6f, 8f));

                var mining = root.AddComponent<MiningRigModule>();
                mining.powerConsumptionKW  = 8f;
                mining.extractionRateLPerDay = 200f;
                root.AddComponent<BaseModule>();

                bool success;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
                Object.DestroyImmediate(root);
                if (success) Debug.Log("[ModuleBootstrap] Created MiningRig prefab.");
            }

            def.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(def);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        /// <summary>
        /// Builds a simple cube GameObject representing a module placeholder.
        /// The root has a BoxCollider sized to the module; the visual cube is a child.
        /// </summary>
        private static GameObject BuildModulePrimitive(string name, Color color, Vector3 size)
        {
            // Root (no mesh — just collider + scripts)
            var root = new GameObject(name);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, size.y * 0.5f, 0f);

            // Visual child cube
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            visual.transform.localScale    = size;

            // Remove the auto-added collider from the visual (root has it)
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Apply color via material
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                // slight metallic look
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Smoothness", 0.4f);
                renderer.sharedMaterial = mat;
            }

            return root;
        }

        private static T CreateOrLoad<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
#endif
