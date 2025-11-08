using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public Transform target;
    public float minDistance = 0.5f;
    public float maxDistance = 3.0f;
    public float smooth = 10.0f;
    Vector3 dollyDir;
    private float distance;

    void Awake()
    {
        dollyDir = transform.localPosition.normalized;
        distance = transform.localPosition.magnitude;
    }

    void LateUpdate()
    {
        Vector3 desiredCameraPos = target.TransformPoint(dollyDir * maxDistance);
        RaycastHit hit;
        if (Physics.Linecast(target.position, desiredCameraPos, out hit))
        {
            distance = Mathf.Clamp(hit.distance * 0.9f, minDistance, maxDistance);
        }
        else
        {
            distance = maxDistance;
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, dollyDir * distance, Time.deltaTime * smooth);
    }
}
