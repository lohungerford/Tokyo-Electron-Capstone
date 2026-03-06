using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VR Gripper. Goes on the controller. Handles physics joint connections and haptic feedback.
/// </summary>
public class VRGripper : MonoBehaviour
{
    /// <summary>
    /// Which controller this gripper belongs to (set in Inspector)
    /// </summary>
    public OVRInput.Controller CurrentController = OVRInput.Controller.RTouch;

    /// <summary>
    /// Optional audio source for playing surface hits
    /// </summary>
    AudioSource CurrentAudio;

    /// <summary>
    /// Static list of all active controllers managed by grippers
    /// </summary>
    private static List<OVRInput.Controller> ControllerList = new List<OVRInput.Controller>();

    public static List<OVRInput.Controller> GetControllers()
    {
        return new List<OVRInput.Controller>(ControllerList);
    }

    void OnEnable()
    {
        CurrentAudio = GetComponent<AudioSource>();

        if (!ControllerList.Contains(CurrentController))
            ControllerList.Add(CurrentController);
    }

    void OnDisable()
    {
        if (ControllerList.Contains(CurrentController))
            ControllerList.Remove(CurrentController);

        // Make sure vibration stops if disabled
        OVRInput.SetControllerVibration(0, 0, CurrentController);
    }

    private bool isColliding = false;
    private bool isGripping = false;

    public float VibrationLength = 0.1f;
    public float HapticPulseStrength = 0.5f;
    public float HapticFrameTime = 0.001f;

    void OnCollisionEnter(Collision _collision)
    {
        Debug.Log("Colliding: " + _collision.collider.gameObject.name);

        if (!isColliding && !isGripping)
        {
            StartCoroutine(LongVibration(HapticPulseStrength, VibrationLength));

            if (CurrentAudio != null)
                CurrentAudio.Play();

            isColliding = true;
        }
    }

    bool isVibrating = false;

    IEnumerator LongVibration(float _strength, float _duration)
    {
        if (isVibrating) yield break;

        isVibrating = true;
        _strength = Mathf.Clamp(_strength, 0, 1);

        for (float i = 0; i < _duration; i += Time.fixedDeltaTime)
        {
            OVRInput.SetControllerVibration(_strength, _strength, CurrentController);
            yield return new WaitForFixedUpdate();
        }

        // Stop vibration when done
        OVRInput.SetControllerVibration(0, 0, CurrentController);
        isVibrating = false;
    }

    void HapticPulse(float _strength)
    {
        OVRInput.SetControllerVibration(_strength, _strength, CurrentController);
    }

    public void HapticVibration(float _strength, float _duration)
    {
        StartCoroutine(LongVibration(_strength, _duration));
    }

    void OnCollisionExit(Collision _collision)
    {
        isColliding = false;
    }
}