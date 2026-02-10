using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TrackingCameraScript : MonoBehaviour
{
    public Transform target; // The target to follow (the ant)
    [SerializeField] Vector3 offset = new Vector3(0, 10, 0); // Vector above the ant

    // Late update in case the ant moves during Update
    void LateUpdate()
    {
        if (target != null)
        {
            // Position the camera above the target and look at it
            transform.position = new Vector3(
                target.position.x,
                target.position.y + offset.y,
                target.position.z
            ) + new Vector3(offset.x, 0, offset.z);
            
            transform.LookAt(target);
        }
    }
}
