using UnityEngine;

/// <summary>
/// Pico Park-style camera: keeps all players in frame.
/// - Dynamically zooms out as players spread apart.
/// - Hard zoom limit (maxOrthoSize). At the limit, players cannot move further apart.
/// - Smoothly lerps toward target position and size every frame.
/// Attach to the Main Camera in the game level scenes.
/// </summary>
public class PicoParkCamera : MonoBehaviour
{
    public static PicoParkCamera Instance { get; private set; }

    [Header("Zoom Settings")]
    [SerializeField] private float _minOrthoSize  = 5f;
    [SerializeField] private float _maxOrthoSize  = 14f;
    [SerializeField] private float _padding        = 2f;
    [SerializeField] private float _smoothSpeed    = 5f;

    [Header("Level Bounds (world units)")]
    [SerializeField] private float _levelMinX = -20f;
    [SerializeField] private float _levelMaxX =  20f;
    [SerializeField] private float _levelMinY = -10f;
    [SerializeField] private float _levelMaxY =  10f;

    private Camera _cam;

    // Publicly readable boundary rect — PlayerBoundaryEnforcer reads this
    public Rect CurrentBounds { get; private set; }

    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
    }

    private void LateUpdate()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        // Compute bounding box of all players
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in players)
        {
            var pos = p.transform.position;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        // Target center
        float cx = Mathf.Clamp((minX + maxX) * 0.5f, _levelMinX, _levelMaxX);
        float cy = Mathf.Clamp((minY + maxY) * 0.5f, _levelMinY, _levelMaxY);
        Vector3 targetPos = new Vector3(cx, cy, transform.position.z);

        // Target ortho size (fit all players with padding)
        float aspect = _cam.aspect;
        float sizeFromWidth  = ((maxX - minX) * 0.5f + _padding) / aspect;
        float sizeFromHeight =  (maxY - minY) * 0.5f + _padding;
        float targetSize = Mathf.Clamp(Mathf.Max(sizeFromWidth, sizeFromHeight), _minOrthoSize, _maxOrthoSize);

        // Smooth lerp
        transform.position  = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * _smoothSpeed);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetSize, Time.deltaTime * _smoothSpeed);

        // Publish the current viewable world rect so PlayerBoundaryEnforcer can clamp players
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;
        Vector3 cp  = transform.position;
        CurrentBounds = new Rect(cp.x - halfW, cp.y - halfH, halfW * 2f, halfH * 2f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            new Vector3((_levelMinX + _levelMaxX) * 0.5f, (_levelMinY + _levelMaxY) * 0.5f, 0),
            new Vector3(_levelMaxX - _levelMinX, _levelMaxY - _levelMinY, 0));
    }
#endif
}
