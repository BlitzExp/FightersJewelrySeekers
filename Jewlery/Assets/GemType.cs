using UnityEngine;

public class GemType : MonoBehaviour
{
    public enum ColorOption
    {
        Blue,
        Red,
        Green
    }

    private bool isCollected = false;

    [SerializeField] private ColorOption GemColor;

    ColorOption getGemColor() {
        return GemColor;
    }

    bool isGemCollected()
    {
        return isCollected;
    }

    void Start()
    {
        Debug.Log("Gem color: " + getGemColor());
    }

    void collectGem()
    {
        isCollected = true;
    }
}
