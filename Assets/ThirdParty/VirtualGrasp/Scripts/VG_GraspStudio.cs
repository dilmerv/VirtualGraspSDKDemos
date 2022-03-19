// Copyright (C) 2014-2022 Gleechi AB. All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // for Text UI
using UnityEngine.XR;
using VirtualGrasp;
using System.Linq;

/** 
 * VG_GraspStudio provides a tool to visualize, label, and edit grasps.
 * The MonoBehavior provides a tutorial on the VG API functions for accessing static grasps as well as using the labeling interface.
 */
[LIBVIRTUALGRASP_UNITY_SCRIPT]
[HelpURL("https://docs.virtualgrasp.com/unity_component_vggraspstudio." + VG_Version.__VG_VERSION__ + ".html")]
public class VG_GraspStudio : MonoBehaviour
{
    public enum SelectionMode
    {
        NONE,
        HANDS,
        HEAD,
        FINGER
    };

    [Tooltip("Enable for VR mode. 2D mode otherwise.")]
    public bool m_vrMode = true;
    [Tooltip("Allow or disallow non-contact grasps to be annotated.")]
    public bool m_allowAirGrasps = false;
    [Tooltip("Enable for VR mode. 2D mode otherwise.")]
    public SelectionMode m_selectionMode = SelectionMode.HANDS;
    [Tooltip("Enable if you want all interactable objects to be hidden in the scene (and only focus on grasp editing).")]
    private bool m_hideAllObjects = true;
    [Tooltip("Enable to allow adding of dynamic grasps with another hand.")]
    private bool m_useDynamicGraspAdding = false;
    [RangeAttribute(1.0f, 5.0f)]
    [Tooltip("Range of scaling the GUI during VR mode.")]
    public float m_guiScale = 3.0f;
    [Tooltip("The GAME avatar ID (not for the main editor, but for dynamic grasping).")]
    public int m_gameAvatarID = 2;
    [Tooltip("A list of label buttons. Note that they build a circle of segments for thumbstick selection.")]
    public List<Button> m_labelButtons = new List<Button>();

    [Tooltip("The main view.")]
    [SerializeField] private RectTransform m_mainView = null;
    [Tooltip("The thumbnail / catalogue view.")]
    [SerializeField] private RectTransform m_catalogueView = null;
    [Tooltip("The thumbnail / catalogue grid size.")]
    [SerializeField] private Vector2 m_catalogueTileSize = new Vector2(3, 3);
    private UnityEngine.UI.Button _button_ToggleEnabledGrasps = null;
    private UnityEngine.UI.Button _button_ToggleDisabledGrasps = null;
    [Tooltip("The label to show the grasp selection information.")]
    [SerializeField] private Text _currentSelectionIndicesLabel = null;
    [Tooltip("The label to show the object selection information.")]
    [SerializeField] private Text _currentSelectionObjectLabel = null;

    private int m_editorAvatarID = 1;
    private List<Transform> m_objects = new List<Transform>();
    private int m_currentPageIndex = 0;
    private VG_HandSide m_currentSide = VG_HandSide.LEFT;
    private int m_currentGraspIndex = 0;
    private int m_formerGraspIndex = -1;
    private int m_objectIndex = 0;
    private int m_formerObjectIndex = -1;
    private Transform m_selectedObject = null;
    private Transform m_dynamicObject = null;
    private Transform[] m_hands = { null, null };
    private Quaternion m_targetRotation;
    private bool m_showEnabled = true;
    private bool m_showDisabled = true;
    private List<int> m_currentGraspIds = new List<int>();
    private Dictionary<VG_GraspLabel, HashSet<int>> m_currentLabels = new Dictionary<VG_GraspLabel, HashSet<int>>();
    private List<Button> m_allButtons = new List<Button>();

    private RectTransform m_catalogueTilePrefab = null;
    private RectTransform[] m_catalogueTiles;
    private int m_numCatalogueTiles = 9;

    private Transform m_rootHand;
    private List<Transform> m_displayGrasps = new List<Transform>();
    private float m_displayGraspScale = 0.35f;

    private Sprite _handImage_LeftEnabled = null;
    private Sprite _handImage_RightEnabled = null;
    private Sprite _statusIcon_Primary = null;
    private Sprite _statusIcon_Disabled = null;

