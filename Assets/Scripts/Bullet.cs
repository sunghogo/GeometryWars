using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 3f;
    [SerializeField] LayerMask enemyLayer;

    [Tooltip("If your sprite is drawn pointing UP, set this to -90. If it's drawn pointing RIGHT, set 0.")]
    [SerializeField] float spriteFacingOffsetDeg = -90f;

    [field: SerializeField] public Vector2 Direction { get; private set; } = Vector2.up;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.GameOver) Destroy(gameObject);

        Vector2 move = Direction.normalized * speed * Time.fixedDeltaTime;
        Vector2 currentPos = transform.position;
        Vector2 nextPos = currentPos + move;

        CheckHit(currentPos, Direction, move.magnitude); // hit handled inside CheckHit

        transform.position = nextPos;
    }

    bool CheckHit(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, enemyLayer);

        if (hit.collider != null)
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Hit();
            }

            Destroy(gameObject);
            return true;
        }

        return false;
    }

    public void SetDirection(Vector2 dir)
    {
        Direction = dir.normalized;

        // Angle where 0° = +X (right)
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

        // Adjust for how the sprite is authored
        transform.rotation = Quaternion.Euler(0, 0, angle + spriteFacingOffsetDeg);
    }
}
