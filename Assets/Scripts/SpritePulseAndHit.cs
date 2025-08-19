using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpritePulseAndHit : MonoBehaviour
{
    [Header("Pulse Runtime")]
    [Range(0f, 2f)] public float pulseAmount = 0.75f;
    [Range(0f,10f)] public float pulseSpeed  = 3f;

    [Header("Hit Flash")]
    public Color hitColor = Color.white;
    [Range(0f, 1f)] public float flashStrength = 1f;
    public float flashDuration = 0.12f;

    SpriteRenderer sr;
    MaterialPropertyBlock mpb;
    float hitT = 0f;      // 0..1 during flash
    float hitVel = 0f;

    static readonly int ID_PulseAmount = Shader.PropertyToID("_PulseAmount");
    static readonly int ID_PulseSpeed  = Shader.PropertyToID("_PulseSpeed");
    static readonly int ID_HitAmount   = Shader.PropertyToID("_HitAmount");
    static readonly int ID_HitColor    = Shader.PropertyToID("_HitColor");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_PulseAmount, pulseAmount);
        mpb.SetFloat(ID_PulseSpeed,  pulseSpeed);
        mpb.SetColor(ID_HitColor,    hitColor);
        mpb.SetFloat(ID_HitAmount,   0f);
        sr.SetPropertyBlock(mpb);
    }

    void Update()
    {
        // Decay the hit flash smoothly
        if (hitT > 0f)
        {
            hitT = Mathf.SmoothDamp(hitT, 0f, ref hitVel, flashDuration * 0.6f);
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(ID_HitAmount, hitT * flashStrength);
            sr.SetPropertyBlock(mpb);
        }

        // Keep pulse synced with inspector sliders (optional)
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_PulseAmount, pulseAmount);
        mpb.SetFloat(ID_PulseSpeed,  pulseSpeed);
        sr.SetPropertyBlock(mpb);
    }

    /// <summary>Call this when the entity takes damage.</summary>
    public void TriggerHitFlash()
    {
        hitT = 1f;
        hitVel = 0f;
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_HitAmount, hitT * flashStrength);
        sr.SetPropertyBlock(mpb);
    }
}
