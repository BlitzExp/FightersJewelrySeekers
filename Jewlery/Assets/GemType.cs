using UnityEngine;

public class GemType : MonoBehaviour
{
    public enum ColorOption {Blue,Red,Green}

    [SerializeField] private ColorOption GemColor;
    private bool isCollected = false;

    // Propiedad para consultar el color
    public ColorOption GemColorValue => GemColor;

    // Saber si ya fue recogida
    public bool IsCollected => isCollected;

    // Llamar cuando un agente recoge la gema
    public void CollectGem()
    {
        isCollected = true;
        Debug.Log($"Gem {GemColor} collected!");
    }
}
