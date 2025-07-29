using UnityEngine;

[AddComponentMenu("Camera/TPS Shooter Camera (Smoothed)")]
public class TPShooterCameraSmoothed : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target;               // Player
    public Vector3 targetOffset = new Vector3(0, 1.6f, 0);

    [Header("Distance")]
    public float distance = 4f;           
    public float height   = 1.5f;

    [Header("Speed & Limits")]
    public float yawSpeed   = 120f;        // horizontal turn speed
    public float pitchSpeed = 80f;         // vertical turn speed
    public float pitchMin   = -10f;
    public float pitchMax   =  60f;

    [Header("Smoothing (lower = snappier)")]
    [Range(0f, 0.2f)] public float rotationSmoothTime = 0.05f;
    [Range(0f, 0.2f)] public float positionSmoothTime = 0.05f;

    float rawYaw, rawPitch;                // immediate input angles
    float smoothYaw, smoothPitch;          // smoothed angles
    float yawVelocity, pitchVelocity;      // refs for SmoothDampAngle
    Vector3 positionVelocity;              // ref for SmoothDamp

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // init angles
        var angles       = transform.eulerAngles;
        rawYaw           = smoothYaw = angles.y;
        rawPitch         = smoothPitch = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Mouse Input
        rawYaw   += Input.GetAxis("Mouse X") * yawSpeed   * Time.deltaTime;
        rawPitch -= Input.GetAxis("Mouse Y") * pitchSpeed * Time.deltaTime;
        rawPitch  = Mathf.Clamp(rawPitch, pitchMin, pitchMax);

        smoothYaw   = Mathf.SmoothDampAngle(smoothYaw, rawYaw,   ref yawVelocity,   rotationSmoothTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, rawPitch, ref pitchVelocity, rotationSmoothTime);

        // apply rotation
        Quaternion targetRot = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
        transform.rotation = targetRot;

        Vector3 desiredPos = target.position
                           + targetOffset
                           - targetRot * Vector3.forward * distance
                           + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref positionVelocity, positionSmoothTime);
    }
}
