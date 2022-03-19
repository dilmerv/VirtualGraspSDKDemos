// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

//#define USE_OCULUS_CONTROLLER // Please read below instructions and requirements before activating.

using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualGrasp;

/**
 * This is an external controller class that supports the Oculus Finger Tracking controller as an external controller.
 * Please refer to https://docs.virtualgrasp.com/controllers.html for the definition of an external controller for VG.
 * 
 * The following requirements have to be met to be able to enable the #define USE_OCULUS_CONTROLLER above and use the controller:
 * - You have the Oculus SDK (https://www.oculus.com/setup/) installed on your computer.
 * - You have the Oculus Integration plugin from https://developer.oculus.com/downloads/package/unity-integration/ imported into your Unity project.
 * - You have the same Oculus Integration plugin version as the one on your headset AND Oculus App.
 * - You have setup the AndroidManifest.xml properly, i.e. they need to include
 *		<uses-permission android:name="com.oculus.permission.HAND_TRACKING" />
 *		<uses-feature android:name="oculus.software.handtracking" android:required="false" />
 */

[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vg_ec_oculushand." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_EC_OculusHand : VG_ExternalController
{
#if USE_OCULUS_CONTROLLER
	private OVRPlugin.Skeleton m_skeleton = new OVRPlugin.Skeleton();
	private OVRPlugin.HandState m_currentState = new OVRPlugin.HandState();
#endif

	[Serializable]
	public class HandMapping : VG_ExternalControllerMapping
	{
		public override void Initialize(int avatarID, VG_HandSide side)
		{
			base.Initialize(avatarID, side);
			m_BoneToTransform = new Dictionary<int, Transform>()
			{
#if USE_OCULUS_CONTROLLER
			{ (int)OVRPlugin.BoneId.Hand_WristRoot, Hand_WristRoot },
			{ (int)OVRPlugin.BoneId.Hand_ForearmStub, null }, // this is a child of wrist, but towards the arm
			{ (int)OVRPlugin.BoneId.Hand_Thumb0, null },
			{ (int)OVRPlugin.BoneId.Hand_Thumb1, Hand_Thumb1 },
			{ (int)OVRPlugin.BoneId.Hand_Thumb2, Hand_Thumb2 },
			{ (int)OVRPlugin.BoneId.Hand_Thumb3, Hand_Thumb3 },
			{ (int)OVRPlugin.BoneId.Hand_ThumbTip, null },
			{ (int)OVRPlugin.BoneId.Hand_Index1, Hand_Index1 },
			{ (int)OVRPlugin.BoneId.Hand_Index2, Hand_Index2 },
			{ (int)OVRPlugin.BoneId.Hand_Index3, Hand_Index3 },
			{ (int)OVRPlugin.BoneId.Hand_IndexTip, null },
			{ (int)OVRPlugin.BoneId.Hand_Middle1, Hand_Middle1 },
			{ (int)OVRPlugin.BoneId.Hand_Middle2, Hand_Middle2 },
			{ (int)OVRPlugin.BoneId.Hand_Middle3, Hand_Middle3 },
			{ (int)OVRPlugin.BoneId.Hand_MiddleTip, null },
			{ (int)OVRPlugin.BoneId.Hand_Ring1, Hand_Ring1 },
			{ (int)OVRPlugin.BoneId.Hand_Ring2, Hand_Ring2 },
			{ (int)OVRPlugin.BoneId.Hand_Ring3, Hand_Ring3 },
			{ (int)OVRPlugin.BoneId.Hand_RingTip, null },
			{ (int)OVRPlugin.BoneId.Hand_Pinky0, null },
			{ (int)OVRPlugin.BoneId.Hand_Pinky1, Hand_Pinky1 },
			{ (int)OVRPlugin.BoneId.Hand_Pinky2, Hand_Pinky2 },
			{ (int)OVRPlugin.BoneId.Hand_Pinky3, Hand_Pinky3 },
			{ (int)OVRPlugin.BoneId.Hand_PinkyTip, null }
#endif
			};

			m_BoneToParent = new Dictionary<int, int>()
			{
			};
#if USE_OCULUS_CONTROLLER
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_ForearmStub] = (int)OVRPlugin.BoneId.Hand_WristRoot;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Thumb0] = (int)OVRPlugin.BoneId.Hand_WristRoot;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Thumb1] = (int)OVRPlugin.BoneId.Hand_Thumb0;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Thumb2] = (int)OVRPlugin.BoneId.Hand_Thumb1;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Thumb3] = (int)OVRPlugin.BoneId.Hand_Thumb2;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_ThumbTip] = (int)OVRPlugin.BoneId.Hand_Thumb3;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Index1] = (int)OVRPlugin.BoneId.Hand_WristRoot;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Index2] = (int)OVRPlugin.BoneId.Hand_Index1;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Index3] = (int)OVRPlugin.BoneId.Hand_Index2;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_IndexTip] = (int)OVRPlugin.BoneId.Hand_Index3;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Middle1] = (int)OVRPlugin.BoneId.Hand_WristRoot;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Middle2] = (int)OVRPlugin.BoneId.Hand_Middle1;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Middle3] = (int)OVRPlugin.BoneId.Hand_Middle2;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_MiddleTip] = (int)OVRPlugin.BoneId.Hand_Middle3;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Ring1] = (int)OVRPlugin.BoneId.Hand_WristRoot;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Ring2] = (int)OVRPlugin.BoneId.Hand_Ring1;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Ring3] = (int)OVRPlugin.BoneId.Hand_Ring2;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_RingTip] = (int)OVRPlugin.BoneId.Hand_Ring3;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Pinky0] = (int)OVRPlugin.BoneId.Hand_WristRoot;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Pinky1] = (int)OVRPlugin.BoneId.Hand_Pinky0;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Pinky2] = (int)OVRPlugin.BoneId.Hand_Pinky1;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_Pinky3] = (int)OVRPlugin.BoneId.Hand_Pinky2;
			m_BoneToParent[(int)OVRPlugin.BoneId.Hand_PinkyTip] = (int)OVRPlugin.BoneId.Hand_Pinky3;
