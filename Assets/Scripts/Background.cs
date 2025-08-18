using UnityEngine;

public class Background : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.SetMinMaxXY(gameObject);
    }
}
