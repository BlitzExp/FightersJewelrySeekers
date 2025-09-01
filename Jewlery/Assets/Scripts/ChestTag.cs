using UnityEngine;

//Class to identify the chests and their color
public class ChestTag : MonoBehaviour
{
    //Color of the chest
    public enum ColorOption
    {Blue,Red,Green}

    //Initialize it on the object
    [SerializeField] private ColorOption ChestColorValue;

    //Function to get the color of the chest
    public ColorOption getChestColor()
    {
        return ChestColorValue;
    }
}
