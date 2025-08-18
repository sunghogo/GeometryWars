using UnityEngine;

public class PaddleVFXDriver : MonoBehaviour
{
    [SerializeField] SpriteRenderer sr;
    [SerializeField] float smooth = 12f; // larger = smoother

    MaterialPropertyBlock mpb;
    Vector3 prevPos;
    float velXSmoothed;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        prevPos = transform.position;
    }

    void FixedUpdate()
    {
        // raw velocity from transform motion
        float velX = (transform.position.x - prevPos.x) / Time.fixedDeltaTime;
        prevPos = transform.position;

        // smooth it a bit so glow doesn’t flicker
        float a = 1f - Mathf.Exp(-smooth * Time.fixedDeltaTime);
        velXSmoothed = Mathf.Lerp(velXSmoothed, velX, a);

        // push to shader using a PropertyBlock (no material instancing needed)
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat("_VelX", velXSmoothed);
        sr.SetPropertyBlock(mpb);
    }
}
