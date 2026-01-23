using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using My.Scripts.Gameplay.KeyDoor;

[InitializeOnLoad]
public static class PrefabAutoNamer
{
    #region Configuration

    private static readonly string[] SIMPLE_PREFABS =
    {
        "CoinPickup",
        "FuelPickup",
        "CratePickup",
        "LandingPad",
        "CrateLandingPad",
        "Terrain",
        "CaveZone",
        "AsteroidSpawner",
        "Turret"
    };

    private static readonly string[] TYPED_PREFAB_BASES =
    {
        "Key",
        "KeyDeliver",
        "Door"
    };

    private static readonly string[] TYPED_PREFAB_FULL_NAMES =
    {
        "KeyRed", "KeyGreen", "KeyBlue",
        "KeyDeliverRed", "KeyDeliverGreen", "KeyDeliverBlue",
        "DoorRed", "DoorGreen", "DoorBlue"
    };

    #endregion

    #region Private Fields

    // Корневой объект текущего редактируемого префаба (для исключения)
    private static GameObject _currentPrefabRoot;

    #endregion

    #region Initialization

    static PrefabAutoNamer()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        PrefabStage.prefabSaving += OnPrefabSaving;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        Undo.postprocessModifications += OnPropertyModified;
    }

    #endregion

    #region Event Handlers

    private static void OnPrefabStageOpened(PrefabStage prefabStage)
    {
        _currentPrefabRoot = prefabStage.prefabContentsRoot;
        EditorApplication.delayCall += DelayedRename;
    }

    private static void OnPrefabStageClosing(PrefabStage prefabStage)
    {
        _currentPrefabRoot = null;
    }

    private static void OnPrefabSaving(GameObject prefabRoot)
    {
        var savedRoot = _currentPrefabRoot;
        _currentPrefabRoot = prefabRoot;
        ProcessPrefabRoot(prefabRoot);
        _currentPrefabRoot = savedRoot;
    }

    private static void OnHierarchyChanged()
    {
        if (Application.isPlaying) return;

        EditorApplication.delayCall -= DelayedRename;
        EditorApplication.delayCall += DelayedRename;
    }

    private static UndoPropertyModification[] OnPropertyModified(UndoPropertyModification[] modifications)
    {
        if (Application.isPlaying) return modifications;

        bool needsUpdate = false;

        foreach (var mod in modifications)
        {
            if (mod.currentValue == null) continue;

            Object target = mod.currentValue.target;
            string propertyPath = mod.currentValue.propertyPath;

            if (IsKeyTypeProperty(target, propertyPath))
            {
                needsUpdate = true;
                break;
            }
        }

        if (needsUpdate)
        {
            EditorApplication.delayCall -= DelayedRenameTypedOnly;
            EditorApplication.delayCall += DelayedRenameTypedOnly;
        }

        return modifications;
    }

    private static bool IsKeyTypeProperty(Object target, string propertyPath)
    {
        if (target is Key && propertyPath.Contains("_keyType"))
        {
            return true;
        }

        if (target is KeyDeliver && propertyPath.Contains("_requiredKeyType"))
        {
            return true;
        }

        if (target is Door && propertyPath.Contains("_requiredKeyType"))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Main Processing

    private static void DelayedRename()
    {
        if (Application.isPlaying) return;

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage != null)
        {
            _currentPrefabRoot = prefabStage.prefabContentsRoot;
            ProcessPrefabRoot(_currentPrefabRoot);
        }
        else
        {
            _currentPrefabRoot = null;
            ProcessScene();
        }
    }

    private static void DelayedRenameTypedOnly()
    {
        if (Application.isPlaying) return;

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        List<GameObject> allObjects;

        if (prefabStage != null)
        {
            _currentPrefabRoot = prefabStage.prefabContentsRoot;
            if (_currentPrefabRoot == null) return;

            allObjects = new List<GameObject>();
            CollectAllChildren(_currentPrefabRoot, allObjects);
        }
        else
        {
            _currentPrefabRoot = null;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            allObjects = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                CollectAllChildren(root, allObjects);
            }
        }

        RenameTypedInstances(allObjects);
    }

    private static void ProcessPrefabRoot(GameObject prefabRoot)
    {
        if (prefabRoot == null) return;

        var allObjects = new List<GameObject>();
        CollectAllChildren(prefabRoot, allObjects);

        ProcessAllObjects(allObjects);
    }

    private static void ProcessScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        var allObjects = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            CollectAllChildren(root, allObjects);
        }

        ProcessAllObjects(allObjects);
    }

    private static void ProcessAllObjects(List<GameObject> allObjects)
    {
        foreach (string prefabName in SIMPLE_PREFABS)
        {
            RenameSimpleInstances(prefabName, allObjects);
        }

        foreach (string fullName in TYPED_PREFAB_FULL_NAMES)
        {
            RenameSimpleInstances(fullName, allObjects);
        }

        RenameTypedInstances(allObjects);
    }

    #endregion

    #region Exclusion Logic

    private static bool ShouldExcludeFromRenaming(GameObject obj)
    {
        // Исключаем корень текущего редактируемого префаба
        if (_currentPrefabRoot != null && obj == _currentPrefabRoot)
        {
            return true;
        }

        // Исключаем корни вложенных префабов (nested prefabs)
        // Это объекты, которые являются экземплярами других префабов
        if (PrefabUtility.IsAnyPrefabInstanceRoot(obj))
        {
            // Но только если это корень ТОГО ЖЕ префаба, что мы редактируем
            // (т.е. сам префаб в себе)
            if (_currentPrefabRoot != null)
            {
                var currentPrefabAsset = GetPrefabAssetRoot(_currentPrefabRoot);
                var objPrefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);

                if (currentPrefabAsset != null && objPrefabAsset != null)
                {
                    // Получаем корневой объект префаба-источника
                    var objPrefabRoot = GetPrefabAssetRoot(objPrefabAsset);

                    if (currentPrefabAsset == objPrefabRoot)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static GameObject GetPrefabAssetRoot(Object obj)
    {
        if (obj == null) return null;

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return null;

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    #endregion

    #region Simple Prefab Renaming

    private static void RenameSimpleInstances(string baseName, List<GameObject> allObjects)
    {
        var toRename = new List<GameObject>();
        var usedNumbers = new HashSet<int>();

        foreach (var obj in allObjects)
        {
            // Пропускаем объекты, которые не должны переименовываться
            if (ShouldExcludeFromRenaming(obj)) continue;

            string name = obj.name;

            if (IsProperlyNamed(name, baseName))
            {
                int num = ExtractNumber(name, baseName);
                if (num >= 0)
                {
                    usedNumbers.Add(num);
                }
                continue;
            }

            if (NeedsRenaming(name, baseName))
            {
                toRename.Add(obj);
            }
        }

        if (toRename.Count == 0) return;

        int nextNumber = 0;
        foreach (var obj in toRename)
        {
            while (usedNumbers.Contains(nextNumber))
            {
                nextNumber++;
            }

            Undo.RecordObject(obj, "Auto Rename Prefab");
            obj.name = $"{baseName}{nextNumber}";
            EditorUtility.SetDirty(obj);
            usedNumbers.Add(nextNumber);
            nextNumber++;
        }
    }

    #endregion

    #region Typed Prefab Renaming (Key, KeyDeliver, Door)

    private static void RenameTypedInstances(List<GameObject> allObjects)
    {
        var usedNumbers = new Dictionary<string, HashSet<int>>();
        var toRename = new List<(GameObject obj, string targetName)>();

        // Первый проход: собираем уже правильно названные объекты
        foreach (var obj in allObjects)
        {
            // Пропускаем объекты, которые не должны переименовываться
            if (ShouldExcludeFromRenaming(obj)) continue;

            string targetName = GetTargetNameForTypedObject(obj);
            if (targetName == null) continue;

            if (!usedNumbers.ContainsKey(targetName))
            {
                usedNumbers[targetName] = new HashSet<int>();
            }

            if (IsProperlyNamed(obj.name, targetName))
            {
                int num = ExtractNumber(obj.name, targetName);
                if (num >= 0)
                {
                    usedNumbers[targetName].Add(num);
                }
            }
        }

        // Второй проход: определяем, какие объекты нужно переименовать
        foreach (var obj in allObjects)
        {
            // Пропускаем объекты, которые не должны переименовываться
            if (ShouldExcludeFromRenaming(obj)) continue;

            string targetName = GetTargetNameForTypedObject(obj);
            if (targetName == null) continue;

            if (!IsProperlyNamed(obj.name, targetName))
            {
                toRename.Add((obj, targetName));
            }
        }

        // Переименовываем объекты
        foreach (var (obj, targetName) in toRename)
        {
            if (!usedNumbers.ContainsKey(targetName))
            {
                usedNumbers[targetName] = new HashSet<int>();
            }

            int nextNumber = 0;
            while (usedNumbers[targetName].Contains(nextNumber))
            {
                nextNumber++;
            }

            Undo.RecordObject(obj, "Auto Rename Typed Prefab");
            obj.name = $"{targetName}{nextNumber}";
            EditorUtility.SetDirty(obj);
            usedNumbers[targetName].Add(nextNumber);
        }
    }

    private static string GetTargetNameForTypedObject(GameObject obj)
    {
        if (obj.TryGetComponent<Key>(out var key))
        {
            return $"Key{GetColorName(key.Type)}";
        }

        if (obj.TryGetComponent<KeyDeliver>(out var keyDeliver))
        {
            return $"KeyDeliver{GetColorName(keyDeliver.RequiredKeyType)}";
        }

        if (obj.TryGetComponent<Door>(out var door))
        {
            return $"Door{GetColorName(door.RequiredKeyType)}";
        }

        return null;
    }

    private static string GetColorName(Key.KeyType keyType)
    {
        return keyType switch
        {
            Key.KeyType.Red => "Red",
            Key.KeyType.Green => "Green",
            Key.KeyType.Blue => "Blue",
            _ => "Unknown"
        };
    }

    #endregion

    #region Helper Methods

    private static void CollectAllChildren(GameObject obj, List<GameObject> list)
    {
        list.Add(obj);
        foreach (Transform child in obj.transform)
        {
            CollectAllChildren(child.gameObject, list);
        }
    }

    private static bool NeedsRenaming(string name, string baseName)
    {
        if (name == baseName) return true;

        string pattern = $@"^{Regex.Escape(baseName)}\s*\(\d+\)$";
        return Regex.IsMatch(name, pattern);
    }

    private static bool IsProperlyNamed(string name, string baseName)
    {
        if (!name.StartsWith(baseName)) return false;
        if (name == baseName) return false;
        if (name.Length <= baseName.Length) return false;

        string suffix = name.Substring(baseName.Length);

        if (suffix.Contains("(") || suffix.Contains(" ")) return false;

        return int.TryParse(suffix, out _);
    }

    private static int ExtractNumber(string name, string baseName)
    {
        if (name.Length <= baseName.Length) return -1;

        string numberPart = name.Substring(baseName.Length);
        return int.TryParse(numberPart, out int result) ? result : -1;
    }

    #endregion
}