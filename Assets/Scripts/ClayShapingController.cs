using UnityEngine;

public class ClayShapingController : MonoBehaviour
{
    [Header("Drag these in from Hierarchy")]
    public Transform      rightHand;
    public Transform      leftHand;
    public Transform      wheelCenter;
    public ClayController clay;

    [Header("Settings")]
    public float interactionRadius   = 0.15f;
    public float heightSensitivity   = 8.0f;  // how much up/down affects height
    public float radialSensitivity   = 6.0f;  // how much in/out affects width

    [Header("Debug")]
    public bool  rightHandShaping;
    public bool  leftHandShaping;
    public float rightHandDistance;
    public float leftHandDistance;

    private Vector3 rightLastPos;
    private Vector3 leftLastPos;

    void Start()
    {
        if (clay == null)
            clay = FindFirstObjectByType<ClayController>();

        rightLastPos = rightHand.position;
        leftLastPos  = leftHand.position;
    }

    void Update()
    {
        Vector3 rPos = rightHand.position;
        Vector3 lPos = leftHand.position;
        Vector3 axis = new Vector3(
            wheelCenter.position.x, 0f, wheelCenter.position.z);

        rightHandDistance = FlatDist(rPos, wheelCenter.position);
        leftHandDistance  = FlatDist(lPos, wheelCenter.position);

        rightHandShaping = rightHandDistance <= interactionRadius;
        leftHandShaping  = leftHandDistance  <= interactionRadius;

        if (rightHandShaping) Shape(rPos, ref rightLastPos, axis);
        if (leftHandShaping)  Shape(lPos, ref leftLastPos,  axis);

        rightLastPos = rPos;
        leftLastPos  = lPos;
    }

    void Shape(Vector3 pos, ref Vector3 lastPos, Vector3 axis)
    {
        Vector3 delta = pos - lastPos;
        if (delta.magnitude < 0.0001f) return;

        // ── Vertical: purely Y movement ──────────────────────────
        float vertical = Mathf.Clamp(
            delta.y * heightSensitivity, -0.03f, 0.03f);

        // ── Radial: only XZ movement toward/away from center ─────
        Vector3 radialDir = new Vector3(
            pos.x - axis.x, 0f, pos.z - axis.z).normalized;
        float radial = Mathf.Clamp(
            Vector3.Dot(new Vector3(delta.x, 0f, delta.z), radialDir)
            * radialSensitivity, -0.03f, 0.03f);

        // Vertical and radial are completely independent
        if (vertical >  0.0001f) clay.PullUp(vertical);
        if (vertical < -0.0001f) clay.PressDown(-vertical);
        if (radial   >  0.0001f) clay.PressInward(radial);
    }

    float FlatDist(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return Vector3.Distance(a, b);
    }
}