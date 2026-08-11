#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// One-shot editor automation: creates ConnectionScreen and Lobby scenes,
/// wires them into Build Settings, and sets up all GameObjects + components.
/// Run via: Tools → TeamTilt → Setup Scenes.
/// </summary>
public static class SceneSetupTool
{
    [MenuItem("Tools/TeamTilt/Setup Scenes")]
    public static void SetupAllScenes()
    {
        // Ensure folders exist
        EnsureDir("Assets/_Scenes");
        EnsureDir("Assets/_Prefabs/UI");

        SetupMainMenuScene();
        SetupConnectionScreenScene();
        SetupLobbyScene();
        AddScenesToBuildSettings();

        AssetDatabase.Refresh();
        Debug.Log("[SceneSetupTool] All scenes created and added to Build Settings.");
        EditorUtility.DisplayDialog("TeamTilt Scene Setup", "All scenes configured successfully!\n\nCheck File > Build Settings to verify.", "OK");
    }

    // ── Main Menu ─────────────────────────────────────────────

    private static void SetupMainMenuScene()
    {
        string path = "Assets/_Scenes/MainMenu.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        CreateCamera(scene);

        // --- Canvas root ---
        var canvasGO = CreateCanvas(scene, "MainMenuCanvas");
        var canvas    = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Background Image (full-screen)
        var bgGO  = CreateChildStretch(canvasGO, "Background");
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.06f, 0.20f); // deep purple fallback
        // bgGO.AddComponent<BackgroundManager>();

        // Title text
        var titleGO = new GameObject("TitleText");
        SceneManager.MoveGameObjectToScene(titleGO, scene);
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "TEAM TILT";
        titleTMP.fontSize  = 88;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = new Color(1f, 0.92f, 0.23f);  // bright yellow
        var titleRect = titleTMP.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(800, 120);
        titleRect.anchoredPosition = Vector2.zero;

        // Subtitle
        var subtGO  = new GameObject("SubtitleText");
        SceneManager.MoveGameObjectToScene(subtGO, scene);
        subtGO.transform.SetParent(canvasGO.transform, false);
        var subtTMP = subtGO.AddComponent<TextMeshProUGUI>();
        subtTMP.text      = "Co-op Chaos. Pure Fun.";
        subtTMP.fontSize  = 28;
        subtTMP.alignment = TextAlignmentOptions.Center;
        subtTMP.color     = new Color(0.80f, 0.80f, 1.0f);
        var subtRect = subtTMP.GetComponent<RectTransform>();
        subtRect.anchorMin = new Vector2(0.5f, 0.62f);
        subtRect.anchorMax = new Vector2(0.5f, 0.62f);
        subtRect.sizeDelta = new Vector2(600, 50);
        subtRect.anchoredPosition = Vector2.zero;

        // Play button
        var playBtn = CreateButton(canvasGO, scene, "PlayButton", "PLAY",
            new Vector2(0.5f, 0.45f), new Color(0.35f, 0.85f, 0.55f));

        // Quit button
        var quitBtn = CreateButton(canvasGO, scene, "QuitButton", "QUIT",
            new Vector2(0.5f, 0.33f), new Color(0.90f, 0.30f, 0.30f));

        // MainMenuUI controller
        var uiCtrl  = canvasGO.AddComponent<MainMenuUI>();
        SetPrivateField(uiCtrl, "_playButton", playBtn.GetComponent<Button>());
        SetPrivateField(uiCtrl, "_quitButton", quitBtn.GetComponent<Button>());

