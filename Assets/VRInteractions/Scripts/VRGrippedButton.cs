using UnityEngine;
using System.Collections;

/// <summary>
/// VR gripped button. Requires VRButton. This button responds to being "clicked" rather than a physical press.
/// </summary>
[RequireComponent(typeof(VRButton))]
public class VRGrippedButton : MonoBehaviour
{
    /// <summary>
    /// Animation that makes the button press down
    /// </summary>
    public Animation ButtonAnim;

    /// <summary>
    /// Which controller hand should activate this button
    /// </summary>
    public OVRInput.Controller ActivatingController = OVRInput.Controller.RTouch;

    VRButton Button;

    void OnEnable()
    {
        Button = GetComponent<VRButton>();
        if (Button == null)
            Debug.LogError("VRButton is null");

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider _collider)
    {
        if (Button.Interactable == true)
            ActivateButton(_collider.gameObject);
    }

    /// <summary>
    /// Triggers the button if the controller's grip trigger is down
    /// </summary>
    public void ActivateButton(GameObject _controllerObject)
    {
        if (_controllerObject == null) return;

        // Check if it's a known controller by tag
        OVRInput.Controller controller = OVRInput.Controller.None;

        if (_controllerObject.CompareTag("LeftController"))
            controller = OVRInput.Controller.LTouch;
        else if (_controllerObject.CompareTag("RightController"))
            controller = OVRInput.Controller.RTouch;

        if (controller == OVRInput.Controller.None) return;

        // Activate if the trigger is held down
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            if (ButtonAnim != null)
                ButtonAnim.Play();
        }
    }
}