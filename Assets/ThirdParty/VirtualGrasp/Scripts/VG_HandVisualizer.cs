// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VirtualGrasp;

/** 
 * VG_HandVisualizer provides a tool to visualize the hand bones in Unity.
 * The MonoBehavior provides a tutorial on the VG API functions for accessing specific bones / elements of the hands.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vghandvisualizer." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_HandVisualizer : MonoBehaviour
{
    private Dictionary<int, GameObject> m_limbs = new Dictionary<int, GameObject>();
    private Dictionary<int, LineRenderer> m_lines = new Dictionary<int, LineRenderer>();
    private Transform m_root = null;

    void Start()
    {
        m_root = new GameObject("DebugVis").transform;
        VG_Controller.OnPostUpdate.AddListener(Visualize);
    }

    void Visualize()
    {
        foreach (VG_HandStatus hand in VG_Controller.GetHands())
        {
            if (VG_Controller.GetBone(hand.m_avatarID, hand.m_side, VG_BoneType.WRIST, out int wrist_iid, out Vector3 pw, out Quaternion qw) == VG_ReturnCode.SUCCESS)
            {
                if (!m_limbs.ContainsKey(wrist_iid))
                {
                    m_limbs[wrist_iid] = new GameObject("Avatar" + hand.m_avatarID + "_" + hand.m_side + "_wrist");
                    m_limbs[wrist_iid].transform.SetParent(m_root, true);
                    Destroy(m_limbs[wrist_iid].GetComponent<Collider>());
                }
                m_limbs[wrist_iid].transform.SetPositionAndRotation(pw, qw);
            }
            else continue;

            foreach (int fingerId in new List<int>() { 0, 1, 2, 3, 4 })
            {
                foreach (int boneId in new List<int>() { 0, 1, 2, -1 })
                {
                    if (VG_Controller.GetFingerBone(hand.m_avatarID, hand.m_side, fingerId, boneId, out int iid, out Vector3 pf, out Quaternion qf) == VG_ReturnCode.SUCCESS)
                    {
                        if (!m_limbs.ContainsKey(iid))
                        {
                            Transform last_bone = m_limbs.Last().Value.transform;
                            m_limbs[iid] = new GameObject(hand.m_side + "_" + VG_Controller.GetBone(iid).name + "_" + fingerId.ToString() + boneId.ToString());
                            m_limbs[iid].transform.SetParent(boneId == 0 ? m_limbs[wrist_iid].transform : last_bone, true); 
                            Destroy(m_limbs[iid].GetComponent<Collider>()); 
                            m_lines[iid] = m_limbs[iid].AddComponent<LineRenderer>();
                            m_lines[iid].widthMultiplier = 0.002f;
                            m_lines[iid].positionCount = 2;
                            m_lines[iid].useWorldSpace = true;                                                      
                        }

                        m_limbs[iid].transform.SetPositionAndRotation(pf, qf);
                        m_lines[iid].SetPosition(0, m_limbs[iid].transform.parent.position);
                        m_lines[iid].SetPosition(1, pf);
                    }
                }
            }
        }
    }
}