        // EventSystem
        var esGO = new GameObject("EventSystem");
        SceneManager.MoveGameObjectToScene(esGO, scene);
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"[SceneSetupTool] MainMenu scene saved to {path}");
    }

    // ── Connection Screen ─────────────────────────────────────

    private static void SetupConnectionScreenScene()
    {
        string path = "Assets/_Scenes/ConnectionScreen.unity";

        var scene    = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        CreateCamera(scene);
        var canvasGO = CreateCanvas(scene, "ConnectionCanvas");

        // Background
        var bgGO  = CreateChildStretch(canvasGO, "Background");
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.05f, 0.18f);

        // Title
        var titleGO  = new GameObject("TitleText");
        SceneManager.MoveGameObjectToScene(titleGO, scene);
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "JOIN OR HOST";
        titleTMP.fontSize  = 52;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = new Color(1f, 0.92f, 0.23f);
        var titleRect = titleTMP.GetComponent<RectTransform>();
        titleRect.anchorMin       = new Vector2(0.5f, 0.78f);
        titleRect.anchorMax       = new Vector2(0.5f, 0.78f);
        titleRect.sizeDelta       = new Vector2(700, 80);
        titleRect.anchoredPosition = Vector2.zero;

        // Host button
        var hostBtn = CreateButton(canvasGO, scene, "HostButton", "HOST A GAME",
            new Vector2(0.5f, 0.60f), new Color(0.30f, 0.75f, 0.50f));

        // Divider label
        var divGO  = new GameObject("OrLabel");
        SceneManager.MoveGameObjectToScene(divGO, scene);
        divGO.transform.SetParent(canvasGO.transform, false);
        var divTMP = divGO.AddComponent<TextMeshProUGUI>();
        divTMP.text      = "— or —";
        divTMP.fontSize  = 22;
        divTMP.alignment = TextAlignmentOptions.Center;
        divTMP.color     = new Color(0.6f, 0.6f, 0.6f);
        var divRect = divTMP.GetComponent<RectTransform>();
        divRect.anchorMin       = new Vector2(0.5f, 0.50f);
        divRect.anchorMax       = new Vector2(0.5f, 0.50f);
        divRect.sizeDelta       = new Vector2(300, 40);
        divRect.anchoredPosition = Vector2.zero;

        // IP Input
        var ipGO   = new GameObject("IPInput");
        SceneManager.MoveGameObjectToScene(ipGO, scene);
        ipGO.transform.SetParent(canvasGO.transform, false);
        var inputRect = ipGO.AddComponent<RectTransform>();
        inputRect.anchorMin       = new Vector2(0.5f, 0.43f);
        inputRect.anchorMax       = new Vector2(0.5f, 0.43f);
        inputRect.sizeDelta       = new Vector2(380, 60);
        inputRect.anchoredPosition = Vector2.zero;
        var ipBg  = ipGO.AddComponent<Image>();
        ipBg.color = new Color(0.15f, 0.10f, 0.30f);
        var ipInput  = ipGO.AddComponent<TMP_InputField>();
        // Placeholder child
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(ipGO.transform, false);
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text  = "127.0.0.1";
        phTMP.color = new Color(0.5f, 0.5f, 0.5f);
        phTMP.fontSize = 24;
        phTMP.margin   = new Vector4(10, 0, 10, 0);
        var phRect = phGO.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax  = Vector2.zero;
        // Text child
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(ipGO.transform, false);
        var txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        txtTMP.color    = Color.white;
        txtTMP.fontSize = 24;
        txtTMP.margin   = new Vector4(10, 0, 10, 0);
        var txtRect = txtGO.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax  = Vector2.zero;
        ipInput.placeholder = phTMP;
        ipInput.textComponent = txtTMP;

        // Join button
        var joinBtn = CreateButton(canvasGO, scene, "JoinButton", "JOIN GAME",
            new Vector2(0.5f, 0.33f), new Color(0.30f, 0.55f, 0.90f));

        // Status text
        var statusGO  = new GameObject("StatusText");
        SceneManager.MoveGameObjectToScene(statusGO, scene);
        statusGO.transform.SetParent(canvasGO.transform, false);
        var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text      = "";
        statusTMP.fontSize  = 20;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color     = new Color(0.9f, 0.9f, 0.5f);
        var statusRect = statusTMP.GetComponent<RectTransform>();
        statusRect.anchorMin       = new Vector2(0.5f, 0.24f);
        statusRect.anchorMax       = new Vector2(0.5f, 0.24f);
        statusRect.sizeDelta       = new Vector2(700, 50);
        statusRect.anchoredPosition = Vector2.zero;

        // Back button
        var backBtn = CreateButton(canvasGO, scene, "BackButton", "← BACK",
            new Vector2(0.5f, 0.14f), new Color(0.5f, 0.5f, 0.5f), width: 200, height: 55, fontSize: 22);

        // ConnectionScreenUI controller
        var ctrl = canvasGO.AddComponent<ConnectionScreenUI>();
        SetPrivateField(ctrl, "_hostButton",  hostBtn.GetComponent<Button>());
        SetPrivateField(ctrl, "_joinButton",  joinBtn.GetComponent<Button>());
        SetPrivateField(ctrl, "_ipInput",     ipInput);
        SetPrivateField(ctrl, "_statusText",  statusTMP);
        SetPrivateField(ctrl, "_backButton",  backBtn.GetComponent<Button>());

        // EventSystem
        var esGO = new GameObject("EventSystem");
        SceneManager.MoveGameObjectToScene(esGO, scene);
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"[SceneSetupTool] ConnectionScreen scene saved to {path}");
    }

    // ── Lobby ─────────────────────────────────────────────────

    private static void SetupLobbyScene()
    {
        string path = "Assets/_Scenes/Lobby.unity";

        var scene    = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        CreateCamera(scene);
        var canvasGO = CreateCanvas(scene, "LobbyCanvas");

        // Background
        var bgGO  = CreateChildStretch(canvasGO, "Background");
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.04f, 0.15f);

        // Title
        var titleGO  = new GameObject("TitleText");
        SceneManager.MoveGameObjectToScene(titleGO, scene);
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "LOBBY";
        titleTMP.fontSize  = 60;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = new Color(1f, 0.92f, 0.23f);
        var titleRect = titleTMP.GetComponent<RectTransform>();
        titleRect.anchorMin       = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax       = new Vector2(0.5f, 0.85f);
        titleRect.sizeDelta       = new Vector2(500, 90);
        titleRect.anchoredPosition = Vector2.zero;

        // Player count label  
        var countGO  = new GameObject("PlayerCountText");
        SceneManager.MoveGameObjectToScene(countGO, scene);
        countGO.transform.SetParent(canvasGO.transform, false);
        var countTMP = countGO.AddComponent<TextMeshProUGUI>();
        countTMP.text      = "Players: 0";
        countTMP.fontSize  = 26;
        countTMP.alignment = TextAlignmentOptions.Center;
        countTMP.color     = Color.white;
        var countRect = countTMP.GetComponent<RectTransform>();
        countRect.anchorMin       = new Vector2(0.5f, 0.77f);
        countRect.anchorMax       = new Vector2(0.5f, 0.77f);
        countRect.sizeDelta       = new Vector2(400, 40);
        countRect.anchoredPosition = Vector2.zero;

        // Player list panel (scroll area)
        var listPanelGO = new GameObject("PlayerListPanel");
        SceneManager.MoveGameObjectToScene(listPanelGO, scene);
        listPanelGO.transform.SetParent(canvasGO.transform, false);
        var lpImg  = listPanelGO.AddComponent<Image>();
        lpImg.color = new Color(0.12f, 0.08f, 0.25f, 0.8f);
        var lpRect = listPanelGO.GetComponent<RectTransform>();
        lpRect.anchorMin       = new Vector2(0.5f, 0.35f);
        lpRect.anchorMax       = new Vector2(0.5f, 0.73f);
        lpRect.sizeDelta       = new Vector2(600, 0);
        lpRect.anchoredPosition = Vector2.zero;

        // Vertical layout for entries
        var listContainer = new GameObject("PlayerList");
        SceneManager.MoveGameObjectToScene(listContainer, scene);
        listContainer.transform.SetParent(listPanelGO.transform, false);
        var lcRect = listContainer.AddComponent<RectTransform>();
        lcRect.anchorMin = Vector2.zero; lcRect.anchorMax = Vector2.one;
        lcRect.offsetMin = new Vector2(10, 10); lcRect.offsetMax = new Vector2(-10, -10);
        var vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 8;
        vlg.childAlignment      = TextAnchor.UpperCenter;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Waiting text
        var waitGO  = new GameObject("WaitingText");
        SceneManager.MoveGameObjectToScene(waitGO, scene);
        waitGO.transform.SetParent(canvasGO.transform, false);
        var waitTMP = waitGO.AddComponent<TextMeshProUGUI>();
        waitTMP.text      = "Waiting for players…";
        waitTMP.fontSize  = 22;
        waitTMP.alignment = TextAlignmentOptions.Center;
        waitTMP.color     = new Color(0.8f, 0.8f, 1.0f);
        var waitRect = waitTMP.GetComponent<RectTransform>();
        waitRect.anchorMin       = new Vector2(0.5f, 0.27f);
        waitRect.anchorMax       = new Vector2(0.5f, 0.27f);
        waitRect.sizeDelta       = new Vector2(500, 40);
        waitRect.anchoredPosition = Vector2.zero;

        // Start Game button (host-only)
        var startBtn = CreateButton(canvasGO, scene, "StartGameButton", "START GAME",
            new Vector2(0.5f, 0.18f), new Color(0.25f, 0.80f, 0.45f));

        // Disconnect button
        var discBtn = CreateButton(canvasGO, scene, "DisconnectButton", "← LEAVE",
            new Vector2(0.5f, 0.08f), new Color(0.55f, 0.55f, 0.55f), width: 200, height: 55, fontSize: 22);

        // LobbyUI controller
        var lobbyUI = canvasGO.AddComponent<LobbyUI>();
        SetPrivateField(lobbyUI, "_playerListContainer", listContainer.transform);
        SetPrivateField(lobbyUI, "_playerCountText",     countTMP);
        SetPrivateField(lobbyUI, "_waitingText",         waitTMP);
        SetPrivateField(lobbyUI, "_startGameButton",     startBtn.GetComponent<Button>());
        SetPrivateField(lobbyUI, "_disconnectButton",    discBtn.GetComponent<Button>());

        // ── LobbyManager Network Object ──────────────────────
        var lmGO = new GameObject("LobbyManager");
        SceneManager.MoveGameObjectToScene(lmGO, scene);
        lmGO.AddComponent<NetworkObject>();
        lmGO.AddComponent<LobbyManager>();

        // ── LobbyPlayerEntry prefab reference ────────────────
        // We need a prefab for player entries. Create a simple one.
        var entryPrefab = CreateLobbyEntryPrefab();
        SetPrivateField(lobbyUI, "_playerEntryPrefab", entryPrefab);

        // EventSystem
        var esGO = new GameObject("EventSystem");
        SceneManager.MoveGameObjectToScene(esGO, scene);
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"[SceneSetupTool] Lobby scene saved to {path}");
    }

    // ── Prefabs ───────────────────────────────────────────────

    private static GameObject CreateLobbyEntryPrefab()
    {
        // Create a temporary scene object to turn into a prefab
        var rootGO = new GameObject("LobbyPlayerEntry");
        rootGO.AddComponent<RectTransform>().sizeDelta = new Vector2(560, 64);

        // Background panel
        var bg = rootGO.AddComponent<Image>();
        bg.color = new Color(0.20f, 0.14f, 0.38f);

        // Avatar color swatch
        var avatarGO    = new GameObject("AvatarColor");
        avatarGO.transform.SetParent(rootGO.transform, false);
        var avatarImg   = avatarGO.AddComponent<Image>();
        avatarImg.color = Color.white;
        var avatarRect  = avatarGO.GetComponent<RectTransform>();
        avatarRect.anchorMin       = new Vector2(0f, 0.5f);
        avatarRect.anchorMax       = new Vector2(0f, 0.5f);
        avatarRect.sizeDelta       = new Vector2(44, 44);
        avatarRect.anchoredPosition = new Vector2(34, 0);

        // Name label
        var nameGO   = new GameObject("NameLabel");
        nameGO.transform.SetParent(rootGO.transform, false);
        var nameTMP  = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text     = "Player";
        nameTMP.fontSize = 26;
        nameTMP.color    = Color.white;
        var nameRect = nameTMP.GetComponent<RectTransform>();
        nameRect.anchorMin       = new Vector2(0f, 0f);
        nameRect.anchorMax       = new Vector2(1f, 1f);
        nameRect.offsetMin       = new Vector2(92, 0);
        nameRect.offsetMax       = new Vector2(-16, 0);

        // Wire up LobbyPlayerEntry component
        // var entryComp = rootGO.AddComponent<LobbyPlayerEntry>();
        // SetPrivateField(entryComp, "_nameLabel",   nameTMP);
        // SetPrivateField(entryComp, "_avatarColor", avatarImg);

        // Save as prefab
        string prefabDir  = "Assets/_Prefabs/UI";
        string prefabPath = $"{prefabDir}/LobbyPlayerEntry.prefab";
        EnsureDir(prefabDir);

        var prefab = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
        Object.DestroyImmediate(rootGO);
        Debug.Log($"[SceneSetupTool] LobbyPlayerEntry prefab saved to {prefabPath}");
        return prefab;
    }

    // ── Build Settings ────────────────────────────────────────

    private static void AddScenesToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string[] required =
        {
            "Assets/_Scenes/Bootstrap.unity",
            "Assets/_Scenes/MainMenu.unity",
            "Assets/_Scenes/ConnectionScreen.unity",
            "Assets/_Scenes/Lobby.unity",
            "Assets/_Scenes/GameLevel1.unity",
        };

        foreach (var scenePath in required)
        {
            bool exists = false;
            foreach (var s in scenes)
                if (s.path.Equals(scenePath, System.StringComparison.OrdinalIgnoreCase))
                { exists = true; break; }

            if (!exists)
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            else
            {
                // Ensure it's enabled
                for (int i = 0; i < scenes.Count; i++)
                    if (scenes[i].path.Equals(scenePath, System.StringComparison.OrdinalIgnoreCase))
                    { scenes[i] = new EditorBuildSettingsScene(scenePath, true); break; }
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SceneSetupTool] Build Settings updated.");
    }

    // ── Helpers ───────────────────────────────────────────────

    private static void CreateCamera(Scene scene)
    {
        var camGO = new GameObject("Main Camera");
        SceneManager.MoveGameObjectToScene(camGO, scene);
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camGO.AddComponent<AudioListener>();
    }

    private static GameObject CreateCanvas(Scene scene, string name)
    {
        var go     = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject CreateChildStretch(GameObject parent, string name)
    {
        var go   = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, parent.scene);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject CreateButton(GameObject parent, Scene scene, string name, string label,
        Vector2 anchorCenter, Color color,
        float width = 320, float height = 75, float fontSize = 32)
    {
        var go   = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetParent(parent.transform, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var btn  = go.AddComponent<Button>();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin       = anchorCenter;
        rect.anchorMax       = anchorCenter;
        rect.sizeDelta       = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;

        // Rounded look via color block
        var colors = btn.colors;
        colors.normalColor      = color;
        colors.highlightedColor = color * 1.15f;
        colors.pressedColor     = color * 0.80f;
        btn.colors = colors;

        // Label
        var textGO   = new GameObject("Label");
        SceneManager.MoveGameObjectToScene(textGO, scene);
        textGO.transform.SetParent(go.transform, false);
        var tmp      = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        var tRect    = tmp.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;

        return go;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
