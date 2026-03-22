using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoonBase.Modules;

namespace MoonBase.Core
{
    /// <summary>
    /// Save/load system for LUNAR OPS base state.
    ///
    /// Persists:
    ///   - Base name and mission metadata
    ///   - All placed module positions, rotations, and types
    ///   - Resource levels: battery charge, water stored
    ///   - Mission Elapsed Time (MET)
    ///
    /// Storage: JSON files at Application.persistentDataPath/saves/{slotName}.json
    /// Auto-save: every 5 real minutes (configurable).
    ///
    /// Usage:
    ///   SaveSystem.Instance.SaveGame("slot1");
    ///   SaveSystem.Instance.LoadGame("slot1");
    ///   string[] slots = SaveSystem.Instance.GetSaveSlots();
    ///
    /// Setup: Attach to a persistent GameObject.
    ///        Assign moduleLibrary (array of all ModuleDefinition ScriptableObjects)
    ///        so LoadGame can look up prefabs by moduleTypeId.
    ///        Assign modulesParent (the PlacedModulesRoot transform).
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static SaveSystem Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Module Library")]
        [Tooltip("All ModuleDefinition ScriptableObjects in the project. " +
                 "Used to look up prefabs by moduleTypeId during load.")]
        public ModuleDefinition[] moduleLibrary;

        [Tooltip("The transform that is parent to all placed modules in the scene.")]
        public Transform modulesParent;

        [Header("Auto-Save")]
        [Tooltip("Auto-save interval in real seconds. 0 = disabled.")]
        public float autoSaveIntervalSeconds = 300f; // 5 minutes

        [Tooltip("Slot name used for auto-save.")]
        public string autoSaveSlotName = "autosave";

        [Header("Base Info")]
        [Tooltip("Display name for this player's base.")]
        public string baseName = "Shackleton Base Alpha";

        // ── Runtime State ──────────────────────────────────────────────────────
        /// <summary>True while a save or load operation is in progress.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>Slot name of the most recently loaded or saved game.</summary>
        public string LastActiveSlot { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired after a successful save. Arg = slot name.</summary>
        public event Action<string> OnSaveComplete;

        /// <summary>Fired after a successful load. Arg = slot name.</summary>
        public event Action<string> OnLoadComplete;

        /// <summary>Fired if save or load fails. Arg = error message.</summary>
        public event Action<string> OnSaveLoadError;

        // ── Private ────────────────────────────────────────────────────────────
        private string savesDirectory;
        private float autoSaveTimer;
        private Dictionary<string, ModuleDefinition> moduleLibraryLookup;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
            Directory.CreateDirectory(savesDirectory);

            BuildLibraryLookup();
        }

        private void Start()
        {
            autoSaveTimer = autoSaveIntervalSeconds;
            Debug.Log($"[SaveSystem] Saves directory: {savesDirectory}");
        }