    private Vector3 m_objectCenterOffset;
    private Dictionary<XRNode, Transform> m_markers = new Dictionary<XRNode, Transform>()
        { {XRNode.LeftHand, null }, {XRNode.RightHand, null } };
    private Dictionary<XRNode, int> m_controllerJoystickSection = new Dictionary<XRNode, int>()
        { {XRNode.LeftHand, -1 }, {XRNode.RightHand, -1 } };
    private Dictionary<XRNode, bool> m_controllerPrimaryPushed = new Dictionary<XRNode, bool>()
        { {XRNode.LeftHand, false }, {XRNode.RightHand, false } };
    private Dictionary<XRNode, bool> m_controllerPrimaryGripped = new Dictionary<XRNode, bool>()
        { {XRNode.LeftHand, false }, {XRNode.RightHand, false } };
    private Dictionary<XRNode, bool> m_controllerPrimaryTriggered = new Dictionary<XRNode, bool>()
        { {XRNode.LeftHand, false}, {XRNode.RightHand, false } };

    private bool m_initialized = false;

    void Start()
    {
        VG_Controller.OnPostUpdate.AddListener(Check);
    }

    public bool Initialize()
    {
        if (!VG_Controller.IsEnabled())
        {
            Debug.LogError("VirtualGrasp was not initialized.");
            return false;
        }

        m_useDynamicGraspAdding = m_vrMode;

        if (m_currentLabels.Count > 0)
            return false;

        if (m_vrMode)
        {
            foreach (XRNode node in new List<XRNode>() { XRNode.LeftHand, XRNode.RightHand })
            {
                GameObject go = new GameObject();
                go.name = "Marker";
                go.transform.SetParent(transform);
                LineRenderer r = go.AddComponent<LineRenderer>();
                r.material = new Material(Shader.Find("Sprites/Default"));
                r.widthMultiplier = 0.01f;
                r.positionCount = 2;
                r.startColor = Color.gray;
                r.endColor = Color.red;
                m_markers[node] = go.transform;
            }
        }
        else
        {
            if (Camera.main != null) Camera.main.tag = "Untagged";
            transform.Find("Camera UI").gameObject.SetActive(true);
            transform.Find("Camera UI").GetComponent<Camera>().tag = "MainCamera";
        }

        _handImage_LeftEnabled = Resources.Load<Sprite>("GraspStudio/Graphics/Hand Toggle L Selected");
        _handImage_RightEnabled = Resources.Load<Sprite>("GraspStudio/Graphics/Hand Toggle R Selected");
        _statusIcon_Primary = Resources.Load<Sprite>("GraspStudio/Graphics/Icon_Star_Black");
        _statusIcon_Disabled = Resources.Load<Sprite>("GraspStudio/Graphics/Icon_Disable_Black");
        m_catalogueTilePrefab = Resources.Load<RectTransform>("GraspStudio/Catalogue Tile");
        VG_Controller.GetBone(m_editorAvatarID, VG_HandSide.LEFT, VG_BoneType.WRIST, out m_hands[0]);
        VG_Controller.GetBone(m_editorAvatarID, VG_HandSide.RIGHT, VG_BoneType.WRIST, out m_hands[1]);
        if (FindObjectOfType<VG_MainScript>().m_sensors[0].m_avatars[0].m_urdf == VG_UrdfType.HUMANOID_HAND)
            m_rootHand = FindObjectOfType<VG_MainScript>().m_sensors[0].m_avatars[0].m_skeletalMesh.transform.parent;
        else m_rootHand = FindObjectOfType<VG_MainScript>().m_sensors[0].m_avatars[0].m_robotTCP;

        foreach (Transform t in VG_Controller.GetSelectableObjects())
            m_objects.Add(t);
        
        if (m_objects.Count == 0)
        {
            VG_Debug.LogWarning("No objects in scene. Deactivating grasp studio component.");
            this.enabled = false;
            return false;
        }

        m_currentLabels.Add(VG_GraspLabel.DISABLED, new HashSet<int>());
        m_currentLabels.Add(VG_GraspLabel.PRIMARY, new HashSet<int>());
        m_allButtons = GetComponentsInChildren<Button>().ToList();

        // Create the tiles.
        float xIncrement = 1.0f / m_catalogueTileSize.x;
        float yIncrement = 1.0f / m_catalogueTileSize.y;
        m_catalogueTiles = new RectTransform[(int)(m_catalogueTileSize.x * m_catalogueTileSize.y)];
        for (int y = 0, i = 0; y < m_catalogueTileSize.y; y++)
        {
            for (int x = 0; x < m_catalogueTileSize.x; x++, i++)
            {
                m_catalogueTiles[i] = GameObject.Instantiate(m_catalogueTilePrefab, m_catalogueView);
                m_catalogueTiles[i].name = "Thumbnail_" + i;
                m_catalogueTiles[i].anchorMin = new Vector2(xIncrement * x, yIncrement * (m_catalogueTileSize.y - y - 1));
                m_catalogueTiles[i].anchorMax = new Vector2(xIncrement * (x + 1), yIncrement * (m_catalogueTileSize.y - y));

                m_catalogueTiles[i].offsetMin = Vector2.one *  0.01f;
                m_catalogueTiles[i].offsetMax = Vector2.one * -0.01f;

                BoxCollider boxCollider = m_catalogueTiles[i].GetComponent<BoxCollider>();
                boxCollider.size = new Vector3(m_catalogueTiles[i].rect.width, m_catalogueTiles[i].rect.height, 0.025f);
                boxCollider.center = new Vector3(0.0f, 0.0f, boxCollider.size.z * 0.5f);

                m_catalogueTiles[i].anchoredPosition3D = new Vector3(0.0f, 0.0f, -boxCollider.size.z);

                Image statusIconImage = m_catalogueTiles[i].GetChild(0).GetComponent<Image>();
                statusIconImage.enabled = false;
            }
        }

        m_numCatalogueTiles = m_catalogueTiles.Length;

        VG_Controller.SetSensorActive(m_editorAvatarID, m_currentSide == VG_HandSide.LEFT ? VG_HandSide.RIGHT : VG_HandSide.LEFT, false, Vector3.one * 1000);

        SwitchToObject(0);
        if (m_hideAllObjects)
            foreach (Transform obj in m_objects)
                obj.gameObject.SetActive(false);
            
        Check();
        UpdateGraspSet();
        UpdateGraspRange(0, true);

        m_initialized = true;
        return true;
    }

