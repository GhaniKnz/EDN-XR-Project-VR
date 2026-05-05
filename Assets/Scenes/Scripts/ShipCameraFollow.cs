using UnityEngine;

/// <summary>
/// Smoothly follows the ship from behind. Attach to the XR Origin (or camera rig root).
/// </summary>
public class ShipCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, -5.5f);
    [SerializeField] private bool lockToTarget = true;
    [SerializeField] private float positionSmoothing = 60f;
    [SerializeField] private float rotationSmoothing = 45f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.TransformPoint(offset);
        Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);

        if (lockToTarget)
        {
            transform.position = desiredPos;
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            return;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, Mathf.Clamp01(positionSmoothing * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Mathf.Clamp01(rotationSmoothing * Time.deltaTime));
    }

    public void SetTarget(Transform ship)
    {
        target = ship;
        offset = new Vector3(0f, 2.2f, -5.5f);
        lockToTarget = true;
        positionSmoothing = 60f;
        rotationSmoothing = 45f;
    }
}
