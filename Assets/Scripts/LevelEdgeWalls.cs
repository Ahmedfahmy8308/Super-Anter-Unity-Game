using UnityEngine;

/// <summary>
/// Invisible vertical barriers at the left/right of the level so the player cannot walk off the tilemap.
/// </summary>
public class LevelEdgeWalls : MonoBehaviour
{
    [SerializeField] private float leftWallCenterX = -15.5f;
    [SerializeField] private float rightWallCenterX = 42.5f;
    [SerializeField] private bool autoSyncRightWallToFlagpole = true;
    [SerializeField] private float rightWallPaddingFromFlag = 1.5f;
    [SerializeField] private Vector2 wallSize = new Vector2(1f, 36f);
    [SerializeField] private float wallCenterY = 2f;

    private void Awake()
    {
        SyncRightWallToLevelEnd();
        CreateWall("LeftBarrier", leftWallCenterX);
        CreateWall("RightBarrier", rightWallCenterX);
    }

    private void SyncRightWallToLevelEnd()
    {
        if (!autoSyncRightWallToFlagpole)
            return;

        Flagpole flagpole = Object.FindAnyObjectByType<Flagpole>();
        if (flagpole != null)
        {
            rightWallCenterX = flagpole.transform.position.x + rightWallPaddingFromFlag;
            return;
        }

        FinishPoint finishPoint = Object.FindAnyObjectByType<FinishPoint>();
        if (finishPoint != null)
            rightWallCenterX = finishPoint.transform.position.x + rightWallPaddingFromFlag;
    }

    private void CreateWall(string objectName, float centerWorldX)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(centerWorldX, wallCenterY, 0f);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = wallSize;
    }
}
