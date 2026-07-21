using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RenameWallPrefabsBySourceOrder
{
    private const string Root =
        "Assets/DungeonTavern/Prototypes/Rotation25D/WallPrefabs";

    // Left-to-right order in Walls_interior.png (all at y=98).
    private static readonly string[] SourceSpriteNames =
    {
        "Walls_interior_7",
        "Walls_interior_20",
        "Walls_interior_14",
        "Walls_interior_21",
        "Walls_interior_22",
        "Walls_interior_15",
        "Walls_interior_23",
        "Walls_interior_16",
        "Walls_interior_24",
        "Walls_interior_25"
    };

    private readonly struct Family
    {
        public Family(string folder, string oldPrefix, string newPrefix)
        {
            Folder = folder;
            OldPrefix = oldPrefix;
            NewPrefix = newPrefix;
        }

        public string Folder { get; }
        public string OldPrefix { get; }
        public string NewPrefix { get; }
    }

    private static readonly Family[] Families =
    {
        new("Horizontal", "Wall_", "Wall_Horizontal_"),
        new("Diagonal/1x1", "Wall_1x1_Up_", "Wall_1x1_Up_"),
        new("Diagonal/1x1", "Wall_1x1_Down_", "Wall_1x1_Down_"),
        new("Diagonal/2x1", "Wall_2x1_Up_", "Wall_2x1_Up_"),
        new("Diagonal/2x1", "Wall_2x1_Down_", "Wall_2x1_Down_")
    };

    [MenuItem("Tools/Dungeon Tavern/Rename Wall Prefabs By Source Order")]
    public static void Rename()
    {
        List<string> renamedPaths = new();
        List<string> errors = new();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (Family family in Families)
            {
                for (int index = 0; index < SourceSpriteNames.Length; index++)
                {
                    string folder = $"{Root}/{family.Folder}";
                    string oldPath =
                        $"{folder}/{family.OldPrefix}{SourceSpriteNames[index]}.prefab";
                    string newPath =
                        $"{folder}/{family.NewPrefix}{index + 1:00}.prefab";

                    if (AssetDatabase.LoadAssetAtPath<GameObject>(oldPath) == null)
                    {
                        if (AssetDatabase.LoadAssetAtPath<GameObject>(newPath) != null)
                            renamedPaths.Add(newPath);
                        else
                            errors.Add($"Missing source prefab: {oldPath}");
                        continue;
                    }

                    if (AssetDatabase.LoadAssetAtPath<GameObject>(newPath) != null)
                    {
                        errors.Add($"Destination already exists: {newPath}");
                        continue;
                    }

                    string moveError = AssetDatabase.MoveAsset(oldPath, newPath);
                    if (string.IsNullOrEmpty(moveError))
                        renamedPaths.Add(newPath);
                    else
                        errors.Add($"{oldPath} -> {newPath}: {moveError}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        foreach (string prefabPath in renamedPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(prefabPath);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log(
            $"Renamed {renamedPaths.Count} wall prefabs to source-order names " +
            "(01-10) while preserving their asset GUIDs.");
    }
}
