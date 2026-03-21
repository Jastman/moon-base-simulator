// This file is in the Editor folder — it won't be included in builds.
// It creates the 5 default ModuleDefinition ScriptableObject assets for you
// so you don't have to set them up by hand.
//
// How to run: Unity menu → MoonBase → Create Default Module Assets

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MoonBase.Modules;

namespace MoonBase.Editor
{
    public static class ModuleAssetBootstrapper
    {
        private const string OutputPath = "Assets/ScriptableObjects/ModuleData";

        [MenuItem("MoonBase/Create Default Module Assets")]
        public static void CreateDefaultModules()
        {
            // Make sure the output directory exists
            if (!AssetDatabase.IsValidFolder(OutputPath))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "ModuleData");
                Debug.Log($"[Bootstrapper] Created folder: {OutputPath}");
            }

            CreateModule(new ModuleDefinition
            {
                moduleTypeId  = "module_habitat_01",
                moduleName    = "Habitat Module",
                description   = "Pressurized living quarters. Houses 4 crew in cramped but functional quarters. " +
                                "Requires connection to power and life support systems.",
                footprintMeters         = new Vector2(12f, 8f),
                heightMeters            = 5f,
                snapGapMeters           = 0.5f,
                powerConsumptionKW      = 8f,
                powerGenerationKW       = 0f,
                crewCapacity            = 4,
                oxygenProductionKgPerDay = 0f,
                waterExtractionLitersPerDay = 0f,
                maxSlopeOverrideDegrees = 15f,
                prefersPermanentShadow  = false,
                prefersSunlight         = false,
                category                = ModuleCategory.Habitat
            }, "Habitat");

            CreateModule(new ModuleDefinition
            {
                moduleTypeId  = "module_solar_array_01",
                moduleName    = "Solar Panel Array",
                description   = "Foldable photovoltaic array. Best placed on ridges with high solar exposure. " +
                                "Output drops to zero in permanently shadowed regions.",
                footprintMeters         = new Vector2(20f, 8f),
                heightMeters            = 2f,
                snapGapMeters           = 1f,
                powerConsumptionKW      = 0.5f,   // Control systems
                powerGenerationKW       = 25f,
                crewCapacity            = 0,
                maxSlopeOverrideDegrees = 25f,    // Can be placed on moderate slopes
                prefersPermanentShadow  = false,
                prefersSunlight         = true,
                category                = ModuleCategory.Power
            }, "SolarArray");

            CreateModule(new ModuleDefinition
            {
                moduleTypeId  = "module_power_storage_01",
                moduleName    = "Power Storage Unit",
                description   = "Battery bank for storing excess solar power during lunar day. " +
                                "Critical for survival through the 14-day lunar night.",
                footprintMeters         = new Vector2(8f, 8f),
                heightMeters            = 3f,
                snapGapMeters           = 0.5f,
                powerConsumptionKW      = 1f,      // Thermal management
                powerGenerationKW       = 0f,
                crewCapacity            = 0,
                maxSlopeOverrideDegrees = 20f,
                prefersPermanentShadow  = false,
                prefersSunlight         = false,
                category                = ModuleCategory.Power
            }, "PowerStorage");

            CreateModule(new ModuleDefinition
            {
                moduleTypeId  = "module_ice_drill_01",
                moduleName    = "Ice Mining Drill",
                description   = "Extracts water ice from permanently shadowed regolith. " +
                                "Must be placed in a Permanently Shadowed Region (PSR) to be effective. " +
                                "High power draw requires dedicated solar infrastructure.",
                footprintMeters         = new Vector2(6f, 6f),
                heightMeters            = 8f,     // Tall drill tower
                snapGapMeters           = 2f,
                powerConsumptionKW      = 15f,
                powerGenerationKW       = 0f,
                crewCapacity            = 0,
                waterExtractionLitersPerDay = 200f,
                maxSlopeOverrideDegrees = 10f,    // Needs flat ground for drill stability
                prefersPermanentShadow  = true,
                prefersSunlight         = false,
                category                = ModuleCategory.Mining
            }, "IceDrill");

            CreateModule(new ModuleDefinition
            {
                moduleTypeId  = "module_airlock_01",
                moduleName    = "Airlock",
                description   = "Pressurized transition chamber between hab modules and the lunar surface. " +
                                "Every base needs at least one. Connect between habitat and exterior.",
                footprintMeters         = new Vector2(4f, 4f),
                heightMeters            = 3.5f,
                snapGapMeters           = 0.25f,
                powerConsumptionKW      = 1f,
                powerGenerationKW       = 0f,
                crewCapacity            = 0,       // Crew pass through but don't live here
                maxSlopeOverrideDegrees = 15f,
                prefersPermanentShadow  = false,
                prefersSunlight         = false,
                category                = ModuleCategory.Infrastructure
            }, "Airlock");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Bootstrapper] Created 5 default module assets in " + OutputPath);
            EditorUtility.DisplayDialog("Module Assets Created",
                "Created 5 default ModuleDefinition assets in:\n" + OutputPath +
                "\n\nAssign them to the ModuleSelectorUI.availableModules list in the Inspector.",
                "OK");
        }

        private static void CreateModule(ModuleDefinition data, string filename)
        {
            string path = $"{OutputPath}/{filename}.asset";

            // Don't overwrite if it already exists
            if (AssetDatabase.LoadAssetAtPath<ModuleDefinition>(path) != null)
            {
                Debug.Log($"[Bootstrapper] Skipped (already exists): {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<ModuleDefinition>();

            // Copy all public fields from data to asset
            asset.moduleTypeId              = data.moduleTypeId;
            asset.moduleName                = data.moduleName;
            asset.description               = data.description;
            asset.footprintMeters           = data.footprintMeters;
            asset.heightMeters              = data.heightMeters;
            asset.snapGapMeters             = data.snapGapMeters;
            asset.powerConsumptionKW        = data.powerConsumptionKW;
            asset.powerGenerationKW         = data.powerGenerationKW;
            asset.crewCapacity              = data.crewCapacity;
            asset.oxygenProductionKgPerDay  = data.oxygenProductionKgPerDay;
            asset.waterExtractionLitersPerDay = data.waterExtractionLitersPerDay;
            asset.maxSlopeOverrideDegrees   = data.maxSlopeOverrideDegrees;
            asset.prefersPermanentShadow    = data.prefersPermanentShadow;
            asset.prefersSunlight           = data.prefersSunlight;
            asset.category                  = data.category;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[Bootstrapper] Created: {path}");
        }
    }
}
#endif
