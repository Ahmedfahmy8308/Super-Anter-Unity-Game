using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Super Anter/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    [Tooltip("Must match a scene in Build Settings (e.g. Assets/Scenes/MainMenu.unity).")]
    public string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [Tooltip("Ordered list of gameplay level scene asset paths. Index 0 is World 1-1.")]
    public string[] levelScenePaths = Array.Empty<string>();

    [Tooltip("Coins required to complete each level. If shorter than levelScenePaths, missing values default to 0.")]
    public int[] requiredCoinCounts = Array.Empty<int>();

    public int LevelCount => levelScenePaths != null ? levelScenePaths.Length : 0;

    public int GetMainMenuBuildIndex()
    {
        if (string.IsNullOrEmpty(mainMenuScenePath)) return -1;
        int idx = SceneUtility.GetBuildIndexByScenePath(mainMenuScenePath);
        return idx >= 0 ? idx : -1;
    }

    public int GetLevelBuildIndex(int levelOrderIndex)
    {
        if (levelScenePaths == null || levelOrderIndex < 0 || levelOrderIndex >= levelScenePaths.Length)
            return -1;
        string path = levelScenePaths[levelOrderIndex];
        if (string.IsNullOrEmpty(path)) return -1;
        int idx = SceneUtility.GetBuildIndexByScenePath(path);
        return idx >= 0 ? idx : -1;
    }

    /// <summary>Returns 0-based index in levelScenePaths, or -1 if not a catalog level.</summary>
    public int GetCatalogIndexForScenePath(string sceneAssetPath)
    {
        if (levelScenePaths == null || string.IsNullOrEmpty(sceneAssetPath)) return -1;
        for (int i = 0; i < levelScenePaths.Length; i++)
        {
            if (string.Equals(levelScenePaths[i], sceneAssetPath, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    public int GetCatalogIndexForActiveScene()
    {
        return GetCatalogIndexForScenePath(SceneManager.GetActiveScene().path);
    }

    public bool HasNextLevelAfter(int catalogIndex)
    {
        return catalogIndex >= 0 && catalogIndex < LevelCount - 1;
    }

    public int GetNextLevelBuildIndex(int currentCatalogIndex)
    {
        if (!HasNextLevelAfter(currentCatalogIndex)) return -1;
        return GetLevelBuildIndex(currentCatalogIndex + 1);
    }

    public bool IsGameplayScene(string sceneAssetPath)
    {
        return GetCatalogIndexForScenePath(sceneAssetPath) >= 0;
    }

    public int GetRequiredCoinsForLevel(int levelOrderIndex)
    {
        if (requiredCoinCounts == null || levelOrderIndex < 0 || levelOrderIndex >= requiredCoinCounts.Length)
            return 0;
        return Mathf.Max(0, requiredCoinCounts[levelOrderIndex]);
    }

    public string GetLevelScenePath(int levelOrderIndex)
    {
        if (levelScenePaths == null || levelOrderIndex < 0 || levelOrderIndex >= levelScenePaths.Length)
            return string.Empty;
        return levelScenePaths[levelOrderIndex] ?? string.Empty;
    }
}
