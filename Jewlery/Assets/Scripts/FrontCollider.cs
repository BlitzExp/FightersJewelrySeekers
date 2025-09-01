using UnityEngine;

//Class to detect front objects and avoid collisions

public class FrontCollider : MonoBehaviour
{
    // Reference to the parent AgentController
    private AgentController parentAgent;

    //Gets the controller of the parent Agent
    void Start()
    {
        parentAgent = GetComponentInParent<AgentController>();
    }

    //Triggers the function of the parent AgentController when an obstacle is detected
    void OnTriggerEnter(Collider other)
    {
        if (parentAgent != null)
            parentAgent.OnDetectionObstacleTriggerEnter(other);
    }

}
