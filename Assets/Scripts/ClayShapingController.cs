using UnityEngine;
using UnityEngine.InputSystem;

public class ClayShapingController : MonoBehaviour
{
    [Header("References - drag in from Hierarchy")]
    public Transform rightHand;
    public Transform leftHand;
    public Transform wheelCenter;
    public ClayController clay;

    [Header("Shaping Settings")]
    public float shapingSensitivity = 1.5f;
    public float interactionRadius = 0.35f;

    [Header("Debug - watch these in Inspector while playing")]
    public bool rightHandShaping = false;
    public bool leftHandShaping = false;
    public float rightHandDistance = 0f;
    public float leftHandDistance = 0f;

    private Vector3 rightLastPos;
    private Vector3 leftLastPos;

    private InputAction rightTrigger;
    private InputAction leftTrigger;

    void Start()
    {
        if (clay == null)
            clay = Object.FindFirstObjectByType<ClayController>();

        if (!rightHand || !leftHand || !wheelCenter || !clay)
        {
            Debug.LogError("ClayShapingController: Missing references! Check Inspector.");
            enabled = false;
            return;
        }

        rightLastPos = rightHand.position;
        leftLastPos  = leftHand.position;

        // Bind triggers
        rightTrigger = new InputAction(
            "RightTrigger",
            binding: "<XRController>{RightHand}/trigger"
        );
        leftTrigger = new InputAction(
            "LeftTrigger",
            binding: "<XRController>{LeftHand}/trigger"
        );

        rightTrigger.Enable();
        leftTrigger.Enable();
    }

    // void Update()
    // {
    //     bool rightPressed = rightTrigger.ReadValue<float>() > 0.5f;
    //     bool leftPressed  = leftTrigger.ReadValue<float>()  > 0.5f;

    //     // Update debug values so you can watch in Inspector
    //     rightHandDistance = GetFlatDistance(rightHand.position, wheelCenter.position);
    //     leftHandDistance  = GetFlatDistance(leftHand.position,  wheelCenter.position);

    //     rightHandShaping = rightPressed && rightHandDistance <= interactionRadius;
    //     leftHandShaping  = leftPressed  && leftHandDistance  <= interactionRadius;

    //     // Only shape when trigger held AND hand near wheel
    //     if (rightHandShaping)
    //         HandleHand(rightHand, ref rightLastPos);

    //     if (leftHandShaping)
    //         HandleHand(leftHand, ref leftLastPos);

    //     // Always update last positions
    //     rightLastPos = rightHand.position;
    //     leftLastPos  = leftHand.position;
    // }

void Update()
{
    // Temporarily always active - no trigger needed
    // We'll add trigger back once shaping is confirmed working
    rightHandDistance = GetFlatDistance(rightHand.position, wheelCenter.position);
    leftHandDistance  = GetFlatDistance(leftHand.position,  wheelCenter.position);

    rightHandShaping = rightHandDistance <= interactionRadius;
    leftHandShaping  = leftHandDistance  <= interactionRadius;

    if (rightHandShaping)
        HandleHand(rightHand, ref rightLastPos);

    if (leftHandShaping)
        HandleHand(leftHand, ref leftLastPos);

    rightLastPos = rightHand.position;
    leftLastPos  = leftHand.position;
}

    void HandleHand(Transform hand, ref Vector3 lastPos)
    {
        Vector3 delta = hand.position - lastPos;

        // Ignore tiny jitter
        if (delta.magnitude < 0.0001f) return;

        float vertical   = delta.y * shapingSensitivity;
        Vector3 toCenter = (wheelCenter.position - hand.position).normalized;
        float horizontal = Vector3.Dot(delta, toCenter) * shapingSensitivity;

        // Vertical movement
        if (Mathf.Abs(vertical) > 0.0001f)
        {
            if (vertical > 0)
                clay.PullUp(vertical);
            else
                clay.PressDown(-vertical);
        }

        // Horizontal movement
        if (Mathf.Abs(horizontal) > 0.0001f)
        {
            if (horizontal > 0)
                clay.PressInward(horizontal);
            else
                clay.PressInward(-horizontal * 0.3f);
        }
    }

    float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return Vector3.Distance(a, b);
    }

    void OnDestroy()
    {
        rightTrigger?.Disable();
        leftTrigger?.Disable();
    }
}