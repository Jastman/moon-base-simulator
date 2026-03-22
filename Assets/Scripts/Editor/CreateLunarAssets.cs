using UnityEngine;
using UnityEditor;
using System.IO;

namespace MoonBase.Editor
{
    public class CreateLunarAssets
    {
        [MenuItem("MoonBase/Create Lunar Assets")]
        public static void CreateAssets()
        {
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder("Assets/ScriptableObjects/Modules");

            // ── Materials ────────────────────────────────────────────────────────
            var habitatMat   = CreateMaterial("HabitatMat",   "#8ab4d4");
            var solarMat     = CreateMaterial("SolarMat",     "#ffb347");
            var batteryMat   = CreateMaterial("BatteryMat",   "#39ff6a");
            var greenhouseMat= CreateMaterial("GreenhouseMat","#4aff8c");
            var airlockMat   = CreateMaterial("AirlockMat",   "#c8d8e8");

            // ── Prefabs ──────────────────────────────────────────────────────────
            // Habitat: Cylinder scaled (2,1.5,2)
            CreateModulePrefab("Habitat",    PrimitiveType.Cylinder, new Vector3(2f,1.5f,2f),   habitatMat,
                power: -12f, crew: 4, mass: 8500f,
                description: "Pressurized crew habitat module");

            // SolarArray: Cube scaled (4,0.1,3)
            CreateModulePrefab("SolarArray", PrimitiveType.Cube,     new Vector3(4f,0.1f,3f),   solarMat,
                power: 48f,  crew: 0, mass: 320f,
                description: "Deployable photovoltaic array");

            // Battery: Cube scaled (1.5,1.5,1.5)
            CreateModulePrefab("Battery",    PrimitiveType.Cube,     new Vector3(1.5f,1.5f,1.5f), batteryMat,
                power: 0f,   crew: 0, mass: 1200f,
                description: "High-density lithium storage unit",
                storageCapacity: 240f);

            // Greenhouse: Sphere scaled (2,2,2)
            CreateModulePrefab("Greenhouse", PrimitiveType.Sphere,   new Vector3(2f,2f,2f),     greenhouseMat,
                power: -8f,  crew: 1, mass: 3400f,
                description: "Hydroponic food production dome");

            // Airlock: Cylinder scaled (1,1,1)
            CreateModulePrefab("Airlock",    PrimitiveType.Cylinder, new Vector3(1f,1f,1f),     airlockMat,
                power: -2f,  crew: 0, mass: 950f,
                description: "EVA ingress/egress airlock");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateLunarAssets] All lunar assets created successfully.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static Material CreateMaterial(string name, string hexColor)
        {
            var mat = new Material(Shader.Find("Standard"));
            if (ColorUtility.TryParseHtmlString(hexColor, out Color c))
                mat.color = c;
            string path = $"Assets/Materials/{name}.mat";
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"[CreateLunarAssets] Material created: {path}");
            return mat;
        }

        private static void CreateModulePrefab(
            string moduleName,
            PrimitiveType primitive,
            Vector3 scale,
            Material mat,
            float power,
            int crew,
            float mass,
            string description,
            float storageCapacity = 0f)
        {
            // Create GO with primitive mesh
            var go = GameObject.CreatePrimitive(primitive);
            go.name = moduleName;
            go.transform.localScale = scale;

            // Apply material
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;

            // Attach BaseModule if available
            var baseModuleType = System.Type.GetType("MoonBase.BaseModule, Assembly-CSharp");
            if (baseModuleType != null)
            {
                var comp = go.AddComponent(baseModuleType);
                var so = new SerializedObject(comp);
                SetSerializedField(so, "_moduleName",   moduleName);
                SetSerializedField(so, "_description",  description);
                SetSerializedField(so, "_powerDelta",   power);
                SetSerializedField(so, "_crewCapacity", crew);
                SetSerializedField(so, "_mass",         mass);
                if (storageCapacity > 0f)
                    SetSerializedField(so, "_storageCapacity", storageCapacity);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[CreateLunarAssets] BaseModule not found — add component manually to {moduleName} prefab.");
            }

            // Save prefab
            string prefabPath = $"Assets/Prefabs/{moduleName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[CreateLunarAssets] Prefab created: {prefabPath}");

            // Load the saved prefab for the ScriptableObject reference
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            CreateModuleDefinition(moduleName, power, crew, mass, description, prefabAsset, storageCapacity);
        }

        private static void CreateModuleDefinition(
            string moduleName,
            float power,
            int crew,
            float mass,
            string description,
            GameObject prefab,
            float storageCapacity)
        {
            var defType = System.Type.GetType("MoonBase.ModuleDefinition, Assembly-CSharp");
            if (defType == null)
            {
                Debug.LogWarning($"[CreateLunarAssets] ModuleDefinition ScriptableObject type not found — skipping SO for {moduleName}.");
                return;
            }

            var so = ScriptableObject.CreateInstance(defType);
            so.name = moduleName;
            var serialized = new SerializedObject(so);
            SetSerializedField(serialized, "_moduleName",       moduleName);
            SetSerializedField(serialized, "_description",      description);
            SetSerializedField(serialized, "_powerDelta",       power);
            SetSerializedField(serialized, "_crewCapacity",     crew);
            SetSerializedField(serialized, "_mass",             mass);
            if (storageCapacity > 0f)
                SetSerializedField(serialized, "_storageCapacity", storageCapacity);
            // Assign prefab reference
            var prefabProp = serialized.FindProperty("_prefab");
            if (prefabProp != null) prefabProp.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string soPath = $"Assets/ScriptableObjects/Modules/{moduleName}Definition.asset";
            AssetDatabase.CreateAsset(so, soPath);
            Debug.Log($"[CreateLunarAssets] ModuleDefinition SO created: {soPath}");
        }

        private static void SetSerializedField(SerializedObject so, string fieldName, object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            switch (value)
            {
                case string s:   prop.stringValue  = s;            break;
                case float f:    prop.floatValue   = f;            break;
                case int i:      prop.intValue     = i;            break;
                case bool b:     prop.boolValue    = b;            break;
                case long l:     prop.longValue    = l;            break;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = next;
                }
            }
        }
    }
}
