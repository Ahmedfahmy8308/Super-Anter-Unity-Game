[System.Serializable]
public class GameDataRuntimeData
{
    public string gameTitle;
    public string[] buildOrder;
    public ScoreData score;
    public PlayerData player;
    public EnemyData enemy;
    public AudioData audio;
}

[System.Serializable]
public class ScoreData
{
    public int coinValue;
    public int startingScore;
}

[System.Serializable]
public class PlayerData
{
    public float moveSpeed;
    public float acceleration;
    public float deceleration;
    public float jumpForce;
    public float coyoteTime;
    public float jumpBufferTime;
}

[System.Serializable]
public class EnemyData
{
    public float moveSpeed;
    public float waitAtWaypoint;
}

[System.Serializable]
public class AudioData
{
    public float musicVolume;
    public float sfxVolume;
    public float jumpPitchMin;
    public float jumpPitchMax;
}