        private void Update()
        {
            if (autoSaveIntervalSeconds <= 0f || IsBusy) return;

            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer <= 0f)
            {
                autoSaveTimer = autoSaveIntervalSeconds;
                SaveGame(autoSaveSlotName);
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Saves the current game state to the specified slot.
        /// File: {persistentDataPath}/saves/{slotName}.json
        /// </summary>
        public void SaveGame(string slotName)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[SaveSystem] Save requested while busy. Ignored.");
                return;
            }
            StartCoroutine(SaveCoroutine(slotName));
        }

        /// <summary>
        /// Loads a saved game from the specified slot.
        /// Destroys all currently placed modules before restoring.
        /// </summary>
        public void LoadGame(string slotName)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[SaveSystem] Load requested while busy. Ignored.");
                return;
            }
            StartCoroutine(LoadCoroutine(slotName));
        }

        /// <summary>
        /// Returns an array of available save slot names (no extension).
        /// Sorted by last write time descending (most recent first).
        /// </summary>
        public string[] GetSaveSlots()
        {
            if (!Directory.Exists(savesDirectory)) return Array.Empty<string>();

            var files = Directory.GetFiles(savesDirectory, "*.json");
            Array.Sort(files, (a, b) =>
                File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

            var slots = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                slots[i] = Path.GetFileNameWithoutExtension(files[i]);

            return slots;
        }

        /// <summary>
        /// Returns true if a save slot with the given name exists.
        /// </summary>
        public bool SlotExists(string slotName)
        {
            return File.Exists(GetSavePath(slotName));
        }

        /// <summary>
        /// Deletes a save slot file. No-op if it doesn't exist.
        /// </summary>
        public void DeleteSlot(string slotName)
        {
            string path = GetSavePath(slotName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveSystem] Deleted save: {slotName}");
            }
        }

        /// <summary>
        /// Reads save metadata (base name, MET, module count) without fully loading.
        /// Useful for displaying a save slot preview in a menu.
        /// Returns null if the slot doesn't exist or is corrupt.
        /// </summary>
        public SaveMetadata ReadSlotMetadata(string slotName)
        {
            string path = GetSavePath(slotName);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                return new SaveMetadata
                {
                    slotName     = slotName,
                    baseName     = data.baseName,
                    metSeconds   = data.metSeconds,
                    moduleCount  = data.modules?.Count ?? 0,
                    savedAtUTC   = File.GetLastWriteTimeUtc(path).ToString("u")
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to read metadata for '{slotName}': {e.Message}");
                return null;
            }
        }

        // ── Save Coroutine ─────────────────────────────────────────────────────
        private IEnumerator SaveCoroutine(string slotName)
        {
            IsBusy = true;
            yield return null; // let frame complete before collecting state

            try
            {
                var data = CollectSaveData(slotName);
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(GetSavePath(slotName), json);
                LastActiveSlot = slotName;
                Debug.Log($"[SaveSystem] Saved '{slotName}' " +
                          $"({data.modules.Count} modules, MET {data.metSeconds:F0}s)");
                OnSaveComplete?.Invoke(slotName);
            }
            catch (Exception e)
            {
                string msg = $"Save failed: {e.Message}";
                Debug.LogError($"[SaveSystem] {msg}");
                OnSaveLoadError?.Invoke(msg);
            }

            IsBusy = false;
        }

        // ── Load Coroutine ─────────────────────────────────────────────────────
        private IEnumerator LoadCoroutine(string slotName)
        {
            IsBusy = true;

            string path = GetSavePath(slotName);
            if (!File.Exists(path))
            {
                string msg = $"Save file not found: {slotName}";
                Debug.LogError($"[SaveSystem] {msg}");
                OnSaveLoadError?.Invoke(msg);
                IsBusy = false;
                yield break;
            }

            SaveData data = null;
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                string msg = $"Load failed (JSON parse error): {e.Message}";
                Debug.LogError($"[SaveSystem] {msg}");
                OnSaveLoadError?.Invoke(msg);
                IsBusy = false;
                yield break;
            }

            yield return null; // allow any cleanup to run

            // Destroy all existing modules
            ClearAllModules();

            yield return null; // allow destroy to process

            // Restore simulation state
            RestoreSimState(data);

            // Restore modules
            yield return StartCoroutine(RestoreModulesCoroutine(data));

            baseName       = data.baseName;
            LastActiveSlot = slotName;
            Debug.Log($"[SaveSystem] Loaded '{slotName}' ({data.modules.Count} modules).");
            OnLoadComplete?.Invoke(slotName);

            IsBusy = false;
        }

        // ── Data Collection ────────────────────────────────────────────────────
        private SaveData CollectSaveData(string slotName)
        {
            var data = new SaveData
            {
                slotName    = slotName,
                baseName    = baseName,
                metSeconds  = LunarSimulationClock.Instance?.METSeconds ?? 0.0,
                modules     = new List<ModuleSaveEntry>()
            };

            if (ResourceSimulator.Instance != null)
            {
                var snap = ResourceSimulator.Instance.GetSnapshot();
                data.batteryStoredKWh = snap.powerStoredKWh;
                data.waterStoredLiters = snap.waterStoredLiters;
            }

            if (MoonBaseManager.Instance != null)
            {
                foreach (var module in MoonBaseManager.Instance.PlacedModules)
                {
                    if (module == null || module.ModuleDefinition == null) continue;
                    var pos = module.transform.position;
                    var rot = module.transform.rotation;
                    data.modules.Add(new ModuleSaveEntry
                    {
                        moduleTypeId = module.ModuleDefinition.moduleTypeId,
                        posX = pos.x, posY = pos.y, posZ = pos.z,
                        rotX = rot.x, rotY = rot.y, rotZ = rot.z, rotW = rot.w
                    });
                }
            }

            return data;
        }

        // ── State Restoration ──────────────────────────────────────────────────
        private void RestoreSimState(SaveData data)
        {
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.SetMET(data.metSeconds);

            if (ResourceSimulator.Instance != null)
            {
                ResourceSimulator.Instance.SetBatteryCharge(data.batteryStoredKWh);
                ResourceSimulator.Instance.SetWaterStored(data.waterStoredLiters);
            }
        }

        private IEnumerator RestoreModulesCoroutine(SaveData data)
        {
            foreach (var entry in data.modules)
            {
                if (!moduleLibraryLookup.TryGetValue(entry.moduleTypeId, out var definition))
                {
                    Debug.LogWarning($"[SaveSystem] Unknown moduleTypeId '{entry.moduleTypeId}' — skipped.");
                    continue;
                }

                if (definition.prefab == null)
                {
                    Debug.LogWarning($"[SaveSystem] ModuleDefinition '{entry.moduleTypeId}' has no prefab — skipped.");
                    continue;
                }

                Vector3    pos = new Vector3(entry.posX, entry.posY, entry.posZ);
                Quaternion rot = new Quaternion(entry.rotX, entry.rotY, entry.rotZ, entry.rotW);

                var go = Instantiate(definition.prefab, pos, rot,
                                     modulesParent != null ? modulesParent : null);
                var baseModule = go.GetComponent<BaseModule>();
                if (baseModule != null)
                    baseModule.InitializePlacement(definition, pos, rot * Vector3.up);

                yield return null; // spread instantiation over frames
            }
        }

        private void ClearAllModules()
        {
            if (modulesParent == null) return;
            for (int i = modulesParent.childCount - 1; i >= 0; i--)
                Destroy(modulesParent.GetChild(i).gameObject);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private string GetSavePath(string slotName)
        {
            // Sanitize slot name to avoid path traversal
            string safe = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(savesDirectory, safe + ".json");
        }

        private void BuildLibraryLookup()
        {
            moduleLibraryLookup = new Dictionary<string, ModuleDefinition>(StringComparer.Ordinal);
            if (moduleLibrary == null) return;

            foreach (var def in moduleLibrary)
            {
                if (def == null || string.IsNullOrEmpty(def.moduleTypeId)) continue;
                if (!moduleLibraryLookup.TryAdd(def.moduleTypeId, def))
                    Debug.LogWarning($"[SaveSystem] Duplicate moduleTypeId: '{def.moduleTypeId}'");
            }
        }

        // ── Serializable Data Structures ───────────────────────────────────────
        [Serializable]
        private class SaveData
        {
            public string slotName;
            public string baseName;
            public double metSeconds;
            public float  batteryStoredKWh;
            public float  waterStoredLiters;
            public List<ModuleSaveEntry> modules = new();
        }

        [Serializable]
        private class ModuleSaveEntry
        {
            public string moduleTypeId;
            public float posX, posY, posZ;
            public float rotX, rotY, rotZ, rotW;
        }
    }

    // ── Public Metadata DTO ────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight save slot preview data, available without fully loading the save.
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public string slotName;
        public string baseName;
        public double metSeconds;
        public int    moduleCount;
        public string savedAtUTC;
    }
}
