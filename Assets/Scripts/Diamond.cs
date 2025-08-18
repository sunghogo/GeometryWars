using UnityEngine;

public class Diamond : Enemy
{
    [Header("ZigZag Settings")]
    [SerializeField] float forwardSpeed = 3f;      // how fast it advances
    [SerializeField] float zigzagAmplitude = 1f;   // sideways displacement
    [SerializeField] float zigzagFrequency = 2f;   // oscillations per second
    [SerializeField] float faceTurnSpeedDeg = 360f; // smooth facing speed

    float timeAlive = 0f;

    void Start()
    {
        moveSpeed = 4f;
    }

    protected override void Move()
    {
        timeAlive += Time.deltaTime;

        Vector2 playerPos = GameManager.Instance.PlayerPostion;
        Vector2 toPlayer  = (playerPos - (Vector2)transform.position);
        Vector2 forward   = toPlayer.normalized;

        // Get a perpendicular direction for sideways oscillation
        Vector2 side = new Vector2(-forward.y, forward.x);

        // Zigzag offset = sine wave along the side vector
        float offset = Mathf.Sin(timeAlive * Mathf.PI * zigzagFrequency) * zigzagAmplitude;

        // Combine forward + sideways
        Vector2 moveDir = (forward * forwardSpeed) + (side * offset);

        // Apply movement
        transform.position += (Vector3)(moveDir * Time.deltaTime);

        // Face toward the player (with smooth turn)
        float targetDeg = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0, 0, targetDeg);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRot, faceTurnSpeedDeg * Time.deltaTime
        );

        // Sync Direction field
        Direction = forward;
    }
}
