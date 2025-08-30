using UnityEngine;

public class ChestTag : MonoBehaviour
{
    public enum ColorOption
    {Blue,Red,Green}

    [SerializeField] private ColorOption ChestColorValue;

    public ColorOption getChestColor()
    {
        return ChestColorValue;
    }
}
