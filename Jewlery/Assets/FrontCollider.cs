using UnityEngine;

public class FrontCollider : MonoBehaviour
{
    private AgentController parentAgent;

    void Start()
    {
        parentAgent = GetComponentInParent<AgentController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (parentAgent != null)
            parentAgent.OnDetectionObstacleTriggerEnter(other);
    }

}
