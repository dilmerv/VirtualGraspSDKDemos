// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using VirtualGrasp;

/** 
 * ChangeSelectionWeight shows as a tutorial on how to runtime change object
 * selection weight to affect how easy an object can be selected for interaction with VG.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_vgonboarding_task3." + VG_Version.__VG_VERSION__ + ".html")]
public class ChangeSelectionWeight : MonoBehaviour
{
    public Transform m_dependent_object;
    public float m_releasedWeight = 1.0f;
    public float m_graspedWeight = 2.0f;

    void Start()
    {
        VG_Controller.OnObjectFullyReleased.AddListener(ObjectReleased);
        VG_Controller.OnObjectGrasped.AddListener(ObjectGrasped);

        if (m_dependent_object == null)
            m_dependent_object = transform.parent;
    }

    void ObjectReleased(VG_HandStatus hand)
    {
        if (hand.m_selectedObject == m_dependent_object)
            VG_Controller.SetObjectSelectionWeight(transform, m_releasedWeight);
    }

    void ObjectGrasped(VG_HandStatus handStatus)
    {
        if (handStatus.m_selectedObject == m_dependent_object)
            VG_Controller.SetObjectSelectionWeight(transform, m_graspedWeight);        
    }
}
