using UnityEngine;

public class ClayShapingController : MonoBehaviour
{
    [Header("References")]
    public Transform rightHand;
    public Transform leftHand;
    public Transform wheelCenter;
    public ClayController clay;

    [Header("Trigger Input (0 to 1)")]
    [Range(0, 1)] public float rightTrigger;
    [Range(0, 1)] public float leftTrigger;

    [Header("Shaping Settings")]
    public float shapingSensitivity = 1.2f;
    public float interactionRadius = 0.6f; // increased for reliability

    private Vector3 rightLastPos;
    private Vector3 leftLastPos;

    void Start()
    {
        if (clay == null)
            clay = FindFirstObjectByType<ClayController>();

        if (!rightHand || !leftHand || !wheelCenter || !clay)
        {
            Debug.LogError("ClayShapingController: Missing references!");
            enabled = false;
            return;
        }

        rightLastPos = rightHand.position;
        leftLastPos = leftHand.position;
    }

    void Update()
    {
        HandleHand(rightHand, ref rightLastPos, rightTrigger);
        HandleHand(leftHand, ref leftLastPos, leftTrigger);
    }

    void HandleHand(Transform hand, ref Vector3 lastPos, float triggerValue)
    {
        if (triggerValue < 0.5f)
        {
            lastPos = hand.position;
            return;
        }

        float distance = GetFlatDistance(hand.position, wheelCenter.position);

        Debug.DrawLine(hand.position, wheelCenter.position, Color.red);

        if (distance > interactionRadius)
        {
            lastPos = hand.position;
            return;
        }

        Vector3 delta = hand.position - lastPos;

        ApplyShaping(delta, hand.position);

        lastPos = hand.position;
    }

    float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return Vector3.Distance(a, b);
    }

    void ApplyShaping(Vector3 delta, Vector3 handPos)
    {
        float vertical = delta.y * shapingSensitivity;

        Vector3 toCenter = (wheelCenter.position - handPos).normalized;
        float horizontal = Vector3.Dot(delta, toCenter) * shapingSensitivity;

        // Vertical shaping
        if (Mathf.Abs(vertical) > 0.0001f)
        {
            if (vertical > 0)
                clay.PullUp(vertical);
            else
                clay.PressDown(-vertical);
        }

        // Horizontal shaping
        if (Mathf.Abs(horizontal) > 0.0001f)
        {
            if (horizontal > 0)
                clay.PressInward(horizontal);
            else
                clay.PressInward(-horizontal * 0.3f);
        }
    }
}