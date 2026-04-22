using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class HandGestureMovement : MonoBehaviour
{
    public CharacterController characterController;
    public Transform head;
    public float speed = 1.5f;

    private InputDevice leftHand;
    private InputDevice rightHand;

    void Start()
    {
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        bool leftPinch = IsPinching(leftHand);
        bool rightPinch = IsPinching(rightHand);

        // Move if either hand is pinching
        if (leftPinch || rightPinch)
        {
            Vector3 forward = head.forward;
            forward.y = 0;
            forward.Normalize();

            characterController.Move(forward * speed * Time.deltaTime);
        }
    }

    bool IsPinching(InputDevice hand)
    {
        if (!hand.isValid) return false;

        // This works for many OpenXR hand setups
        if (hand.TryGetFeatureValue(CommonUsages.trigger, out float value))
        {
            return value > 0.7f; // pinch threshold
        }

        return false;
    }
}