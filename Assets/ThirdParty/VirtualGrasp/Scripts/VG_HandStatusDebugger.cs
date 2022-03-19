// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using VirtualGrasp;
using System.Collections.Generic;

/** 
 * VG_HandStatus_Debugger provides a tool to show the VG_HandStatus members during runtime in editor mode.
 * The MonoBehavior provides a tutorial on the VG API functions for using the VG_HandStatus.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vghandstatusdebugger." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_HandStatusDebugger : MonoBehaviour
{
    [Tooltip("This list will be updated during runtime with the VG_HandStatus of all hands.")]
    public List<VG_HandStatus> m_hands = new List<VG_HandStatus>();

#if UNITY_EDITOR
    public void Start()
    {
        this.hideFlags = HideFlags.NotEditable;
    }

    public void Update()
    {
        m_hands.Clear();
        foreach (VG_HandStatus hand in VG_Controller.GetHands())
            m_hands.Add(hand);
    }
#endif
}