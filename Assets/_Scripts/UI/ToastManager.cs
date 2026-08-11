using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Singleton toast notification manager. DontDestroyOnLoad.
/// Creates its UI panel entirely at runtime — no UXML asset required.
/// Call ToastManager.Show("message") from anywhere.
/// </summary>
public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    private VisualElement _container;
    private bool          _ready;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Build the UIDocument + root container at runtime
        var doc = gameObject.AddComponent<UIDocument>();

        // Find PanelSettings from scene
        var ps = FindAnyObjectByType<PanelSettings>();
        if (ps == null)
        {
            // Try loading from Resources
            ps = Resources.Load<PanelSettings>("DefaultPanelSettings");
        }
        if (ps != null) doc.panelSettings = ps;
        doc.sortingOrder = 50; // On top of everything

        // Build container
        var root = doc.rootVisualElement;
        _container               = new VisualElement();
        _container.name          = "toast-container";
        _container.style.position       = Position.Absolute;
        _container.style.bottom         = 80;
        _container.style.left           = 0;
        _container.style.right          = 0;
        _container.style.alignItems     = Align.Center;
        _container.style.flexDirection  = FlexDirection.ColumnReverse;
        _container.pickingMode          = PickingMode.Ignore;
        root.Add(_container);
        _ready = true;
    }

    // ── Public API ─────────────────────────────────────────────

    public static void Show(string message, float duration = 3f)
    {
        if (Instance == null) return;
        // If not ready yet, retry in 0.1s
        if (!Instance._ready) { Instance.StartCoroutine(Instance.ShowDelayed(message, duration)); return; }
        Instance.StartCoroutine(Instance.ShowToast(message, duration));
    }

    private IEnumerator ShowDelayed(string message, float duration)
    {
        yield return new WaitUntil(() => _ready);
        StartCoroutine(ShowToast(message, duration));
    }

    private IEnumerator ShowToast(string message, float duration)
    {
        var toast = new VisualElement();
        toast.style.backgroundColor         = new StyleColor(new Color(0.12f, 0.12f, 0.12f, 0.90f));
        toast.style.borderTopLeftRadius     = 10;
        toast.style.borderTopRightRadius    = 10;
        toast.style.borderBottomLeftRadius  = 10;
        toast.style.borderBottomRightRadius = 10;
        toast.style.paddingLeft   = 22;
        toast.style.paddingRight  = 22;
        toast.style.paddingTop    = 12;
        toast.style.paddingBottom = 12;
        toast.style.marginBottom  = 8;
        toast.pickingMode = PickingMode.Ignore;

        var label = new Label(message);
        label.style.color                       = Color.white;
        label.style.fontSize                    = 15;
        label.style.unityFontStyleAndWeight     = FontStyle.Bold;
        label.style.unityTextAlign              = TextAnchor.MiddleCenter;
        label.pickingMode                       = PickingMode.Ignore;

        toast.Add(label);
        _container.Add(toast);

        // Visible phase
        yield return new WaitForSecondsRealtime(duration - 0.5f);

        // Fade out
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            toast.style.opacity = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }

        _container.Remove(toast);
    }
}
