using UnityEngine;
using System.Collections.Generic;

// Class to store shared knowledge among all agents
public class SharedKnowledge : MonoBehaviour
{

    // Variables shared among all agents
    static public int numberOfAgents = 3;
    static public int totalnumberOfGems;
    static public int numberOfGreenGems;
    static public int numberOfRedGems;
    static public int numberOfBlueGems;
    static public int missingGems;

    //Variables to keep track of the board current stae for each Agent
    static public HashSet<Vector3> VisitedPositions = new HashSet<Vector3>();
    static public HashSet<Vector3> BlockedPositionsRed = new HashSet<Vector3>();
    static public HashSet<Vector3> BlockedPositionsBlue = new HashSet<Vector3>();
    static public HashSet<Vector3> BlockedPositionsGreen = new HashSet<Vector3>();
    static public List<Vector3> AgentPositions = new List<Vector3>();
    static public List<Vector3> AgentNextPositions = new List<Vector3>();
    static public List<Vector3> MissingPos = new List<Vector3>();

    //Positions of the Chests
    static public Vector3 blueChest;
    static public Vector3 redChest;
    static public Vector3 greenChest;

    //Gems which have been found but have not yet been collected
    static public List<Vector3> redGemsFound = new List<Vector3>();
    static public List<Vector3> greenGemsFound = new List<Vector3>();
    static public List<Vector3> blueGemsFound = new List<Vector3>();
    static public List<Vector3> ReservedGems = new List<Vector3>();

    //Stadistics Variabñes
    static public int numberOfCollisions = 0;
    static public int numberOfMovements = 0;

    //Grid representation
    static public Vector3[,] grid;

    //Timer
    [SerializeField] GameTimer gameTimer;


    //Initialize static variables
    void Start()
    {
        missingGems = totalnumberOfGems;
        BlockedPositionsRed.Add(blueChest);
        BlockedPositionsBlue.Add(blueChest);
        BlockedPositionsGreen.Add(blueChest);
        BlockedPositionsRed.Add(redChest);
        BlockedPositionsBlue.Add(redChest);
        BlockedPositionsGreen.Add(redChest);
        BlockedPositionsRed.Add(greenChest);
        BlockedPositionsBlue.Add(greenChest);
        BlockedPositionsGreen.Add(greenChest);
    }

    void Update()
    {
        // Game ending mecanism
        if (missingGems == 0) 
        {
            gameTimer.PauseTimer();
            Debug.Log("All gems collected!");
            Debug.Log($"Total Collisions: {numberOfCollisions}");
            Debug.Log($"Time taken: {gameTimer.elapsedTime}");
            Debug.Log($"Total Movements: {numberOfMovements}");

            //Pausar todo el juego
            Time.timeScale = 0f;
            this.enabled = false;
        }
    }
}
