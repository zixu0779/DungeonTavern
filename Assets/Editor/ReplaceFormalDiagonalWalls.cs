using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ReplaceFormalDiagonalWalls
{
    private const string FormalScene =
        "Assets/Scenes/Tavern/Tavern_ReadabilityPrototype.unity";
    private const string TileRoot =
        "Assets/DungeonTavern/Art/TileAssets/Dungeon_Walls";

    private readonly struct Replacement
    {
        public readonly Vector3Int Cell;
        public readonly string Expected;
        public readonly string ReplacementName;

        public Replacement(int x, int y, string expected, string replacementName)
        {
            Cell = new Vector3Int(x, y, 0);
            Expected = expected;
            ReplacementName = replacementName;
        }
    }

    private static readonly Replacement[] Replacements =
    {
        new(33, 2, "Walls_Diagonal_3x2_Up_00", "Walls_Diagonal_2x1_Up_00"),
        new(34, 2, "Walls_Diagonal_3x2_Up_01", "Walls_Diagonal_2x1_Up_01"),
        new(35, 3, "Walls_Diagonal_3x2_Up_02", "Walls_Diagonal_2x1_Up_02"),
        new(36, 4, "Walls_Diagonal_3x2_Up_03", "Walls_Diagonal_1x1_Up_03"),
        new(37, 4, "Walls_Diagonal_3x2_Up_04", "Walls_Diagonal_2x1_Up_04"),
        new(38, 5, "Walls_Diagonal_3x2_Up_05", "Walls_Diagonal_2x1_Up_05")
    };

    [MenuItem("Tools/Dungeon Tavern/Replace Formal 3x2 Diagonal Walls")]
    public static void Replace()
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.isDirty)
            throw new InvalidOperationException(
                "Save the active scene before replacing the formal diagonal wall tiles.");

        Scene scene = EditorSceneManager.OpenScene(FormalScene, OpenSceneMode.Single);
        Tilemap wallTilemap = UnityEngine.Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include)
            .FirstOrDefault(tilemap => tilemap.name == "WallTiles");
        if (wallTilemap == null)
            throw new InvalidOperationException("WallTiles was not found in the formal scene.");

        foreach (Replacement replacement in Replacements)
        {
            TileBase currentTile = wallTilemap.GetTile(replacement.Cell);
            if (currentTile == null || currentTile.name != replacement.Expected)
            {
                throw new InvalidOperationException(
                    $"Expected {replacement.Expected} at {replacement.Cell}, " +
                    $"but found {(currentTile == null ? "<empty>" : currentTile.name)}.");
            }
        }

        foreach (Replacement replacement in Replacements)
        {
            string path = $"{TileRoot}/{replacement.ReplacementName}.asset";
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
                throw new InvalidOperationException($"Replacement Tile not found: {path}");
            wallTilemap.SetTile(replacement.Cell, tile);
        }

        wallTilemap.RefreshAllTiles();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, FormalScene))
            throw new InvalidOperationException($"Could not save scene: {FormalScene}");

        Selection.activeGameObject = wallTilemap.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log(
            "Replaced the six formal 3x2-up wall tiles with a " +
            "2x1-up / 1x1-up / 2x1-up sequence. GroundTiles were not modified.");
    }
}
