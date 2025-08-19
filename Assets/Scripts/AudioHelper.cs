using UnityEngine;

public static class AudioHelper
{
    public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        GameObject go = new GameObject("TempAudio");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f; // 2D, set to 1 for 3D
        src.Play();
        Object.Destroy(go, clip.length);
    }
}
