using UnityEngine;

public class TeleportRig : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    [SerializeField]
    private GameObject[] rigComponents;

    public void Teleport()
    {
        foreach (var r in rigComponents)
        {
            r.transform.position = new Vector3(target.transform.position.x, r.transform.position.y, r.transform.position.z);
        }
    }
}
