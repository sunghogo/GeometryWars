using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] GameObject square;
    [SerializeField] GameObject diamond;
    [SerializeField] GameObject circle;

    [Header("Spawn Interval (log curve)")]
    [SerializeField] float baseInterval = 3f;
    [SerializeField] float curveGain = -0.33f; // e.g., 5 min ramp with base=3, min=0.25, logBase=2
    [SerializeField] float logBase = 2f;
    [SerializeField] float minInterval = 1f;
    [SerializeField] float maxInterval = 10f;

    [Header("Spawn Area (local space)")]
    [SerializeField] Vector2 areaSize = new Vector2(12f, 7f);
    [SerializeField] bool spawnOnEdges = true;
    [SerializeField] float edgeInset = 0.25f;

    [Header("Burst Spawning")]
    [SerializeField] int   maxBurstCount    = 3;
    [SerializeField] float burstBaseChance  = 0.10f; // at t≈0
    [SerializeField] float burstGain        = 0.05f; // grows with log(t)

    [Header("Multi-Spawner Coordination")]
    [Tooltip("Probability this spawner actually fires on its tick. Set 0.25 when you have 4 spawners.")]
    [Range(0f,1f)] [SerializeField] float triggerChance = 0.25f;
    [Tooltip("Randomize initial next spawn to avoid perfect sync across spawners.")]
    [SerializeField] bool randomizeInitialPhase = true;

    float nextSpawnTime;
    GameObject[] pool;
    
    float elapsed;         
    float nextSpawnAtLocal; // local 'elapsed' timestamp for next spawn

    void Awake()
    {
        pool = new[] { square, diamond, circle };
        ScheduleNextSpawn(0f);

        if (randomizeInitialPhase)
            nextSpawnTime += Random.Range(0f, baseInterval);

        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStart += HandleGameStart;

    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStart -= HandleGameStart;
    }

    void ResetProgression(bool randomizePhase)
    {
        elapsed = 0f;
        ScheduleNextSpawn(elapsed);
        if (randomizePhase)
            nextSpawnAtLocal += Random.Range(0f, baseInterval);
    }

    void HandleGameOver()
    {
        ResetProgression(false);
    }

    void HandleGameStart()
    {
        ResetProgression(true); // reset curve + schedule for a fresh run
    }

    void Update()
    {
        if (GameManager.Instance.GameOver) return;

        elapsed += Time.deltaTime;

        if (elapsed >= nextSpawnAtLocal)
        {
            if (Random.value < triggerChance)
                SpawnBurst(elapsed);

            ScheduleNextSpawn(elapsed);
        }
    }

    // interval(t) = clamp( baseInterval + curveGain * log_base(t+1), minInterval, maxInterval )
    void ScheduleNextSpawn(float tLocal)
    {
        float interval = Mathf.Clamp(ComputeLogCurve(baseInterval, curveGain, tLocal), minInterval, maxInterval);
        nextSpawnAtLocal = tLocal + interval; // << key change
    }

    void SpawnBurst(float tLocal)
    {
        int count = 1;
        float chance = Mathf.Clamp01(ComputeLogCurve(burstBaseChance, burstGain, tLocal));
        if (Random.value < chance)
            count = Random.Range(2, maxBurstCount + 1);

        for (int i = 0; i < count; i++)
            SpawnOne();
    }

    void SpawnOne()
    {
        var prefab = RandomPick(pool);
        if (!prefab) return;

        // Pick a LOCAL position within (or on the edges of) areaSize
        Vector2 posLocal = spawnOnEdges
            ? RandomPointOnLocalEdge(areaSize, edgeInset)
            : RandomPointInLocalArea(areaSize, edgeInset);

        // Instantiate AS A CHILD of the spawner, then place via localPosition
        var go = Instantiate(prefab, transform);
        go.transform.localPosition = posLocal;
        go.transform.localRotation = Quaternion.identity; // optional
        // (scale will inherit the spawner's local scale)
    }

    GameObject RandomPick(GameObject[] arr)
    {
        int tries = 0;
        while (tries < arr.Length)
        {
            var g = arr[Random.Range(0, arr.Length)];
            if (g != null) return g;
            tries++;
        }
        return null;
    }

    float ComputeLogCurve(float baseVal, float gain, float t)
    {
        float shifted = Mathf.Max(t + 1f, 1.0001f);
        float denom   = Mathf.Max(logBase, 1.0001f);
        float logt    = Mathf.Log(shifted) / Mathf.Log(denom);
        return baseVal + gain * logt;
    }

    // --- Local-space spawn helpers (box centered at origin) ---
    static Vector2 RandomPointInLocalArea(Vector2 size, float inset)
    {
        float hx = Mathf.Max(size.x * 0.5f - inset, 0f);
        float hy = Mathf.Max(size.y * 0.5f - inset, 0f);
        return new Vector2(Random.Range(-hx, hx), Random.Range(-hy, hy));
    }

    static Vector2 RandomPointOnLocalEdge(Vector2 size, float inset)
    {
        float hx = Mathf.Max(size.x * 0.5f - inset, 0f);
        float hy = Mathf.Max(size.y * 0.5f - inset, 0f);
        switch (Random.Range(0, 4))
        {
            case 0:  return new Vector2(-hx, Random.Range(-hy, hy)); // left
            case 1:  return new Vector2( hx, Random.Range(-hy, hy)); // right
            case 2:  return new Vector2(Random.Range(-hx, hx), -hy); // bottom
            default: return new Vector2(Random.Range(-hx, hx),  hy); // top
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix; // visualize the local-area box in world
        Gizmos.DrawCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.01f));
    }
#endif
}
