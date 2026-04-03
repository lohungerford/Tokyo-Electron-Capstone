using UnityEngine;

public class ZombieArmPose : MonoBehaviour
{
    public Transform leftUpperArm;   // drag arm1_L here
    public Transform rightUpperArm;  // drag arm1_R here

    [Range(-180f, 180f)]
    public float armForwardAngle = -80f; // negative = arms forward

    void LateUpdate()
    {
        if (leftUpperArm != null)
        {
            Vector3 rot = leftUpperArm.localEulerAngles;
            rot.x = armForwardAngle;
            leftUpperArm.localEulerAngles = rot;
        }

        if (rightUpperArm != null)
        {
            Vector3 rot = rightUpperArm.localEulerAngles;
            rot.x = armForwardAngle;
            rightUpperArm.localEulerAngles = rot;
        }
    }
}