#endif
		}
	}

	public VG_EC_OculusHand(int avatarID, VG_HandSide side)
	{
		m_avatarID = avatarID;
		m_handType = side;
	}

	public new void Initialize()
	{
#if USE_OCULUS_CONTROLLER
		m_mapping = new HandMapping();

		if (OVRPlugin.GetSkeleton(
			(m_handType == VG_HandSide.LEFT) ? OVRPlugin.SkeletonType.HandLeft :
			(m_handType == VG_HandSide.RIGHT) ? OVRPlugin.SkeletonType.HandRight : OVRPlugin.SkeletonType.None,
			out m_skeleton))
		{
			base.Initialize();
			m_initialized = true;
		}	
		else m_initialized = false;
#endif
	}

	private Matrix4x4 modifyPose(Matrix4x4 rawPose, bool isWrist)
	{
		if (isWrist) return Matrix4x4.TRS(rawPose.GetColumn(3),
				Quaternion.LookRotation(
				m_handType == VG_HandSide.LEFT ? rawPose.GetColumn(1) : -rawPose.GetColumn(1),
				m_handType == VG_HandSide.LEFT ? rawPose.GetColumn(0) : -rawPose.GetColumn(0)), Vector3.one);
		else return Matrix4x4.TRS(rawPose.GetColumn(3), Quaternion.LookRotation( // Z, Y
				m_handType == VG_HandSide.LEFT ? rawPose.GetColumn(2) : -rawPose.GetColumn(2),
				m_handType == VG_HandSide.LEFT ? rawPose.GetColumn(0) : -rawPose.GetColumn(0)),
				Vector3.one);
	}

	public override float GetGrabStrength()
	{
#if USE_OCULUS_CONTROLLER
		if (m_initialized && m_currentState.Status.HasFlag(OVRPlugin.HandStatus.HandTracked))
			//return -1.0f; // let VG decide from full DOF
			return (m_currentState.PinchStrength[0] + m_currentState.PinchStrength[1] + m_currentState.PinchStrength[2]) / 3.0f;
#endif
		return 0.0f;
	}

	private bool IsTracking()
	{
#if USE_OCULUS_CONTROLLER
		return OVRPlugin.GetHandState(OVRPlugin.Step.Render, m_handType == VG_HandSide.LEFT ? OVRPlugin.Hand.HandLeft : OVRPlugin.Hand.HandRight, ref m_currentState)
			&& m_currentState.Status.HasFlag(OVRPlugin.HandStatus.HandTracked);
#else
		return false;
#endif
	}

	public override bool Compute()
	{
		if (!m_initialized)
		{ 
			Initialize();
			return false;
		}

#if USE_OCULUS_CONTROLLER
		if (!IsTracking()) return false;

		for (int boneId = 0; boneId < GetNumBones(); ++boneId)
		{
			if (boneId == 0)
			{
				SetPose(boneId, Matrix4x4.TRS(
					m_currentState.RootPose.Position.FromFlippedZVector3f(),
					m_currentState.RootPose.Orientation.FromFlippedZQuatf(),
					Vector3.one));
			}
			else SetPose(boneId, m_poses[m_mapping.GetParent(boneId)] *
				Matrix4x4.TRS(
				m_skeleton.Bones[boneId].Pose.Position.FromFlippedZVector3f(),
				m_currentState.BoneRotations[boneId].FromFlippedZQuatf(),
				Vector3.one));
		}

		for (int boneId = 0; boneId < GetNumBones(); ++boneId)
			m_poses[boneId] = modifyPose(m_poses[boneId], boneId == 0);

		return true;
#else
		return false;
#endif
	}

	public override Color GetConfidence()
	{
#if USE_OCULUS_CONTROLLER
		if (!m_initialized) return Color.black;

		switch (m_currentState.HandConfidence)
		{
			case OVRPlugin.TrackingConfidence.High:
				return Color.green;
			case OVRPlugin.TrackingConfidence.Low:
				return Color.red;
		}
#endif
		return Color.black;
	}

	public override void HapticPulse(VG_HandStatus hand, float amplitude = 0.5F, float duration = 0.01F, int finger = 5)
	{
	}
}
