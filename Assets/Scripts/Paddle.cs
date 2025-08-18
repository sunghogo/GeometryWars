using UnityEngine;

public class Paddle : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] float speed = 10f;
    [field: SerializeField] public Vector2 Direction { get; private set; } = Vector2.zero;
    Rigidbody2D rb;
    SpriteRenderer sr;


    void ProcessInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            Direction = Vector2.left;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            Direction = Vector2.right;
        }
        else
        {
            Direction = Vector2.zero;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.GameStart) return;

        ProcessInput();
        transform.Translate(Direction * speed * Time.fixedDeltaTime, Space.World);
    }
    
    void Hide()
    {
        sr.enabled = false;
    }

    void Show()
    {
        sr.enabled = true;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GameManager.OnGameStart += Show;
        GameManager.OnGameOver += Hide;
    }

    void OnDestroy()
    {
        GameManager.OnGameStart -= Show;
        GameManager.OnGameOver -= Hide;
    }
}
