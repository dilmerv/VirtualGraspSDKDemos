// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using UnityEngine.XR;
using VirtualGrasp;

/** 
 * VG_PostAnimator exemplifies how you could overwrite (post-animate) grasp animations that are handled by VirtualGrasp.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vgpostanimator." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_PostAnimator : MonoBehaviour
{
    private Quaternion m_leftHandTargetRotation = Quaternion.Euler(14.47f, -274.42f, -348.29f);
    private Quaternion m_rightHandTargetRotation = Quaternion.Euler(14.47f, 274.42f, 348.29f);

    void Start()
    {
        VG_Controller.OnPostUpdate.AddListener(Animate);
    }

    private bool GetOtherButtonTrigger(VG_HandSide handSide, out float trigger)
    {
        // Receive the device used for this hand side, and receive the trigger value of ...
        InputDevice device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(handSide == VG_HandSide.LEFT ? XRNode.LeftHand : XRNode.RightHand);
        // ... the grip button if VG is using the trigger for grasping (or the other way around)
        return device.TryGetFeatureValue(VG_Controller.GetTriggerButton() == VG_VrButton.TRIGGER ? CommonUsages.grip : CommonUsages.trigger, out trigger);
    }

    public void Animate()
    {
        foreach (VG_HandStatus hand in VG_Controller.GetHands())
        {
            // Check if it is this object that is held in the hand.
            if (hand.m_selectedObject != transform || !hand.IsHolding())
                continue;

            // Receive the trigger signal of the controller and the transform of the first (0) bone of the index finger (1).
            if (GetOtherButtonTrigger(hand.m_side, out float trigger) &&
                VG_Controller.GetFingerBone(hand.m_avatarID, hand.m_side, 1, 0, out Transform currentTransform) == VG_ReturnCode.SUCCESS)
            {
                // Modify the local transform by interpolating it between the current and the target rotation.
                currentTransform.localRotation = Quaternion.Slerp(hand.m_side == VG_HandSide.LEFT ?
                    m_leftHandTargetRotation : m_rightHandTargetRotation,
                    currentTransform.localRotation,
                    trigger);
            }
        }
    }
}