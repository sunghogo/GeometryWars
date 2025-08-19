using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShip : MonoBehaviour
{
    [Header("Refs)")]
    [SerializeField] AimCursor aimCursor;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] AudioClip laser1Clip;
    [SerializeField] AudioClip laser2Clip;
    [SerializeField] AudioClip explosionClip;


    [field: Header("Movement")]
    [field: SerializeField] float moveSpeed = 5f;
    [field: SerializeField] Vector3 startingPositon;
    [field: SerializeField] public Vector2 Direction { get; private set; } = Vector2.up;
    [field: SerializeField] public Vector2 Movement { get; private set; } = Vector2.zero;


    [Header("Turning")]
    [SerializeField] float turnSpeedDegPerSec = 720f;
    [SerializeField] bool faceDirection = true;
    [SerializeField] float facingOffsetDeg = -90f;

    [Header("Shooting")]
    [SerializeField] float fireCooldown = 0.2f;

    [Header("iFrame")]
    [SerializeField] float hitCooldown = 0.5f;

    Rigidbody2D rb;
    AudioSource audioSource;
    InputAction moveAction;
    InputAction fireAction;
    SpritePulseAndHit spritePulseAndHit;
    float timer = 0f;
    bool hit;
    float hitTimer = 0f;

    void Awake()
    {
        startingPositon = transform.position;
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.75f;
        spritePulseAndHit = GetComponent<SpritePulseAndHit>();
        InitializeInput();

        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStart += HandleGameStart;
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStart -= HandleGameStart;
    }

    void OnEnable()
    {
        moveAction.Enable();
        fireAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        fireAction.Disable();
    }

    void HandleGameOver()
    {
        AudioHelper.PlayClipAtPoint(explosionClip, transform.position, 0.75f);
        Direction = Vector2.up;
        Movement = Vector2.zero;
        transform.position = startingPositon;
        gameObject.SetActive(false);
    }

    void HandleGameStart()
    {
        gameObject.SetActive(true);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (!hit)
            {
                spritePulseAndHit.TriggerHitFlash();
                GameManager.Instance.DecrementLives();
                hit = true;
            }
            collision.gameObject.GetComponent<Enemy>().Hit();
        }
    }

    void Update()
    {
        Movement = moveAction.ReadValue<Vector2>().normalized;

        // Fire if pressed & cooldown expired
        if (fireAction.IsPressed() && timer >= fireCooldown)
        {
            Shoot();
            timer = 0f;
        }
        timer += Time.deltaTime;

        // Hit invicibility
        if (hitTimer >= hitCooldown)
        {
            hitTimer = 0f;
            hit = false;
        }
        if (hit)
        {
            hitTimer += Time.deltaTime;
        }

        GameManager.Instance.SetPlayerPosition(transform.position);
    }

    void FixedUpdate()
    {
        UpdateDirectionAndRotation();
        rb.MovePosition(rb.position + Movement * moveSpeed * Time.fixedDeltaTime);
    }

    void InitializeInput()
    {
        // Movement (Gamepad stick + WASD + Arrows)
        moveAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");

        // WASD composite
        var wasd = moveAction.AddCompositeBinding("2DVector");
        wasd.With("Up", "<Keyboard>/w");
        wasd.With("Down", "<Keyboard>/s");
        wasd.With("Left", "<Keyboard>/a");
        wasd.With("Right", "<Keyboard>/d");

        // Arrow-keys composite
        var arrows = moveAction.AddCompositeBinding("2DVector");
        arrows.With("Up", "<Keyboard>/upArrow");
        arrows.With("Down", "<Keyboard>/downArrow");
        arrows.With("Left", "<Keyboard>/leftArrow");
        arrows.With("Right", "<Keyboard>/rightArrow");

        // Fire (left mouse button + gamepad south button)
        fireAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        fireAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void UpdateDirectionAndRotation()
    {
        if (aimCursor == null) return;

        Vector2 toCursor = (Vector2)(aimCursor.transform.position - transform.position);
        if (toCursor.sqrMagnitude < 1e-6f) return; // cursor on top of player -> keep current Direction

        Vector2 targetDir = toCursor.normalized;

        // If Direction is zero, snap once to avoid RotateTowards zero-vector trap
        if (Direction.sqrMagnitude < 1e-6f)
        {
            Direction = targetDir;
        }
        else
        {
            float maxRad = turnSpeedDegPerSec * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 newDir3 = Vector3.RotateTowards(
                new Vector3(Direction.x, Direction.y, 0f),
                new Vector3(targetDir.x, targetDir.y, 0f),
                maxRad,
                0f
            );
            Direction = new Vector2(newDir3.x, newDir3.y).normalized;
        }

        if (faceDirection)
        {
            float ang = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg + facingOffsetDeg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }
    
    void Shoot()
    {
        if (!bulletPrefab) return;

        int bit =  Random.Range(0, 2);
        if (bit == 0) audioSource.PlayOneShot(laser1Clip);
        else audioSource.PlayOneShot(laser2Clip);

        // Spawn the bullet at the ship’s position, facing the current Direction
        GameObject bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.identity
        );

        // Use your tracked facing Direction (normalized)
        bullet.GetComponent<Bullet>().SetDirection(Direction.normalized);
    }
}

