using UnityEngine;
using UnityEngine.Events;
using VirtualGrasp;

public class ButtonTrigger : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onButtonOff;

    [SerializeField]
    private UnityEvent onButtonOn;

    private VG_Articulation articulation;
    
    private void Awake()
    {
        articulation = GetComponent<VG_Articulation>();
    }

    void Update()
    {
        float state = VG_Controller.GetObjectJointState(transform);

        if(state == articulation.m_min)
        {
            onButtonOff?.Invoke();
        }
        else if(state == articulation.m_max)
        {
            onButtonOn?.Invoke();
        }
    }
}
