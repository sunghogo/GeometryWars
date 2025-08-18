using UnityEngine;

public class Brick : MonoBehaviour
{
    SpriteRenderer sr;
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            GameManager.Instance.DecrementBricks();
            GameManager.Instance.IncrementScore();
            Destroy(gameObject);
        }
    }

    void Delete()
    {
        Destroy(gameObject);
    }

    void Show()
    {
        sr.enabled = true;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GameManager.OnGameStart += Show;
        GameManager.OnGameOver += Delete;
    }

    void OnDestroy()
    {
        GameManager.OnGameStart -= Show;
        GameManager.OnGameOver -= Delete;
    }
}
