using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject ballPrefab;

    [Header("Properties")]
    [SerializeField] Vector3 startingPosition = new Vector3(0, -3, -1);

    void Awake()
    {
        GameManager.OnBricksCreated += HandleCreatedBricks;
    }

    void OnDestroy()
    {
        GameManager.OnBricksCreated -= HandleCreatedBricks;
    }

    void HandleCreatedBricks()
    {
        GameObject ball = Instantiate(ballPrefab, startingPosition, Quaternion.identity, transform);
    }

}
