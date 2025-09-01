using UnityEngine;

//Class to detect objects in the detection zone of the agent
public class DetectionZone : MonoBehaviour
{
    // Reference to the parent AgentController
    private AgentController parentAgent;

    //Gets the controller of the parent Agent
    void Start()
    {
        parentAgent = GetComponentInParent<AgentController>();
    }

    //Triggers the function of the parent AgentController when an object is detected
    void OnTriggerEnter(Collider other)
    {
        if (parentAgent != null)
            parentAgent.OnDetectionTriggerEnter(other);
    }
}
