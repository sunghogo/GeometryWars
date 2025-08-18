using UnityEngine;
using UnityEngine.InputSystem;

public class AimCursor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerShip playerShip;
    [SerializeField] Camera mainCamera;

    [Header("General")]
    [SerializeField] float smooth = 20f;     // 0 = snap, higher = smoother
    [SerializeField] float worldZ = 0f;      // 2D plane z

    [Header("Stick Mode")]
    [SerializeField] float stickRadius = 2.5f;
    [SerializeField] float stickDeadzone = 0.2f;

    [Header("Mode")]
    [SerializeField] bool preferMouse = true;  // prefer mouse if present

    // Input System
    InputAction point;   // <Pointer>/position
    InputAction look;    // <Gamepad>/rightStick

    // Internals
    Camera cam;
    Vector3 targetPos;
    Vector3 playerPosThisFrame;  // snapshot of player world position (no rotation)
    Transform player;            // source for position only

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Hard decouple: ensure we are not a child of the ship or anything else
        if (transform.parent != null) transform.SetParent(null, true);

        cam = mainCamera ? mainCamera : Camera.main;

        point = new InputAction(type: InputActionType.Value, binding: "<Pointer>/position");
        look  = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/rightStick");
        point.Enable(); look.Enable();

        targetPos = transform.position;

        // Lock our own rotation (pure cosmetic)
        transform.rotation = Quaternion.identity;

        GameManager.OnGameOver += Hide;
        GameManager.OnGameStart += Show;
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= Hide;
        GameManager.OnGameStart -= Show;
    }

    void Hide()
    {
        sr.enabled = false;
    }

    void Show()
    {
        sr.enabled = true;
    }

    void Start()
    {
        if (playerShip) player = playerShip.transform;
    }

    void OnDisable()
    {
        point.Disable();
        look.Disable();
    }

    void Update()
    {
        // Snapshot the player's world position ONCE (position only; rotation doesn’t matter)
        if (player) playerPosThisFrame = player.position;

        bool useMouseNow = preferMouse && Mouse.current != null;

        if (useMouseNow)
        {
            // --- Mouse (OS cursor) ---
            Vector2 screen = point.ReadValue<Vector2>();
            float depth = GetDepthForWorldPlane(cam, worldZ);
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            wp.z = worldZ;                 // force onto our plane
            targetPos = wp;
        }
        else
        {
            // --- Gamepad (virtual cursor) ---
            Vector2 aim = look.ReadValue<Vector2>();
            if (aim.sqrMagnitude < stickDeadzone * stickDeadzone)
                aim = Vector2.zero;
            else
                aim = aim.normalized;

            Vector3 center = player ? playerPosThisFrame : transform.position;
            targetPos = center + (Vector3)(aim * stickRadius);
            targetPos.z = worldZ;          // keep on our plane
        }

        // Smoothly move cursor in WORLD space (no parent influence)
        if (smooth <= 0f)
            transform.position = targetPos;
        else
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        // Keep rotation fixed so it never spins with any parent changes
        transform.rotation = Quaternion.identity;
    }

    static float GetDepthForWorldPlane(Camera c, float planeZ)
    {
        if (c.orthographic) return Mathf.Abs(c.transform.position.z - planeZ);
        float dz = planeZ - c.transform.position.z;
        float denom = Vector3.Dot(c.transform.forward, Vector3.forward);
        return Mathf.Approximately(denom, 0f) ? dz : dz / denom;
    }
}
