// ScoreUI.cs
using UnityEngine;
using TMPro;

public class TextTMP : MonoBehaviour
{
    TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStart += HandleGameStart;
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStart -= HandleGameStart;
    }

    void HandleGameStart()
    {
        tmp.enabled = true;
    }

    void HandleGameOver()
    { 
        tmp.enabled = false;
    }
}
