using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    public Transform target;   
    public float height = 10f;
    public float orthoSize = 6f;

    void LateUpdate()
    {
        if (!target) return;

        transform.position = new Vector3(target.position.x, height, target.position.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var cam = GetComponent<Camera>();
        if (cam) { cam.orthographic = true; cam.orthographicSize = orthoSize; }
    }
}
