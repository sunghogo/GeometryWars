using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] AudioClip breakingClip;

    [Header("Properties")]
    [SerializeField] float startingSpeed = 7.5f;
    [SerializeField] float maxPaddleReflection = 60f;
    [SerializeField] float minObstacleReflection = 30f;
    [SerializeField] float speedUpScale = 2f;
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public Vector2 Direction { get; private set; }
    Rigidbody2D rb;
    SpriteRenderer sr;
    AudioSource audioSource;
    Vector3 startingPosition;
    float brickRatio;

    void SetRandomDirection()
    {
        float randomX = Random.Range(-maxPaddleReflection / 90f, maxPaddleReflection / 90f);
        float randomY = Random.Range(0.5f, 1f);
        Direction = new Vector2(randomX, randomY).normalized;
    }

    void ResetPositionDirection()
    {
        transform.position = startingPosition;
        SetRandomDirection();
    }

    void ResetSpeed()
    {
        Speed = startingSpeed;
    }

    Vector2 ObstacleReflect(Vector2 v, Vector2 n)
    {
        // Normal reflection
        Vector2 reflection = v - 2f * Vector2.Dot(v, n) * n;
        reflection.Normalize();

        // Clamp the outgoing angle
        float angle = Mathf.Atan2(reflection.y, reflection.x) * Mathf.Rad2Deg;

        // If too close to horizontal (0°), push it up to ±30°
        if (angle > -minObstacleReflection && angle < minObstacleReflection)
        {
            angle = Mathf.Sign(angle == 0 ? 1 : angle) * minObstacleReflection;
        }
        // If too close to horizontal (±180°), push it down to ±150°
        else if (angle > 180f - minObstacleReflection || angle < -180f + minObstacleReflection)
        {
            angle = Mathf.Sign(angle) * (180f - minObstacleReflection);
        }

        // Rebuild vector from clamped angle
        reflection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                Mathf.Sin(angle * Mathf.Deg2Rad));

        return reflection.normalized;
    }

    Vector2 PaddleReflect(Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 n = contact.normal;

        // If we hit the underside (n.y <= 0) or the paddle's side, do a normal reflect.
        if (n.y <= 0f || Mathf.Abs(n.x) > 0.5f)
        {
            return ObstacleReflect(Direction, n);
        }

        Vector2 hitPoint = collision.GetContact(0).point;
        float paddleCenterX = collision.collider.bounds.center.x;
        float halfWidth = collision.collider.bounds.extents.x;

        // Offset in [-1,1] across the paddle
        float offset = (hitPoint.x - paddleCenterX) / halfWidth;

        // Max angle away from vertical (in degrees)
        float maxAngle = maxPaddleReflection;

        // Convert offset to angle
        float angle = offset * maxAngle * Mathf.Deg2Rad;

        // Build new direction relative to "up"
        return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.GetContact(0).normal;

        if (collision.gameObject.CompareTag("Paddle"))
        {
            Direction = PaddleReflect(collision).normalized;
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            Direction = ObstacleReflect(Direction, normal).normalized;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bottom"))
        {
            GameManager.Instance.DecrementLives();
            ResetPositionDirection();
        }
    }

    void Awake()
    {
        startingPosition = transform.position;
        Speed = startingSpeed;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.75f;

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        SetRandomDirection();

        GameManager.OnBricksCreated += HandleCreatedBricks;
        GameManager.OnBrickDestroyed += HandleDestroyedBrick;
        GameManager.OnGameStart += Show;
        GameManager.OnGameOver += Hide;
    }

    void OnDestroy()
    {
        GameManager.OnBricksCreated -= HandleCreatedBricks;
        GameManager.OnBrickDestroyed -= HandleDestroyedBrick;
        GameManager.OnGameStart -= Show;
        GameManager.OnGameOver -= Hide;
    }

    void Start()
    {
        brickRatio = 1f;
        ResetPositionDirection();
        ResetSpeed();
        if (GameManager.Instance.ChangeLevel) audioSource.PlayOneShot(breakingClip);
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.GameStart) return;

        transform.Translate(Direction * Speed * Time.fixedDeltaTime, Space.World);
    }

    void HandleCreatedBricks() // Need to respawn ball due to unfixable Direction starting downward on resetting 
    {
        Destroy(gameObject);
    }

    void HandleDestroyedBrick()
    {
        audioSource.PlayOneShot(breakingClip);
        brickRatio = GameManager.Instance.currentBricks / GameManager.Instance.totalBricks;
        float multiplier = Mathf.Lerp(1f, 0.5f, brickRatio);
        Speed = startingSpeed * speedUpScale * multiplier;
    }

    void Hide()
    {
        sr.enabled = false;
    }

    void Show()
    {
        sr.enabled = true;
    }

}
