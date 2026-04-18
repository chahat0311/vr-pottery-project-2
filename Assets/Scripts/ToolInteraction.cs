using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class ToolInteraction : MonoBehaviour
{
    [Header("Tool Type")]
    public string toolMode = "PressInward";

    [Header("Strength")]
    public float toolStrength = 0.0015f;

    private ClayController clay;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    void Start()
    {
        clay = FindObjectOfType<ClayController>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Listen for when the tool is picked up or dropped
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnDrop);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // When grabbed, switch to non-kinematic so hand can move it
        GetComponent<Rigidbody>().isKinematic = false;
    }

    void OnDrop(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // When dropped, freeze it in place so it doesn't fall
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void OnTriggerStay(Collider other)
    {
        // Only deform clay when the tool is being held
        if (!isGrabbed) return;
        if (!other.CompareTag("Clay")) return;
        if (clay == null) return;

        switch (toolMode)
        {
            case "PressInward": clay.PressInward(toolStrength); break;
            case "PressDown":   clay.PressDown(toolStrength);   break;
            case "PullUp":      clay.PullUp(toolStrength);      break;
        }
    }
}