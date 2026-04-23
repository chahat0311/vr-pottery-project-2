using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

[RequireComponent(typeof(CharacterController))]
public class HandLocomotion : MonoBehaviour
{
    public Transform xrCamera;
    public float moveSpeed = 1.5f;
    public float gravity = -9.81f;

    private CharacterController cc;
    private XRHandSubsystem handSubsystem;

    private float verticalVelocity;
    private bool leftFist;
    private bool rightFist;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        if (loader != null)
        {
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
            if (handSubsystem != null)
                handSubsystem.updatedHands += OnHandsUpdated;
        }
    }

    void OnDestroy()
    {
        if (handSubsystem != null)
            handSubsystem.updatedHands -= OnHandsUpdated;
    }

    void OnHandsUpdated(XRHandSubsystem sys,
                        XRHandSubsystem.UpdateSuccessFlags flags,
                        XRHandSubsystem.UpdateType type)
    {
        if (type != XRHandSubsystem.UpdateType.Dynamic) return;

        leftFist  = IsFist(sys.leftHand);
        rightFist = IsFist(sys.rightHand);
    }

    void Update()
    {
        // gravity
        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = Vector3.zero;

        if ((leftFist || rightFist) && xrCamera != null)
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

        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose i) ||
            !hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose m) ||
            !hand.GetJoint(XRHandJointID.RingTip).TryGetPose(out Pose r) ||
            !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose p))
            return false;

        float d1 = Vector3.Distance(i.position, p.position);
        float d2 = Vector3.Distance(m.position, p.position);
        float d3 = Vector3.Distance(r.position, p.position);

        return d1 < 0.06f && d2 < 0.06f && d3 < 0.06f;
    }
}