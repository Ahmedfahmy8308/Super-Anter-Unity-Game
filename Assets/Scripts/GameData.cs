using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Super Anter/Game Data")]
public class GameData : ScriptableObject
{
    [Header("Game")]
    public string gameTitle = "Super Anter";

    [Header("Score")]
    public int coinValue = 1;
    public int startingScore = 0;
    public int coinsFor1UP = 100;

    [Header("Lives")]
    public int startingLives = 3;
    public int maxLives = 99;

    [Header("Timer")]
    public float levelTimeLimit = 300f;
    public float timeWarningThreshold = 60f;
    public int timeUpScorePerSecond = 50;

    [Header("Player")]
    public float moveSpeed = 8f;
    public float acceleration = 70f;
    public float deceleration = 90f;
    public float jumpForce = 15f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float sprintMultiplier = 1.45f;
    public float wallSlideSpeed = 2.5f;
    public float wallJumpForceX = 12f;
    public float wallJumpForceY = 16f;

    [Header("Enemy")]
    public float enemyMoveSpeed = 2f;
    public float enemyWaitAtWaypoint = 0.35f;
    public float stompBounceForce = 14f;
    public int stompScoreValue = 100;

    [Header("Power-Ups")]
    public float invincibilityDuration = 2f;
    public float starDuration = 10f;

    [Header("Audio")]
    [Range(0f, 1f)] public float musicVolume = 0.75f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public float jumpPitchMin = 0.94f;
    public float jumpPitchMax = 1.06f;
}
