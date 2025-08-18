// ScoreUI.cs
using UnityEngine;
using TMPro;

public class GameOverTMP : MonoBehaviour
{
    TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        if (GameManager.Instance) tmp.text = GameManager.Instance.Score.ToString();
        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStart += HandleGameStart;
        HandleGameStart();
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStart -= HandleGameStart;
    }

    void HandleGameStart()
    {
        tmp.enabled = false;
    }

    void HandleGameOver()
    { 
        tmp.enabled = true;
    }
}
