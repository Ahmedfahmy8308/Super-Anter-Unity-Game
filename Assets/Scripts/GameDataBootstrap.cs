using UnityEngine;

public class GameDataBootstrap : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeField] private TextAsset gameDataJson;

    public GameDataRuntimeData LoadedData { get; private set; }

    private void Start()
    {
        LoadAndApplyData();
    }

    [ContextMenu("Load And Apply Data")]
    public void LoadAndApplyData()
    {
        if (gameDataJson == null)
        {
            Debug.LogWarning("GameDataBootstrap has no JSON asset assigned.");
            return;
        }

        if (!GameDataLoader.TryLoadFromText(gameDataJson.text, out GameDataRuntimeData loadedData))
        {
            Debug.LogError("Failed to parse game data JSON.");
            return;
        }

        LoadedData = loadedData;

        GameManager.Instance?.ApplyRuntimeData(loadedData);
        AudioManager.Instance?.ApplyRuntimeData(loadedData);

        PlayerController[] players = FindObjectsByType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].ApplyRuntimeData(loadedData);
        }

        EnemyAI[] enemies = FindObjectsByType<EnemyAI>();
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].ApplyRuntimeData(loadedData);
        }

        Coin[] coins = FindObjectsByType<Coin>();
        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].ApplyRuntimeData(loadedData);
        }

        Debug.Log($"Loaded game data for {loadedData.gameTitle}");
    }
}
