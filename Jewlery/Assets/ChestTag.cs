using UnityEngine;

public class ChestTag : MonoBehaviour
{
    public enum ColorOption
    {Blue,Red,Green}

    [SerializeField] private ColorOption ChestColor;

    ColorOption getCemColor()
    {
        return ChestColor;
    }
}
