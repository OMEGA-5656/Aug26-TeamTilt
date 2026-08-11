using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Level Select screen using UI Toolkit.
/// Host sees clickable level tiles (locked ones are greyed out).
/// Clients see the grid but with a "Waiting for host..." overlay.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LevelSelectUI : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _grid;
    private Label         _statusLabel;
    private VisualElement _clientOverlay;

    private void OnEnable()
    {
        // rootVisualElement is only valid from OnEnable onwards, NOT Awake
        var doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null)
        {
            Debug.LogError("[LevelSelectUI] UIDocument or rootVisualElement is null!");
            return;
        }

        _root          = doc.rootVisualElement;
        _grid          = _root.Q<VisualElement>("level-grid");
        _statusLabel   = _root.Q<Label>("status-label");
        _clientOverlay = _root.Q<VisualElement>("client-overlay");

        if (_grid == null)
            Debug.LogError("[LevelSelectUI] 'level-grid' VisualElement not found in UXML!");

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        Debug.Log($"[LevelSelectUI] OnEnable — IsServer={isHost}, grid found={_grid != null}");

        // Show/hide client overlay
        if (_clientOverlay != null)
            _clientOverlay.style.display = isHost ? DisplayStyle.None : DisplayStyle.Flex;

        BuildGrid(isHost);

        if (_statusLabel != null)
            _statusLabel.text = isHost ? "Pick a level to start!" : "Waiting for host to pick a level…";
    }

    private void BuildGrid(bool isHost)
    {
        if (_grid == null)
        {
            Debug.LogError("[LevelSelectUI] Cannot build grid — _grid is null.");
            return;
        }

        _grid.Clear();

        for (int i = 1; i <= LevelProgressManager.TotalLevels; i++)
        {
            bool unlocked = LevelProgressManager.Instance != null
                ? LevelProgressManager.Instance.IsUnlocked(i)
                : (i == 1); // fallback: level 1 always unlocked

            var tile = new VisualElement();
            tile.AddToClassList("level-tile");
            if (!unlocked) tile.AddToClassList("level-tile-locked");

            var num = new Label(i.ToString());
            num.AddToClassList("level-number");

            var lockIcon = new Label("🔒");
            lockIcon.AddToClassList("lock-icon");
            lockIcon.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;

            tile.Add(num);
            tile.Add(lockIcon);

            int captured = i;
            if (isHost && unlocked)
            {
                tile.RegisterCallback<ClickEvent>(_ => OnTileClicked(captured));
                tile.AddToClassList("level-tile-clickable");
            }

            _grid.Add(tile);
        }

        Debug.Log($"[LevelSelectUI] Grid built with {LevelProgressManager.TotalLevels} tiles.");
    }

    private void OnTileClicked(int level)
    {
        Debug.Log($"[LevelSelectUI] Host selected level {level}");
        if (_statusLabel != null) _statusLabel.text = $"Loading Level {level}…";
        LevelSelectManager.Instance?.HostSelectLevel(level);
    }
}
