using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        if (target == null || GameManager.Instance.GameOver) return;
        transform.position = target.position + offset;

        // keep camera rotation fixed (identity or whatever angle you want)
        transform.rotation = Quaternion.identity;
    }
}
