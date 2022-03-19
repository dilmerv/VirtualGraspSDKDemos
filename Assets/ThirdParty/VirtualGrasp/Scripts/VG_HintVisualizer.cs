// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using VirtualGrasp;

/** 
 * VG_HintVisualizer provides a tool to visualize some hints such as a selection sphere to debug object selection or a push sphere to guide pushing interactions.
 * The MonoBehavior provides a tutorial on the VG API functions for accessing the push state (GetPushCircle).
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vghintvisualizer." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_HintVisualizer : MonoBehaviour
{
    [Tooltip("Enable push hint visualization. For each hand, a circular hint will appear when the hand is in PUSHING mode.")]
    public bool m_enablePushHints = true;
    [Tooltip("Enable grasp hint visualization. For each hand, a sphere will appear as a hint of the current selection state.")]
    public bool m_enableGraspHints = true;
    
    [Tooltip("A push hint visualization (e.g. put a sphere here) when push mode is PUSHING.")]
    public List<Transform> pushHints = new List<Transform> { };
    [Tooltip("A selection hint visualization (e.g. put a sphere here) where the grasp selector is placed.")]
    public List<Transform> graspHints = new List<Transform> { };

    /// Add a collider-less sphere as a marker
    private void AddHintObject(List<Transform> hintList, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.localScale = Vector3.zero;
        go.transform.SetParent(transform);
        hintList.Add(go.transform);
    }

    public void Start()
    {
        VG_Controller.OnPostUpdate.AddListener(HintUpdate);
        if (pushHints.Count == 0) AddHintObject(pushHints, "PushHint");
        if (graspHints.Count == 0) AddHintObject(graspHints, "SelectionHint");        
    }

    void HintUpdate()
    {
        if (m_enablePushHints)
        {
            // Fill up the push hints assuming each avatar has 2 hands
            while (pushHints.Count < VG_Controller.GetHands().Count())
            {
                pushHints.Add(GameObject.Instantiate(pushHints.Last()));
                pushHints.Last().SetParent(transform);
            }

            int num = 0;
            foreach (VG_HandStatus hand in VG_Controller.GetHands())
            {
                Transform t = VG_Controller.GetPushCircle(hand.m_avatarID, hand.m_side, out Vector3 p, out Quaternion q, out float radius, out bool inContact);
                Transform hint = pushHints[num];
                hint.gameObject.SetActive(t != null);
                if (t != null)
                {
                    hint.SetPositionAndRotation(p + q * new Vector3(0, 0, -0.001f), q);
                    hint.localScale = new Vector3(2 * radius, 2 * radius, 0.001f);
                    if (inContact) VG_Controller.OnObjectCollided.Invoke(hand);
                }
                num++;
            }
        }

        if (m_enableGraspHints)
        {
            // Fill up the push hints assuming each avatar has 2 hands
            while (graspHints.Count < VG_Controller.GetHands().Count())
            {
                graspHints.Add(GameObject.Instantiate(graspHints.Last()));
                graspHints.Last().SetParent(transform);
            }

            int num = 0;
            foreach (VG_HandStatus hand in VG_Controller.GetHands())
            {
                Transform t = VG_Controller.GetBone(hand.m_avatarID, hand.m_side, VG_BoneType.APPROACH, out _, out Matrix4x4 m);
                Transform hint = graspHints[num];
                hint.gameObject.SetActive(t != null);
                if (t != null)
                {
                    hint.SetPositionAndRotation(m.GetColumn(3), Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1)));
                    if (m.GetRow(3) != new Vector4(0, 0, 0, 1)) hint.localScale = m.GetRow(3);
                    hint.GetComponent<Renderer>().material.SetColor("_Color",
                        VG_Controller.GetHand(hand.m_avatarID, hand.m_side).m_selectedObject == null ? Color.red : Color.green);
                }
                num++;
            }
        }
    }
}
