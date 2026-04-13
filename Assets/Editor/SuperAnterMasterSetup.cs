#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SuperAnterMasterSetup
{
    private const string ScenesRoot = "Assets/Scenes";
    private const string GeneratedRoot = "Assets/Generated";
    private const string SceneTemplatesRoot = "Assets/Generated/SceneTemplates";
    private const string DeveloperCreditText = "Developed by Ahmed Fahmy";

    [MenuItem("Tools/Super Anter/Clean Scenes And UI", false, 0)]
    public static void CleanScenesAndUi()
    {
        string[] scenePaths =
        {
            Path.Combine(ScenesRoot, "MainMenu.unity").Replace("\\", "/"),
            Path.Combine(ScenesRoot, "Level1.unity").Replace("\\", "/"),
            Path.Combine(ScenesRoot, "Level2.unity").Replace("\\", "/"),
            Path.Combine(ScenesRoot, "Level3.unity").Replace("\\", "/")
        };

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string path = scenePaths[i];
            if (!File.Exists(path))
                continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            CleanupScene(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SuperAnter] Cleaned scene: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SuperAnter] Scene/UI cleanup complete.");
    }

    [MenuItem("Tools/Super Anter/Rebuild Generated Folder", false, 1)]
    public static void RebuildGeneratedFolder()
    {
        if (AssetDatabase.IsValidFolder(GeneratedRoot))
            AssetDatabase.DeleteAsset(GeneratedRoot);

        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedRoot, "SceneTemplates");

        CopySceneTemplate("MainMenu.unity");
        CopySceneTemplate("Level1.unity");
        CopySceneTemplate("Level2.unity");
        CopySceneTemplate("Level3.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SuperAnter] Generated folder recreated.");
    }

    [MenuItem("Tools/Super Anter/Clean + Rebuild Generated", false, 2)]
    public static void CleanAndRebuildAll()
    {
        EnsureLevel3Scene();
        CleanScenesAndUi();
        RebuildGeneratedFolder();
        Debug.Log("[SuperAnter] Cleanup and rebuild finished.");
    }

    [MenuItem("Tools/Super Anter/Create MainMenu LevelSelect Panel", false, 3)]
    public static void CreateMainMenuLevelSelectPanel()
    {
        string path = Path.Combine(ScenesRoot, "MainMenu.unity").Replace("\\", "/");
        if (!File.Exists(path))
        {
            Debug.LogWarning("[SuperAnter] MainMenu scene not found.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        EnsureMainMenuLevelSelectPanel(scene, true);
        EnsureMainMenuDeveloperCredit(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SuperAnter] MainMenu LevelSelect panel ensured.");
    }

    private static void EnsureLevel3Scene()
    {
        string level2 = Path.Combine(ScenesRoot, "Level2.unity").Replace("\\", "/");
        string level3 = Path.Combine(ScenesRoot, "Level3.unity").Replace("\\", "/");

        if (!File.Exists(level2) || File.Exists(level3))
            return;

        if (AssetDatabase.CopyAsset(level2, level3))
            Debug.Log("[SuperAnter] Created Level3 from Level2.");
        else
            Debug.LogWarning("[SuperAnter] Could not create Level3 from Level2.");
    }

    private static void CleanupScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        RemoveDuplicateRootsByName(scene, "Systems");
        RemoveDuplicateRootsByName(scene, "EventSystem");
        RemoveDuplicateRootsByName(scene, "UICanvas");

        if (scene.name != "MainMenu")
        {
            RemoveAllRootsByName(scene, "RuntimeSafetyFloor");
            RemoveAllRootsByName(scene, "RuntimeSafetyWorld");
            RemoveAllObjectsByName(scene, "HudDeveloperText");
            RemoveAllObjectsByName(scene, "MainMenuDeveloperText");
            RemoveAllObjectsByName(scene, "SettingsDeveloperText");
            EnsureSingleEventSystem(scene);
            LowerMainCameraSlightly(scene);
            LevelFloorSafety.EnsureFloorForGameplayScene(scene);
        }
        else
        {
            RemoveAllObjectsByName(scene, "HudDeveloperText");
            EnsureMainMenuLevelSelectPanel(scene, false);
            EnsureMainMenuDeveloperCredit(scene);
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            RemoveDuplicateMonoBehavioursInTree(roots[i].transform);
    }

    private static void RemoveDuplicateRootsByName(Scene scene, string rootName)
    {
        List<GameObject> matches = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (string.Equals(roots[i].name, rootName, StringComparison.Ordinal))
                matches.Add(roots[i]);
        }

        for (int i = 1; i < matches.Count; i++)
            UnityEngine.Object.DestroyImmediate(matches[i]);
    }

    private static void RemoveAllRootsByName(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (string.Equals(roots[i].name, rootName, StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(roots[i]);
        }
    }

    private static void RemoveAllObjectsByName(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            RemoveObjectsByNameRecursive(roots[i].transform, objectName);
    }

    private static void RemoveObjectsByNameRecursive(Transform root, string objectName)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            RemoveObjectsByNameRecursive(root.GetChild(i), objectName);

        if (string.Equals(root.name, objectName, StringComparison.Ordinal))
            UnityEngine.Object.DestroyImmediate(root.gameObject);
    }

    private static void EnsureSingleEventSystem(Scene scene)
    {
        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude);
        if (systems.Length == 0)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            SceneManager.MoveGameObjectToScene(go, scene);
            return;
        }

        for (int i = 1; i < systems.Length; i++)
            UnityEngine.Object.DestroyImmediate(systems[i].gameObject);
    }

    private static void RemoveDuplicateMonoBehavioursInTree(Transform root)
    {
        if (root == null)
            return;

        RemoveDuplicateMonoBehaviours(root.gameObject);
        for (int i = 0; i < root.childCount; i++)
            RemoveDuplicateMonoBehavioursInTree(root.GetChild(i));
    }

    private static void RemoveDuplicateMonoBehaviours(GameObject go)
    {
        MonoBehaviour[] components = go.GetComponents<MonoBehaviour>();
        HashSet<Type> seen = new HashSet<Type>();

        for (int i = 0; i < components.Length; i++)
        {
            MonoBehaviour c = components[i];
            if (c == null)
                continue;

            Type type = c.GetType();
            if (seen.Contains(type))
                UnityEngine.Object.DestroyImmediate(c);
            else
                seen.Add(type);
        }
    }

    private static void LowerMainCameraSlightly(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < cameras.Length; c++)
            {
                Camera cam = cameras[c];
                if (cam == null || !cam.CompareTag("MainCamera"))
                    continue;

                Transform t = cam.transform;
                Vector3 pos = t.position;
                float loweredY = Mathf.Max(-2f, pos.y - 1f);
                t.position = new Vector3(pos.x, loweredY, pos.z);
                return;
            }
        }
    }

    private static void CopySceneTemplate(string sceneName)
    {
        string source = Path.Combine(ScenesRoot, sceneName).Replace("\\", "/");
        string destination = Path.Combine(SceneTemplatesRoot, sceneName).Replace("\\", "/");

        if (!File.Exists(source))
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destination) != null)
            AssetDatabase.DeleteAsset(destination);

        AssetDatabase.CopyAsset(source, destination);
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void EnsureMainMenuLevelSelectPanel(Scene scene, bool forceRebuild)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject canvasRoot = FindCanvasRoot(scene);
        if (canvasRoot == null)
            return;

        Transform existing = FindChildByName(canvasRoot.transform, "LevelSelectPanel");
        if (existing != null)
        {
            if (forceRebuild)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            else
            {
                existing.gameObject.SetActive(true);
                CanvasGroup cg = existing.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                return;
            }
        }

        GameObject panel = new GameObject("LevelSelectPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panel.transform.SetParent(canvasRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.03f, 0.08f, 0.15f, 0.86f);

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        CreateRuntimeTitle(panel.transform, "CHOOSE LEVEL", new Vector2(0f, 165f));

        CreateRuntimeLevelButton(panel.transform, "Level1Button", "Level 1", "L1", new Vector2(0f, 80f), new Color(0.11f, 0.36f, 0.67f, 0.98f));
        CreateRuntimeLevelButton(panel.transform, "Level2Button", "Level 2", "L2", new Vector2(0f, 0f), new Color(0.10f, 0.46f, 0.59f, 0.98f));
        CreateRuntimeLevelButton(panel.transform, "Level3Button", "Level 3", "L3", new Vector2(0f, -80f), new Color(0.14f, 0.55f, 0.48f, 0.98f));
        CreateRuntimeLevelButton(panel.transform, "BackButton", "Back", "<", new Vector2(0f, -170f), new Color(0.28f, 0.31f, 0.36f, 0.95f));

        CreateRuntimeInfoText(panel.transform, "Level1InfoText", new Vector2(0f, 45f));
        CreateRuntimeInfoText(panel.transform, "Level2InfoText", new Vector2(0f, -35f));
        CreateRuntimeInfoText(panel.transform, "Level3InfoText", new Vector2(0f, -115f));
    }

    private static void EnsureMainMenuDeveloperCredit(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject canvasRoot = FindCanvasRoot(scene);
        if (canvasRoot == null)
            return;

        Transform parent = canvasRoot.transform;

        Transform existing = FindChildByName(parent, "MainMenuDeveloperText");
        TextMeshProUGUI text;

        if (existing == null)
        {
            GameObject go = new GameObject("MainMenuDeveloperText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            text = go.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = existing.gameObject.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(760f, 42f);
        rect.anchoredPosition = new Vector2(-24f, 18f);

        text.text = DeveloperCreditText;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.fontSize = 26f;
        text.color = new Color(0.93f, 0.96f, 1f, 0.95f);
        text.fontStyle = FontStyles.Bold;
    }

    private static GameObject FindCanvasRoot(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Canvas c = roots[i].GetComponentInChildren<Canvas>(true);
            if (c != null)
                return c.gameObject;
        }
        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void CreateRuntimeTitle(Transform parent, string title, Vector2 anchoredPos)
    {
        GameObject titleGo = new GameObject("LevelSelectTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(parent, false);
        RectTransform rect = titleGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(680f, 70f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI text = titleGo.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.92f, 0.98f, 1f, 1f);
        text.fontSize = 56f;
    }

    private static void CreateRuntimeLevelButton(Transform parent, string objectName, string label, string iconLabel, Vector2 anchoredPos, Color buttonColor)
    {
        GameObject buttonGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 60f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;

        Image img = buttonGo.GetComponent<Image>();
        img.color = buttonColor;

        GameObject iconBgGo = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconBgGo.transform.SetParent(buttonGo.transform, false);
        RectTransform iconBgRect = iconBgGo.GetComponent<RectTransform>();
        iconBgRect.anchorMin = new Vector2(0f, 0.5f);
        iconBgRect.anchorMax = new Vector2(0f, 0.5f);
        iconBgRect.sizeDelta = new Vector2(36f, 36f);
        iconBgRect.anchoredPosition = new Vector2(26f, 0f);
        Image iconBg = iconBgGo.GetComponent<Image>();
        iconBg.color = new Color(1f, 1f, 1f, 0.2f);

        GameObject iconTextGo = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
        iconTextGo.transform.SetParent(iconBgGo.transform, false);
        RectTransform iconTextRect = iconTextGo.GetComponent<RectTransform>();
        iconTextRect.anchorMin = Vector2.zero;
        iconTextRect.anchorMax = Vector2.one;
        iconTextRect.offsetMin = Vector2.zero;
        iconTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI iconText = iconTextGo.GetComponent<TextMeshProUGUI>();
        iconText.text = iconLabel;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = Color.white;
        iconText.fontSize = 24f;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(56f, 0f);
        textRect.offsetMax = new Vector2(-14f, 0f);

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.fontSize = 32f;
    }

    private static void CreateRuntimeInfoText(Transform parent, string objectName, Vector2 anchoredPos)
    {
        GameObject infoGo = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        infoGo.transform.SetParent(parent, false);
        RectTransform rect = infoGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(460f, 32f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI text = infoGo.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.84f, 0.92f, 0.98f, 0.95f);
        text.fontSize = 22f;
    }
}
#endif
