using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

[RequireComponent(typeof(CharacterController))]
public class HandLocomotion : MonoBehaviour
{
    [Header("References")]
    public Transform xrCamera;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float gravity   = -9.81f;

    [Header("Debug")]
    public bool isMoving;

    private XRHandSubsystem     handSubsystem;
    private CharacterController cc;
    private float               verticalVelocity;

    // Cached each time XR hands updates — safe to read in Update
    private bool leftIsFist;
    private bool rightIsFist;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        TrySubscribe();
    }

    void OnEnable()  => TrySubscribe();
    void OnDisable() => Unsubscribe();
    void OnDestroy() => Unsubscribe();

    void TrySubscribe()
    {
        if (handSubsystem != null) return;

        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader == null) return;

        handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
        if (handSubsystem == null) return;

        // This event fires after XR has written fresh hand data —
        // safe to call GetJoint() inside it
        handSubsystem.updatedHands += OnHandsUpdated;
    }

    void Unsubscribe()
    {
        if (handSubsystem != null)
            handSubsystem.updatedHands -= OnHandsUpdated;
    }

    // Called by XR system after hand data is ready
    void OnHandsUpdated(XRHandSubsystem subsystem,
                        XRHandSubsystem.UpdateSuccessFlags flags,
                        XRHandSubsystem.UpdateType updateType)
    {
        leftIsFist  = IsFist(subsystem.leftHand);
        rightIsFist = IsFist(subsystem.rightHand);
    }

    void Update()
    {
        // Try to subscribe if we didn't manage it in Start
        TrySubscribe();

        // Gravity
        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        isMoving = leftIsFist || rightIsFist;

        Vector3 move = Vector3.zero;
        if (isMoving && xrCamera != null)
        {
            Vector3 forward = xrCamera.forward;
            forward.y = 0f;
            move = forward.normalized * moveSpeed;
        }

        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);
    }

    bool IsFist(XRHand hand)
    {
        if (!hand.isTracked) return false;

        if (!hand.GetJoint(XRHandJointID.IndexTip) .TryGetPose(out Pose idx) ||
            !hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose mid) ||
            !hand.GetJoint(XRHandJointID.RingTip)  .TryGetPose(out Pose rng) ||
            !hand.GetJoint(XRHandJointID.Palm)      .TryGetPose(out Pose palm))
            return false;

        float d1 = Vector3.Distance(idx.position,  palm.position);
        float d2 = Vector3.Distance(mid.position,  palm.position);
        float d3 = Vector3.Distance(rng.position,  palm.position);

        Vector3 palmFwd = palm.rotation * Vector3.forward;
        float fwdCheck  =
            Vector3.Dot((idx.position - palm.position).normalized, palmFwd) +
            Vector3.Dot((mid.position - palm.position).normalized, palmFwd);

        return d1 < 0.06f && d2 < 0.06f && d3 < 0.06f && fwdCheck < 0.5f;
    }
}