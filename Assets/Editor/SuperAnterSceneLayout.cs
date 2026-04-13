#if UNITY_EDITOR
using UnityEngine;

public static class SuperAnterSceneLayout
{
    public const int TargetLevelCount = 3;

    public static readonly string[] SceneOrder =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Level1.unity",
        "Assets/Scenes/Level2.unity",
        "Assets/Scenes/Level3.unity"
    };

    public static readonly int[] RequiredCoinsPerLevel =
    {
        10,
        20,
        30
    };

    public static readonly Vector3[] DefaultPlayerSpawns =
    {
        new Vector3(-8.64f, -0.29f, 0f),
        new Vector3(-8f, -0.5f, 0f),
        new Vector3(-8f, -0.5f, 0f)
    };
}
#endif
