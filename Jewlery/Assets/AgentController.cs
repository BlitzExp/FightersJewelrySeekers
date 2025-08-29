using UnityEngine;
using UnityEngine.UI; // Necesario para Dropdown

public class AgentController : MonoBehaviour
{
    public enum ColorOption
    {
        Blue,
        Red,
        Green
    }

    [SerializeField] private ColorOption AgentColor;


    [SerializeField] private float speed = 10f;
    

    void Start()
    {

    }

    void Update()
    {

    }


}