    bool RectangleClicked(RectTransform rect, out Vector2 localPoint)
    {
        if (rect == null) { localPoint = Vector2.zero; return false; }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, Camera.main, out localPoint);
        return rect.rect.Contains(localPoint);
    }

    void CheckRaycast(VG_HandSide side, bool triggered)
    {
        if (!m_markers[side == VG_HandSide.LEFT ? XRNode.LeftHand : XRNode.RightHand].TryGetComponent<LineRenderer>(out LineRenderer line)
            || m_selectionMode == SelectionMode.NONE)
        {
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.zero);
            return;
        }

        Transform t = null;
        Vector3 dir = Vector3.up;
        switch (m_selectionMode)
        {
            case SelectionMode.HEAD:
                if (Camera.main != null)
                {
                    t = Camera.main.transform;
                    dir = t.TransformDirection(Vector3.forward);
                }
                break;
            case SelectionMode.HANDS:
                if (VG_Controller.GetBone(m_gameAvatarID, side, VG_BoneType.WRIST, out t) == VG_ReturnCode.SUCCESS)
                    dir = t.TransformDirection(Vector3.up + Vector3.forward);
                break;
            case SelectionMode.FINGER:
                if (VG_Controller.GetBone(m_gameAvatarID, side, VG_BoneType.WRIST, out t) == VG_ReturnCode.SUCCESS && Camera.main != null)
                    dir = (t.position - Camera.main.transform.position).normalized;
                break;
        }

        if (t == null) return;        
        if (Physics.Raycast(t.position, dir, out RaycastHit hit, Mathf.Infinity))
        {
            line.SetPosition(0, (m_selectionMode == SelectionMode.HEAD) ? hit.point - .1f * dir : t.position);
            line.SetPosition(1, hit.point);
            if (triggered)
            {
                Button btn = hit.transform.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.Invoke();
                    if (btn.name.Contains("Thumbnail_") && int.TryParse(btn.name.Split('_').Last(), out int idx))
                        ClickedThumbnail(idx);
                }
            }
        }
        else
        {
            line.SetPosition(0, t.position);
            line.SetPosition(1, t.position + .01f * dir);
        }
    }

    public void AddGrasp()
    {
        AddGrasp(m_currentSide);
    }

    void AddGrasp(VG_HandSide handSide)
    {
        if (!m_useDynamicGraspAdding) return;

        if (VG_Controller.EditGrasp(m_gameAvatarID, handSide, VG_EditorAction.ADD_CURRENT, m_allowAirGrasps ? m_dynamicObject : null) == VG_ReturnCode.SUCCESS)
        {
            ToggleHand(handSide == VG_HandSide.LEFT ? -1 : 1);
            UpdateGraspSet();
            m_currentGraspIndex = m_currentGraspIds.Count - 1;
            UpdateGraspRange(m_currentGraspIds.Count - m_currentGraspIds.Count % m_numCatalogueTiles, true);
        }
    }

    public void Update()
    {
        if (!m_initialized && !Initialize())
            return;

        if (Input.mouseScrollDelta.y > 0) NextObject();
        if (Input.mouseScrollDelta.y < 0) PreviousObject();
        if (Input.GetKeyDown(KeyCode.PageUp)) NextObject();
        if (Input.GetKeyDown(KeyCode.PageDown)) PreviousObject();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousGrasp();
        if (Input.GetKeyDown(KeyCode.RightArrow)) NextGrasp();
        if (Input.GetKeyDown(KeyCode.UpArrow)) PreviousPage();
        if (Input.GetKeyDown(KeyCode.DownArrow)) NextPage();
        if (Input.GetKeyDown(KeyCode.P)) SetPrimaryGrasp();
        if (Input.GetKeyDown(KeyCode.LeftControl)) ToggleHand(-1);
        if (Input.GetKeyDown(KeyCode.RightControl)) ToggleHand(1);

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.LeftShift)) DisableAllGrasps();
            else DisableGrasp();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            AddGrasp(VG_HandSide.LEFT);
            AddGrasp(VG_HandSide.RIGHT);
        }

        if (m_formerObjectIndex != m_objectIndex)
            SwitchToObject(m_objectIndex);
        if (m_formerGraspIndex != m_currentGraspIndex)
        {
            int idx = m_currentGraspIndex - m_currentPageIndex;
            for (int i = 0; i < m_catalogueTiles.Length; i++)
                m_catalogueTiles[i].gameObject.GetComponent<Image>().color = (i == idx) ? new Color(.9f, .9f, .9f) : Color.white;
        }

        // Rotate the object if main view is clicked
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            if (RectangleClicked(m_mainView, out Vector2 localPoint))
            {
                localPoint /= -m_mainView.rect.size / 360.0f;
                m_targetRotation = Quaternion.Euler(-localPoint.y, localPoint.x, 0);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Invoke actions if any button clicked
            foreach (Button b in m_allButtons)
            {
                if (b == null) continue;
                if (RectangleClicked(b.gameObject.GetComponent<RectTransform>(), out _))
                    b.onClick.Invoke();
            }

            // Switch to different grasp if any thumbnail clicked
            foreach (RectTransform b in m_catalogueTiles)
                if (RectangleClicked(b, out _) && int.TryParse(b.name.Split('_').Last(), out int idx))
                    ClickedThumbnail(idx);
        }
    }

    public void Check()
    {
        if (!m_initialized) return;

        if (m_vrMode)
        {
            // In VR case
            XRNode controller;
            XRNode mainController = XRNode.RightHand;
            XRNode otherController = mainController == XRNode.LeftHand ? XRNode.RightHand : XRNode.LeftHand;
            
            int angle;
            int n = 4; // split all joystick movements into n sections
            foreach (XRNode node in new List<XRNode>() { XRNode.LeftHand, XRNode.RightHand})
            {
                int section = -1;
                InputDevice device = InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid)
                {
                    if (device.name.Contains(mainController == XRNode.LeftHand ? "- Left" : "- Right"))
                        controller = mainController;
                    else if (device.name.Contains(mainController == XRNode.LeftHand ? "- Right" : "- Left"))
                        controller = otherController;
                    else continue;

                    // Toggle between left and right hand using grip button
                    device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripped);
                    if (gripped != m_controllerPrimaryGripped[controller])
                    {
                        m_controllerPrimaryGripped[controller] = gripped;
                        if (gripped) ToggleHand(device.name.Contains("- Left") ? -1 : 1);
                    }

                    device.TryGetFeatureValue(CommonUsages.primaryButton, out bool pushed);
                    if (pushed != m_controllerPrimaryPushed[controller])
                    {
                        m_controllerPrimaryPushed[controller] = pushed;
                        if (pushed) AddGrasp(device.name.Contains("- Left") ? VG_HandSide.LEFT : VG_HandSide.RIGHT);
                    }

                    // Determine the section that is selected by the joystick
                    device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis);
                    if (axis.magnitude > 0.2f)
                    {
                        angle = 180 - (int)(Mathf.Rad2Deg * Mathf.Atan2(axis.y, axis.x));
                        section = (n * (angle + 180 / n) / 360) % n;
                    }
                    else m_controllerJoystickSection[controller] = -1;
                }

                VG_HandSide side = (node == XRNode.LeftHand) ? VG_HandSide.LEFT : VG_HandSide.RIGHT;
                bool triggered = VG_Controller.GetGrabStrength(m_gameAvatarID, side) > 0.5f;
                if (triggered != m_controllerPrimaryTriggered[node])
                {
                    m_controllerPrimaryTriggered[node] = triggered;
                    if (triggered) CheckRaycast(side, true);
                }
                else CheckRaycast(side, false);

                // One controller is used for labeling current grasp
                if (node == mainController)
                {
                    if (section != m_controllerJoystickSection[mainController])
                    {
                        m_controllerJoystickSection[mainController] = section;
                        if (section >= 0 && m_labelButtons[section] != null)
                            m_labelButtons[section].onClick.Invoke();
                    }
                }

                // Other controller is used for stepping through current object/grasp
                if (node == otherController)
                {
                    if (section != m_controllerJoystickSection[otherController])
                    {
                        m_controllerJoystickSection[otherController] = section;
                        switch (section)
                        {
                            case 0: PreviousGrasp(); break; // left
                            case 1: NextObject(); break; // up
                            case 2: NextGrasp(); break; // right
                            case 3: PreviousObject(); break; // down
                        }
                    }
                }
            }

            // In VR mode, use rotation for object rotation.

            VG_Controller.GetBone(m_gameAvatarID, VG_HandSide.LEFT, VG_BoneType.WRIST, out _, out Vector3 pl, out Quaternion ql);
            VG_Controller.GetBone(m_gameAvatarID, VG_HandSide.RIGHT, VG_BoneType.WRIST, out _, out Vector3 pr, out Quaternion qr);
            m_targetRotation = (otherController == XRNode.LeftHand) ? ql : qr;

            if (VG_Controller.GetGrabStrength(m_gameAvatarID, VG_HandSide.LEFT) > 0.5f &&
                VG_Controller.GetGrabStrength(m_gameAvatarID, VG_HandSide.RIGHT) > 0.5f)
            {
                PlaceDynamicObject();

                // Place object to annotate and place/scale GraspStudio when both triggers are pressed by moving hands.

                float d = (pl - pr).magnitude;
                if (d > 0.5f)
                {
                    if (Camera.current != null)
                    {
                        Transform ct = Camera.current.transform;
                        transform.SetPositionAndRotation(ct.position + 2 * ct.forward, ct.rotation);
                    }
                    transform.localScale = Mathf.Max(1.0f, m_guiScale * d) * Vector3.one;
                }
            }
        }

        // Set main view grasp and thumbnail grasps to correct pose
        if (m_selectedObject != null)
        {
            m_selectedObject.SetPositionAndRotation(
                m_mainView.position + (m_mainView.forward * -0.15f) +
                m_targetRotation * m_objectCenterOffset, m_targetRotation);

            // Update main view label
            _currentSelectionObjectLabel.text = m_selectedObject.name +
                "\n object (" + (m_objectIndex + 1) + "/" + m_objects.Count + ")" +
                " grasp (" + (m_currentGraspIndex + 1) + "/" + m_currentGraspIds.Count + ")";
        }

        for (int i = 0; i < m_displayGrasps.Count; i++)
        {
            m_displayGrasps[i].SetPositionAndRotation(
                m_catalogueTiles[i].position + (m_catalogueView.forward * -0.0625f) +
                m_displayGraspScale * (m_targetRotation * m_objectCenterOffset), m_targetRotation);
        }
        
        if (!VG_Controller.IsEnabled()) return;

        if (VG_Controller.GetNumGrasps(m_selectedObject, m_editorAvatarID, m_currentSide) > 0)
        {
            if (m_rootHand.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                // If human hand, update all fingers from library
                VG_Controller.GetGrasp(m_selectedObject, m_editorAvatarID, m_currentSide, m_currentGraspIndex, out _, out _, out _, out _, out _, VG_QueryGraspMode.MOVE_HAND_DIRECTLY);
            }
            else
            {
                // If robot hand, retrieve and set TCP position
                VG_Controller.GetGrasp(m_selectedObject, m_editorAvatarID, m_currentSide, m_currentGraspIndex, out _, out Vector3 p, out Quaternion q, out _, out _, VG_QueryGraspMode.NO_MOVE, VG_QueryGraspMethod.BY_TCP);
                m_rootHand.SetPositionAndRotation(p, q);
            }
        }
    }

    public void UpdateGraspSet()
    {
        m_currentGraspIds.Clear();
        for (int i = 0; i < VG_Controller.GetNumGrasps(m_selectedObject, m_editorAvatarID, m_currentSide); i++)
        {
            VG_Controller.GetGrasp(m_selectedObject, m_editorAvatarID, m_currentSide, i, out int graspId, out _, out _, out _, out VG_GraspLabel label, VG_QueryGraspMode.NO_MOVE);
            if (label != VG_GraspLabel.DISABLED && m_showEnabled) m_currentGraspIds.Add(graspId);
            if (label == VG_GraspLabel.DISABLED && m_showDisabled) m_currentGraspIds.Add(graspId);
        }
        RefreshThumbnailStatusIcons();
    }

    private void UpdateGraspRange(int start, bool trigger = false)
    {
        if (start < 0) return;
        if (!trigger && m_currentPageIndex == start) return;
        
        m_currentPageIndex = start;

        _currentSelectionIndicesLabel.text = (m_currentPageIndex + 1) + " - " + Mathf.Min(m_currentGraspIds.Count, m_currentPageIndex + m_numCatalogueTiles).ToString() + " of " + m_currentGraspIds.Count.ToString();

        for (int i = 0, iCount = m_displayGrasps.Count; i < iCount; i++)
            Destroy(m_displayGrasps[i].gameObject);
        m_displayGrasps.Clear();

        RefreshThumbnailStatusIcons();

        bool humanHand = (m_rootHand.GetComponentInChildren<SkinnedMeshRenderer>() != null);
        for (int idx = 0; idx < m_numCatalogueTiles; idx++)
        {
            if (m_currentPageIndex + idx > m_currentGraspIds.Count - 1)
                break;

            if (humanHand)
            {
                // If human hand, update all fingers from library
                if (VG_Controller.GetGrasp(m_selectedObject, m_editorAvatarID, m_currentSide, m_currentPageIndex + idx, out _, out _, out _, out _, out _, VG_QueryGraspMode.MOVE_HAND_DIRECTLY)
                     != VG_ReturnCode.SUCCESS) continue;
            }
            else
            {
                // If robot hand, retrieve and set TCP position
                if (VG_Controller.GetGrasp(m_selectedObject, m_editorAvatarID, m_currentSide, m_currentPageIndex + idx, out _, out Vector3 p, out Quaternion q, out _, out _, VG_QueryGraspMode.NO_MOVE, VG_QueryGraspMethod.BY_TCP)
                     != VG_ReturnCode.SUCCESS) continue;
                else m_rootHand.SetPositionAndRotation(p, q);
            }

            Transform newPoseRoot = new GameObject("Grasp_" + (m_currentPageIndex + idx + 1).ToString()).transform;
            newPoseRoot.SetPositionAndRotation(m_selectedObject.position, m_selectedObject.rotation);
            GameObject.DestroyImmediate(m_selectedObject.GetComponent<VG_Articulation>());
            Instantiate(m_selectedObject.gameObject, m_selectedObject.position, m_selectedObject.rotation, newPoseRoot);
            m_selectedObject.gameObject.AddComponent<VG_Articulation>();
            Instantiate(m_rootHand.gameObject, m_rootHand.position, m_rootHand.rotation, newPoseRoot);

            newPoseRoot.SetPositionAndRotation(m_catalogueTiles[idx].position + (m_catalogueView.forward * -0.0625f), m_targetRotation);
            newPoseRoot.localScale = Vector3.one * m_displayGraspScale;
            newPoseRoot.SetParent(transform);

            m_displayGrasps.Add(newPoseRoot);
        }
    }

    public void SetPrimaryGrasp()
    {
        int id = m_currentGraspIds[m_currentGraspIndex];
        if (m_currentLabels[VG_GraspLabel.DISABLED].Contains(id))
        {
            Debug.LogWarning("This grasp is disabled. Not using it for primary.");
            return;
        }
        if (m_currentLabels[VG_GraspLabel.PRIMARY].Contains(id))
        {
            VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.CLEAR_PRIMARY, m_selectedObject);
            m_currentLabels[VG_GraspLabel.PRIMARY].Remove(id);
            foreach (int idx in m_currentLabels[VG_GraspLabel.PRIMARY])
                VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.PRIMARY_CURRENT, m_selectedObject, idx);
            UpdateGraspSet();
            return;
        }

        VG_ReturnCode code = VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.PRIMARY_CURRENT, m_selectedObject, m_currentGraspIds[m_currentGraspIndex]);
        if (code == VG_ReturnCode.SUCCESS) RefreshThumbnailStatusIcons();
        else Debug.Log("Set primary grasp return code: " + code);
    }

    public void DisableAllGrasps()
    {
        if (m_currentLabels[VG_GraspLabel.DISABLED].Contains(m_currentGraspIds[m_currentGraspIndex]))
        {
            m_currentLabels[VG_GraspLabel.DISABLED].Clear();
            VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.CLEAR_DISABLED, m_selectedObject);
        }
        else foreach (int id in m_currentGraspIds)
        {
            if (!m_currentLabels[VG_GraspLabel.PRIMARY].Contains(id))
                VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.DISABLE_CURRENT, m_selectedObject, id);
        }
        UpdateGraspSet();
    }

    public void DisableGrasp()
    {
        int id = m_currentGraspIds[m_currentGraspIndex];
        if (m_currentLabels[VG_GraspLabel.DISABLED].Contains(id))
        {
            // If already disabled, remove.
            VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.DELETE_CURRENT, m_selectedObject);
            m_currentLabels[VG_GraspLabel.DISABLED].Remove(id);

            /*
            // If already disabled, enable again.
            VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.CLEAR_DISABLED, m_selectedObject);
            m_currentLabels[VG_GraspLabel.DISABLED].Remove(id);
            foreach (int idx in m_currentLabels[VG_GraspLabel.DISABLED])
                VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.DISABLE_CURRENT, m_selectedObject, idx);
            */

            UpdateGraspSet();
            UpdateGraspRange(m_currentPageIndex, true);
            return;
        }

        VG_ReturnCode code = VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.DISABLE_CURRENT, m_selectedObject, m_currentGraspIds[m_currentGraspIndex]);
        if (code == VG_ReturnCode.SUCCESS) UpdateGraspSet();
        else Debug.Log("Disable grasp return code: " + code);
    }

    public void DeleteGrasp()
    {
        VG_ReturnCode code = VG_Controller.EditGrasp(m_editorAvatarID, m_currentSide, VG_EditorAction.DELETE_CURRENT, m_selectedObject, m_currentGraspIds[m_currentGraspIndex]);
        if (code == VG_ReturnCode.SUCCESS) UpdateGraspSet();
        else Debug.Log("Delete grasp return code: " + code);
    }

    private void SwitchToLabel(Image image, VG_GraspLabel label)
    {
        switch (label)
        {
            case VG_GraspLabel.PRIMARY:
                image.sprite = _statusIcon_Primary;
                image.enabled = true;
                break;
            case VG_GraspLabel.DISABLED:
                image.sprite = _statusIcon_Disabled;
                image.enabled = true;
                break;
            default:
                image.enabled = false;
                break;
        }
    }

    public void RefreshThumbnailStatusIcons()
    {
        int numGrasps = VG_Controller.GetNumGrasps(m_selectedObject, m_editorAvatarID, m_currentSide);
        for (int idx = 0; idx < m_numCatalogueTiles; idx++)
        {
            if (m_currentPageIndex + idx >= numGrasps ||
                VG_Controller.GetGrasp(m_selectedObject, m_editorAvatarID, m_currentSide, m_currentPageIndex + idx, out _, out _, out _, out _, out VG_GraspLabel label, VG_QueryGraspMode.NO_MOVE)
                != VG_ReturnCode.SUCCESS)
            {
                SwitchToLabel(m_catalogueTiles[idx].GetChild(0).GetComponent<Image>(), VG_GraspLabel.FAILED);
                m_catalogueTiles[idx].GetComponentInChildren<Text>().text = "";
                continue;
            }

            SwitchToLabel(m_catalogueTiles[idx].GetChild(0).GetComponent<Image>(), label);
            m_catalogueTiles[idx].GetComponentInChildren<Text>().text = (m_currentPageIndex + idx + 1).ToString();

            if (m_currentLabels.ContainsKey(label))
                m_currentLabels[label].Add(m_currentGraspIds[m_currentPageIndex + idx]);
        }
    }

    public void ToggleHand(int handId)
    {
        VG_HandSide hand = (VG_HandSide)handId;
        if (m_currentSide == hand || hand == VG_HandSide.UNKNOWN_HANDSIDE) return;

        if (hand == VG_HandSide.LEFT) transform.Find("Canvas/HandImage").GetComponent<Image>().sprite = _handImage_LeftEnabled;
        if (hand == VG_HandSide.RIGHT) transform.Find("Canvas/HandImage").GetComponent<Image>().sprite = _handImage_RightEnabled;

        VG_Controller.SetSensorActive(m_editorAvatarID, m_currentSide, false, Vector3.one * 1000);
        m_currentSide = hand;
        VG_Controller.SetSensorActive(m_editorAvatarID, m_currentSide, true);

        m_currentGraspIndex = 0;
        UpdateGraspSet();
        UpdateGraspRange(0, true);
    }

    public void ToggleActiveGrasps() 
    {
        m_showEnabled = !m_showEnabled;
        _button_ToggleEnabledGrasps.transform.GetChild(0).gameObject.SetActive(m_showEnabled);
        UpdateGraspSet();
        UpdateGraspRange(0, true);
    }

    public void ToggleDisabledGrasps() 
    {
        m_showDisabled = !m_showDisabled;
        _button_ToggleDisabledGrasps.transform.GetChild(0).gameObject.SetActive(m_showDisabled);
        UpdateGraspSet();
        UpdateGraspRange(0, true);
    }

    public void NextGrasp()
    {
        m_currentGraspIndex = m_currentGraspIndex + 1;
        if (m_currentGraspIndex == m_currentGraspIds.Count || m_currentGraspIndex > m_currentPageIndex + m_numCatalogueTiles - 1)
            NextPage(true);
        RefreshThumbnailStatusIcons();
    }

    public void PreviousGrasp()
    {
        m_currentGraspIndex = m_currentGraspIndex - 1;
        if (m_currentGraspIndex < 0)
        {
            PreviousPage(true);
            return;
        }
        m_currentGraspIndex = Mathf.Clamp(m_currentGraspIndex, 0, m_currentGraspIds.Count - 1);
        if (m_currentGraspIndex < m_currentPageIndex)
            PreviousPage(true);
        RefreshThumbnailStatusIcons();
    }

    public void ClickedThumbnail(int index)
    {
        m_currentGraspIndex = Mathf.Min(m_currentPageIndex + index, m_currentGraspIds.Count);
        RefreshThumbnailStatusIcons();
    }

    public void NextPage(bool firstGrasp = false) 
    {
        m_currentPageIndex += m_numCatalogueTiles;
        if (m_currentGraspIds.Count <= m_currentPageIndex)
            m_currentPageIndex = 0;
        if (firstGrasp) m_currentGraspIndex = m_currentPageIndex;
        else m_currentGraspIndex = Mathf.Min(m_currentGraspIndex + m_numCatalogueTiles, m_currentGraspIds.Count);
        UpdateGraspRange(m_currentPageIndex, true);
    }

    public void PreviousPage(bool lastGrasp = false)
    {
        if (m_currentPageIndex == 0)
            m_currentPageIndex = (m_currentGraspIds.Count / m_numCatalogueTiles) * m_numCatalogueTiles;
        else
        {
            m_currentPageIndex -= m_numCatalogueTiles;
            if (m_currentPageIndex < 0) m_currentPageIndex = 0;
        }
        if (lastGrasp) m_currentGraspIndex = Mathf.Min(m_currentPageIndex + m_numCatalogueTiles - 1, m_currentGraspIds.Count - 1);
        else m_currentGraspIndex = Mathf.Max(m_currentGraspIndex - m_numCatalogueTiles, 0);
        UpdateGraspRange(m_currentPageIndex, true);
    }

    public void NextObject()
    {
        m_formerObjectIndex = m_objectIndex;
        SwitchToObject(m_objectIndex + 1);
    }

    public void PreviousObject()
    {
        m_formerObjectIndex = m_objectIndex;
        SwitchToObject(m_objectIndex - 1);
    }

    void PlaceDynamicObject()
    {
        if (!m_useDynamicGraspAdding ||
            VG_Controller.GetHand(m_gameAvatarID, VG_HandSide.LEFT).IsHolding() ||
            VG_Controller.GetHand(m_gameAvatarID, VG_HandSide.RIGHT).IsHolding()) 
            return;
        VG_Controller.GetSensorPose(m_gameAvatarID, VG_HandSide.LEFT, out Vector3 p1, out _);
        VG_Controller.GetSensorPose(m_gameAvatarID, VG_HandSide.RIGHT, out Vector3 p2, out _);
        m_dynamicObject.position = (p1 + p2) / 2 + m_dynamicObject.rotation * m_objectCenterOffset;
        VG_Controller.SetSynthesisMethodForSelectedObject(m_gameAvatarID, VG_HandSide.LEFT, VG_SynthesisMethod.DYNAMIC_GRASP);
        VG_Controller.SetSynthesisMethodForSelectedObject(m_gameAvatarID, VG_HandSide.RIGHT, VG_SynthesisMethod.DYNAMIC_GRASP);
    }

    void SwitchToObject(int index)
    {
        if (m_objects.Count < 1) return;

        index = Mathf.Clamp(index, 0, m_objects.Count - 1);
        if (m_selectedObject != null)
        {
            //VG_Controller.DeleteDistalObjectAtRuntime(m_selectedObject);
            Destroy(m_selectedObject.gameObject);
        }
        bool same = (m_objectIndex == index);
        m_objectIndex = index;
        m_selectedObject = Instantiate(m_objects[m_objectIndex]);
        m_selectedObject.name = m_objects[m_objectIndex].name;
        m_selectedObject.gameObject.SetActive(true);

        // Cache the diff between bounds and transform to keep the object centered
        m_objectCenterOffset = Quaternion.Inverse(m_selectedObject.rotation) * (m_selectedObject.position - m_selectedObject.GetComponent<Renderer>().bounds.center);
        m_selectedObject.position = m_mainView.position + (m_mainView.forward * -0.15f) + m_targetRotation * m_objectCenterOffset;
        
        Vector3 size = m_selectedObject.GetComponent<Renderer>().bounds.size;
        float max = 2 * Mathf.Max(Mathf.Max(size.x, size.y), size.z);
        m_displayGraspScale = Mathf.Min(0.35f, 0.3f / max);

        UpdateGraspSet();
        if (!same) m_currentGraspIndex = 0;
        UpdateGraspRange(same ? m_currentPageIndex : 0, true);
        m_formerObjectIndex = m_objectIndex;

        if (m_useDynamicGraspAdding && m_vrMode)
        {
            if (m_dynamicObject != null)
            {
                //VG_Controller.DeleteDistalObjectAtRuntime(m_dynamicObject);
                Destroy(m_dynamicObject.gameObject);
            }
            m_dynamicObject = Instantiate(m_objects[m_objectIndex]);
            m_dynamicObject.name = m_objects[m_objectIndex].name;
            m_dynamicObject.gameObject.SetActive(true);
            PlaceDynamicObject();
        }
    }
}
