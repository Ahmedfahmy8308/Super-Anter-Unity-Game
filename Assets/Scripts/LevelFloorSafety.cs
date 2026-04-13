using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class LevelFloorSafety
{
    private const string RuntimeFloorName = "RuntimeSafetyWorld";
    private static Sprite cachedFloorSprite;

    public static void EnsureFloorForGameplayScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name == "MainMenu") return;
        if (FindRootByName(scene, RuntimeFloorName) != null) return;

        bool hasGroundCollider = HasUsableGround(scene);
        bool hasVisibleGround = HasVisibleGround(scene);
        if (hasGroundCollider && hasVisibleGround) return;

        BuildSafetyBlockWorld(scene, true);
        Debug.Log("[SuperAnter] Block safety world created for scene: " + scene.name);
    }

    private static void BuildSafetyBlockWorld(Scene scene, bool withCollision)
    {
        GameObject root = new GameObject(RuntimeFloorName);
        SceneManager.MoveGameObjectToScene(root, scene);

        // Main ground strip.
        AddBlockRect(root.transform, -20, -6, 74, 2, withCollision);

        // Barriers and platform shapes to avoid empty world feeling.
        AddBlockRect(root.transform, -12, -5, 2, 4, withCollision);
        AddBlockRect(root.transform, -3, -5, 2, 3, withCollision);
        AddBlockRect(root.transform, 8, -5, 2, 5, withCollision);
        AddBlockRect(root.transform, 18, -5, 3, 4, withCollision);
        AddBlockRect(root.transform, 30, -5, 2, 6, withCollision);

        // Floating platforms.
        AddBlockRect(root.transform, 4, -2, 4, 1, withCollision);
        AddBlockRect(root.transform, 14, -1, 4, 1, withCollision);
        AddBlockRect(root.transform, 24, 0, 3, 1, withCollision);
    }

    private static void AddBlockRect(Transform parent, int startX, int startY, int width, int height, bool withCollision)
    {
        Sprite tileSprite = GetFloorSprite();
        if (tileSprite == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tile = new GameObject($"Tile_{startX + x}_{startY + y}");
                tile.transform.SetParent(parent, false);
                tile.transform.position = new Vector3(startX + x + 0.5f, startY + y + 0.5f, 0f);

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = tileSprite;
                renderer.sortingOrder = -100;

                float tint = ((x + y) % 2 == 0) ? 1f : 0.92f;
                renderer.color = new Color(tint, tint, tint, 1f);

                if (withCollision)
                    tile.AddComponent<BoxCollider2D>();
            }
        }
    }

    private static bool HasUsableGround(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            TilemapCollider2D[] tilemapColliders = roots[i].GetComponentsInChildren<TilemapCollider2D>(true);
            for (int t = 0; t < tilemapColliders.Length; t++)
            {
                TilemapCollider2D c = tilemapColliders[t];
                if (c == null || !c.enabled) continue;
                Bounds b = c.bounds;
                if (b.size.x > 20f && b.size.y > 0.1f)
                    return true;
            }

            BoxCollider2D[] boxColliders = roots[i].GetComponentsInChildren<BoxCollider2D>(true);
            for (int b = 0; b < boxColliders.Length; b++)
            {
                BoxCollider2D c = boxColliders[b];
                if (c == null || !c.enabled || c.isTrigger) continue;
                if (c.bounds.size.x > 20f && c.bounds.size.y > 0.1f)
                    return true;
            }
        }

        return false;
    }

    private static bool HasVisibleGround(Scene scene)
    {
        GameObject groundTilemapRoot = FindRootByName(scene, "GroundTilemap");
        if (groundTilemapRoot == null) return false;

        Tilemap tilemap = groundTilemapRoot.GetComponent<Tilemap>();
        if (tilemap == null) return false;

        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;
            if (tilemap.GetSprite(pos) != null)
                return true;
        }

        return false;
    }

    private static GameObject FindRootByName(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
                return roots[i];
        }
        return null;
    }

    private static Sprite GetFloorSprite()
    {
        if (cachedFloorSprite != null)
            return cachedFloorSprite;

        Sprite[] brickSprites = Resources.LoadAll<Sprite>("Mario/Brick");
        if (brickSprites != null && brickSprites.Length > 0)
        {
            cachedFloorSprite = SelectBestGroundSprite(brickSprites);
            if (cachedFloorSprite != null)
                return cachedFloorSprite;
        }

        Sprite[] terrainSprites = Resources.LoadAll<Sprite>("Terrain/Terrain (16x16)");
        if (terrainSprites != null && terrainSprites.Length > 0)
        {
            cachedFloorSprite = SelectBestGroundSprite(terrainSprites);
            return cachedFloorSprite;
        }

        Texture2D tex = Texture2D.whiteTexture;
        cachedFloorSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        return cachedFloorSprite;
    }

    private static Sprite SelectBestGroundSprite(Sprite[] sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            string n = sprites[i].name.ToLowerInvariant();
            if (n.Contains("brick_1") || n.Contains("brick-1") || n.Contains("brick 1"))
                return sprites[i];
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            string n = sprites[i].name.ToLowerInvariant();
            if (n.Contains("brick"))
                return sprites[i];
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.ToLowerInvariant().Contains("ground"))
                return sprites[i];
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                return sprites[i];
        }

        return null;
    }
}
