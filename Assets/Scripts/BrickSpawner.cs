using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject brickPrefab;

    [Header("Properties")]
    [SerializeField] int startingRows = 3;
    [SerializeField] int maxRows = 8;
    [SerializeField] float gapWidth = 0.25f;
    [SerializeField, Range(0, 1f)] float startingYScale = 0.8f;
    [SerializeField, Range(0, 1f)] float startingXScale = 0.95f;
    [SerializeField, Range(0, 1f)] float brickSaturation = 0.75f;
    [SerializeField, Range(0, 1f)] float brickValue = 0.9f;
    int rows;

    
    void IncrementRows()
    {
        rows = Mathf.Min(maxRows, rows + 1);
    }

    void ResetRows()
    {
        rows = startingRows;
    }

    void SpawnBricks()
    {
        // World bounds from GameManager
        float minX = GameManager.Instance.minX * startingXScale;
        float maxX = GameManager.Instance.maxX * startingXScale;
        float minY = GameManager.Instance.minY;
        float maxY = GameManager.Instance.maxY;

        // Starting Y is 75% up the playable area
        float startingY = (maxY - minY) * startingYScale + minY;

        // Brick dimensions
        Vector2 brickSize = brickPrefab.GetComponent<SpriteRenderer>().bounds.size;
        float brickW = brickSize.x;
        float brickH = brickSize.y;

        // How many bricks fit in one row
        float availableWidth = (maxX - minX);
        int columns = Mathf.FloorToInt((availableWidth + gapWidth) / (brickW + gapWidth));
        columns = Mathf.Max(columns, 1);

        // Center the grid horizontally
        float totalGridWidth = columns * brickW + (columns - 1) * gapWidth;
        float startX = minX + (availableWidth - totalGridWidth) * 0.5f + brickW * 0.5f;

        // Spawn grid of bricks
        for (int r = 0; r < rows; r++)
        {
            float y = startingY - r * (brickH + gapWidth);
            Color rowColor = Color.HSVToRGB((float)r / rows, brickSaturation, brickValue);
            for (int c = 0; c < columns; c++)
            {
                float x = startX + c * (brickW + gapWidth);
                Vector3 pos = new Vector3(x, y, brickPrefab.transform.position.z);
                GameObject brick = Instantiate(brickPrefab, pos, Quaternion.identity, transform);
                SpriteRenderer sr = brick.GetComponent<SpriteRenderer>();
                sr.color = rowColor;
            }
        }

        GameManager.Instance.UpdateBricksNumber(rows * columns);
    }

    void Awake()
    {
        ResetRows();
        GameManager.OnGameStart += HandleGameStart;
        GameManager.OnNextLevel += HandleNextLevel;
    }

    void OnDestroy()
    {
        GameManager.OnGameStart -= HandleGameStart;
        GameManager.OnNextLevel -= HandleNextLevel;
    }

    void HandleGameStart()
    {
        ResetRows();
        SpawnBricks();
    }

    void HandleNextLevel()
    {
        IncrementRows();
        SpawnBricks();
    }
}
