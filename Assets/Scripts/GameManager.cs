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

    public static event Action OnBricksCreated;
    public static event Action OnBrickDestroyed;
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
    [field: SerializeField] public float minX { get; private set; } = 0;
    [field: SerializeField] public float maxX { get; private set; } = 0;
    [field: SerializeField] public float minY { get; private set; } = 0;
    [field: SerializeField] public float maxY { get; private set; } = 0;
    [field: SerializeField] public float totalBricks { get; private set; } = 0;
    [field: SerializeField] public float currentBricks { get; private set; } = 0;
    [field: SerializeField] public int Score { get; private set; } = 0;
    [field: SerializeField] public int HighScore { get; private set; } = 0;
    [field: SerializeField] public int Lives { get; private set; }

    public void UpdateBricksNumber(int n)
    {
        totalBricks = n;
        currentBricks = totalBricks;
        OnBricksCreated?.Invoke();
    }

    public void DecrementBricks()
    {
        --currentBricks;
        OnBrickDestroyed?.Invoke();
        if (currentBricks == 0) Instance.NextLevel();
    }

    public void NextLevel()
    {
        ChangeLevel = true;
        OnNextLevel?.Invoke();
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
        OnGameStart?.Invoke();
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
        ResetScore();
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

    public void SetMinMaxXY(GameObject obj)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        Bounds bounds = spriteRenderer.bounds;
        minX = bounds.min.x;
        maxX = bounds.max.x;
        minY = bounds.min.y;
        maxY = bounds.max.y;
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
    }

    void Start()
    {
        Instance.StartGame();   
    }
}
