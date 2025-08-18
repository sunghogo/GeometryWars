using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestartButton : MonoBehaviour
{
    Button button;
    TMP_Text tmp;
    Image panel;

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!tmp) tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (!panel) panel = GetComponentInChildren<Image>();

        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStart += HandleGameStart;
        HandleGameStart();

        button.onClick.AddListener(() => GameManager.Instance.StartGame());
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStart -= HandleGameStart;
        if (button) button.onClick.RemoveAllListeners();
    }

    void HandleGameStart()
    {
        tmp.enabled = false;
        panel.enabled = false;
    }

    void HandleGameOver()
    {
        tmp.enabled = true;
        panel.enabled = true;
    }
}
