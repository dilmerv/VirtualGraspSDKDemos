using UnityEngine;

public class TeleportRig : MonoBehaviour
{
    [SerializeField]
    private GameObject[] targets;

    [SerializeField]
    private GameObject[] rigComponents;

    private int currentTarget;

    public void Teleport()
    {
        if (targets?.Length == 0|| rigComponents?.Length == 0)
            Debug.LogError("Teleport Rig is not configured");

        foreach (var c in rigComponents)
        {
            c.transform.position = new Vector3(targets[0].transform.position.x, c.transform.position.y, c.transform.position.z);
            currentTarget++;
            if (currentTarget == targets.Length) currentTarget = 0;
        }
    }
}
