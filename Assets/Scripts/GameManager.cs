using UnityEngine;
using System;

public enum GameState
{
    StartingScreen,
    GameStart,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnHighScoreChanged;
    public static event Action<int> OnLivesChanged;
    public static event Action OnNextLevel;
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnScreenStart;


    [field: Header("Game States")]
    [field: SerializeField] public bool StartingScreen { get; private set; }
    [field: SerializeField] public bool GameStart { get; private set; }
    [field: SerializeField] public bool GameOver { get; private set; }
    [field: SerializeField] public bool ChangeLevel { get; private set; } = false;

    [field: Header("Properties")]
    [field: SerializeField] public int StartingLives { get; private set; } = 3;

    [field: Header("Shared Data")]
    [field: SerializeField] public Vector3 PlayerPostion { get; private set; }
    [field: SerializeField] public float MinX { get; private set; } = 0;
    [field: SerializeField] public float MaxX { get; private set; } = 0;
    [field: SerializeField] public float MinY { get; private set; } = 0;
    [field: SerializeField] public float MaxY { get; private set; } = 0;
    [field: SerializeField] public int Score { get; private set; } = 0;
    [field: SerializeField] public int HighScore { get; private set; } = 0;
    [field: SerializeField] public int Lives { get; private set; } = 3;


    public void SetPlayerPosition(Vector3 newPosition) {
        PlayerPostion = newPosition;
    }

    public void IncrementScore()
    {
        ++Score;
        OnScoreChanged?.Invoke(Score);
    }

    public void ResetScore()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }

    void UpdateHighScore()
    {
        HighScore = Score;
        OnHighScoreChanged?.Invoke(HighScore);
    }

    public void DecrementLives()
    {
        Lives = Mathf.Max(0, Lives - 1);
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0) EndGame();
    }

    public void ResetLives()
    {
        Lives = StartingLives;
    }

    public void StartGame()
    {
        StartingScreen = false;
        GameStart = true;
        GameOver = false;
        ChangeLevel = false;
        ResetLives();
        ResetScore();
        OnGameStart?.Invoke();
        OnLivesChanged?.Invoke(Lives);
        OnScoreChanged?.Invoke(Score);
        OnHighScoreChanged?.Invoke(HighScore);
    }

    public void EndGame()
    {
        StartingScreen = false;
        GameStart = false;
        GameOver = true;
        ChangeLevel = false;

        if (Score > HighScore)
        {
            UpdateHighScore();
        }
        OnGameOver?.Invoke();
    }

    public void StartScreen()
    {
        StartingScreen = true;
        GameStart = false;
        GameOver = false;
        ChangeLevel = false;
        OnScreenStart?.Invoke();
    }

    public void NextLevel()
    {
        ChangeLevel = true;
        OnNextLevel?.Invoke();
    }

    public void SetMinMaxXY(GameObject obj)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        Bounds bounds = spriteRenderer.bounds;
        MinX = bounds.min.x;
        MaxX = bounds.max.x;
        MinY = bounds.min.y;
        MaxY = bounds.max.y;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResetLives();
    }

    void Start()
    {
        Instance.StartGame();   
    }
}
