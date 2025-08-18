using UnityEngine;

public class Enemy : MonoBehaviour
{

    [field: Header("Movement")]
    [field: SerializeField] protected float moveSpeed = 5f;
    [field: SerializeField] protected float rotationSpeed = 360f;
    [field: SerializeField] public Vector2 Direction { get; protected set; } = Vector2.up;
    [field: SerializeField] public Vector2 Movement { get; protected set; } = Vector2.zero;

    void FixedUpdate()
    {
        if (GameManager.Instance.GameOver) Destroy(gameObject);

        Vector2 playerPosition = GameManager.Instance.PlayerPostion;
        Vector2 toPlayer = playerPosition - (Vector2)transform.position;

        if (toPlayer.sqrMagnitude < 0.001f) return;

        Direction = toPlayer.normalized;
        RotateTowards(Direction);
        Move();
    }

    void RotateTowards(Vector2 direction)
    {
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    protected virtual void Move()
    {
        Movement = Direction * moveSpeed * Time.fixedDeltaTime;
        transform.position += (Vector3)Movement;
    }

    public void Hit()
    {
        GameManager.Instance.IncrementScore();
        Destroy(gameObject);
    }
}
