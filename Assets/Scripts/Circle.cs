using UnityEngine;

public class Circle : Enemy
{
    [Header("Spiral Settings")]
    [SerializeField] float angularSpeedDeg = 180f;  // slower orbit
    [SerializeField] float inwardSpeed     = 0.5f;   // shrink radius per second
    [SerializeField] float minRadius       = 0.25f; // stop shrinking near center

    [Header("Facing")]
    [SerializeField] float faceTurnSpeedDeg = 360f; // cap sprite turn speed

    float angle;   // radians
    float radius;  // world units
    bool  initialized;

    void Start()
    {
        moveSpeed = 8f;
    } 

    protected override void Move()
    {
        Vector2 playerPos = GameManager.Instance.PlayerPostion;
        Vector2 toPlayer = playerPos - (Vector2)transform.position;

        // Initialize once from current relative position
        if (!initialized)
        {
            radius = Mathf.Max(toPlayer.magnitude, minRadius);
            angle = Mathf.Atan2(toPlayer.y, toPlayer.x);
            initialized = true;
        }

        // Spiral dynamics
        float dt = Time.deltaTime;

        // Optionally scale orbit speed by distance so it doesn't feel frantic near the center
        float dist01 = Mathf.Clamp01(radius / 5f); // 5 = reference distance
        float omega = Mathf.Deg2Rad * angularSpeedDeg * Mathf.Lerp(0.4f, 1f, dist01);

        angle += omega * dt;
        radius = Mathf.Max(radius - inwardSpeed * dt, minRadius);

        // New position around player
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector2 newPos = playerPos + offset;

        // Move
        transform.position = newPos;

        // Face toward the player with a capped turn rate
        Vector2 desiredDir = (playerPos - newPos).normalized;
        float targetDeg = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;

        Quaternion targetRot = Quaternion.Euler(0, 0, targetDeg);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRot, faceTurnSpeedDeg * dt
        );

        // Keep Direction in sync if you use it elsewhere
        Direction = desiredDir;
    }
}
