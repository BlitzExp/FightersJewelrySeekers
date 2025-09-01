using UnityEngine;

// Class to identify the gem type and if it has been collected
public class GemType : MonoBehaviour
{
    // Color of the gem
    public enum ColorOption {Blue,Red,Green}
    [SerializeField] private ColorOption GemColor;

    //State of the gem (collected or not)
    private bool isCollected = false;

    //Position of the gem
    private Vector3 position;

    // Property to access the gem color
    public ColorOption GemColorValue => GemColor;

    // Variable to know if it is active
    public bool IsCollected => isCollected;

    // Function to collect the gem
    public void CollectGem()
    {
        isCollected = true;
        Debug.Log($"Gem {GemColor} collected!");
    }

    // Functions to set and get the position of the gem
    public void setPos(Vector3 pos) 
    {
        position = pos;
    }
    public Vector3 getPos()
    {
        return position;
    }
}
