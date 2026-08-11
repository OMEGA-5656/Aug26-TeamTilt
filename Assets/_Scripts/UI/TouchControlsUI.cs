using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Touch controls overlay. Uses VisualElement (not Button) for reliable held-state tracking.
/// Auto-shows on Android; optional on Windows via Settings toggle.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class TouchControlsUI : MonoBehaviour
{
    private const string PrefKey = "teamtilt_touch_controls_enabled";

    private VisualElement _leftEl, _rightEl, _jumpEl, _dashEl;

    private bool _leftHeld, _rightHeld, _jumpHeld;
    private bool _dashQueued; // Survives one FixedUpdate cycle

    private PlayerInputHandler _input;
    private int _dashFrameQueued = -1;

    // ── Lifecycle ──────────────────────────────────────────────

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;

        // UI Toolkit builds async; defer registration
        doc.rootVisualElement.schedule.Execute(Setup).ExecuteLater(50);
    }

    private void Setup()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return;

        var root = doc.rootVisualElement;
        bool show = ShouldShowTouchControls();

        var touchRoot = root.Q<VisualElement>("touch-root");
        if (touchRoot != null)
        {
            touchRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Constrain mobile controls to Device Safe Area
            Rect safeArea = Screen.safeArea;
            touchRoot.style.paddingLeft = Mathf.Max(24, safeArea.xMin);
            touchRoot.style.paddingRight = Mathf.Max(24, Screen.width - safeArea.xMax);
            touchRoot.style.paddingBottom = Mathf.Max(20, Screen.height - safeArea.yMax);
        }

        if (!show) return;

        _leftEl  = root.Q<VisualElement>("btn-left");
        _rightEl = root.Q<VisualElement>("btn-right");
        _jumpEl  = root.Q<VisualElement>("btn-jump");
        _dashEl  = root.Q<VisualElement>("btn-dash");

        Register(_leftEl,  OnLeftDown,  OnLeftUp);
        Register(_rightEl, OnRightDown, OnRightUp);
        Register(_jumpEl,  OnJumpDown,  OnJumpUp);
        Register(_dashEl,  OnDashDown,  null);
    }

    private void OnDisable()
    {
        _leftHeld = _rightHeld = _jumpHeld = false;
        _dashQueued = false;
    }

    // ── Event Handlers ─────────────────────────────────────────

    private void OnLeftDown()  { _leftHeld  = true; }
    private void OnLeftUp()    { _leftHeld  = false; }
    private void OnRightDown() { _rightHeld = true; }
    private void OnRightUp()   { _rightHeld = false; }

    private void OnJumpDown()
    {
        _jumpHeld = true;
        _input?.VirtualJumpDown();
    }
    private void OnJumpUp()
    {
        _jumpHeld = false;
        _input?.VirtualJumpUp();
    }

    private void OnDashDown()
    {
        // Queue dash to survive until next FixedUpdate reads it
        _dashQueued      = true;
        _dashFrameQueued = Time.frameCount;
    }

    // ── Update Loop ────────────────────────────────────────────

    private void Update()
    {
        if (_input == null) FindLocalPlayer();
        if (_input == null) return;

        // Push movement every frame
        float x = (_rightHeld ? 1f : 0f) - (_leftHeld ? 1f : 0f);
        _input.SetVirtualMove(new Vector2(x, 0f));

        // Push queued dash (keep it alive for 2 frames to bridge FixedUpdate gaps)
        if (_dashQueued)
        {
            _input.VirtualDash();
            if (Time.frameCount > _dashFrameQueued + 1)
                _dashQueued = false;
        }
    }

    // ── Helper: Register ───────────────────────────────────────

    private static void Register(VisualElement el,
        System.Action onDown, System.Action onUp)
    {
        if (el == null) return;

        el.RegisterCallback<PointerDownEvent>(evt =>
        {
            el.CapturePointer(evt.pointerId); // ensures we get PointerUp even if finger leaves
            onDown?.Invoke();
            evt.StopPropagation();
        });

        if (onUp != null)
        {
            el.RegisterCallback<PointerUpEvent>(evt =>
            {
                el.ReleasePointer(evt.pointerId);
                onUp.Invoke();
                evt.StopPropagation();
            });

            el.RegisterCallback<PointerCancelEvent>(evt =>
            {
                el.ReleasePointer(evt.pointerId);
                onUp.Invoke();
            });
        }
    }

    // ── Player Finder ──────────────────────────────────────────

    private void FindLocalPlayer()
    {
        var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
        foreach (var h in handlers)
        {
            var net = h.GetComponent<NetworkObject>();
            if (net == null || net.IsOwner)
            {
                _input = h;
                return;
            }
        }
    }

    // ── Static Helpers ─────────────────────────────────────────

    public static bool ShouldShowTouchControls()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return PlayerPrefs.GetInt(PrefKey, 0) == 1;
#endif
    }

    public static void SetTouchControlsEnabled(bool value)
    {
        PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool GetTouchControlsEnabled()
        => PlayerPrefs.GetInt(PrefKey, 0) == 1;
}
