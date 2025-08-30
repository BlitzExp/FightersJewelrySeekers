using UnityEngine;

public class DetectionZone : MonoBehaviour
{
    private AgentController parentAgent;

    void Start()
    {
        parentAgent = GetComponentInParent<AgentController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (parentAgent != null)
            parentAgent.OnDetectionTriggerEnter(other);
    }
}
