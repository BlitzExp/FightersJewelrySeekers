using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

// Class to configure the game environment, agents and jewels according to user parameters
public class GameConfiguration : MonoBehaviour
{

    // Setup of the simulation environment according to the user parameters
    [Header("Size of the agents")]
    [SerializeField] float AgentSize;

    [Header("X dimension of the stage >(5*AgentSize)")]
    [SerializeField] float UStage;
    [Header("Y dimension of the stage >(5*AgentSize)")]
    [SerializeField] float VStage;

    [Header("Jewel Size Relative to the Agent")]
    [SerializeField] float JewelSize;
    [Header("Number of Jewels per color")]
    [SerializeField] int numJewel;


    //It was accord that the mazimun number of colors would be 3 (Red, Blue and Green)
    [SerializeField] int numColor = 3;
    [Header("Number of Agents")]
    [SerializeField] int numAgent;

    [Header("StagePrefab")]
    [SerializeField] GameObject stage;


    [Header("Agents")]
    [SerializeField] GameObject rebAgent;
    [SerializeField] GameObject blueAgent;
    [SerializeField] GameObject greenAgent;

    [Header("JewelPrefab")]
    [SerializeField] Object redJewelGem;
    [SerializeField] Object blueJewelGem;
    [SerializeField] Object greenJewelGem;

    [SerializeField] Object redJewelRup;
    [SerializeField] Object blueJewelRup;
    [SerializeField] Object greenJewelRup;

    [SerializeField] Object redJewelRing;
    [SerializeField] Object blueJewelRing;
    [SerializeField] Object greenJewelRing;

    [Header("ChestPrefab")]
    [SerializeField] GameObject redChest;
    [SerializeField] GameObject blueChest;
    [SerializeField] GameObject greenChest;

    private Vector3[,] grid; // posiciones del grid
    private int gridWidth;
    private int gridHeight;
    private const float GRID_PERCENT = 0.8f;
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    //Limitations on the parameters
    private void OnValidate()
    {
        if (AgentSize <= 0) { AgentSize = 1; }
        if (UStage < (5 * AgentSize)) { UStage = 5 * AgentSize; }
        if (VStage < (5 * AgentSize)) { VStage = 5 * AgentSize; }
        if (JewelSize < 0.5 * AgentSize) { JewelSize = 0.5f * AgentSize; }else if (JewelSize > AgentSize * 2) { JewelSize = AgentSize*2; }


        if (numJewel < 1) { numJewel = 1; }
        if (numAgent < 3 ) { numAgent = 3; }
        if (numColor > 3) { numColor = 3; } else if (numColor < 1) { numColor = 1; }

    }

    //Initialize the environment, agents and jewels
    void Start()
    {
        if (stage != null)
        {
            stage.transform.localScale = new Vector3(UStage, UStage, VStage);
        }

        if (rebAgent != null)
        {
            rebAgent.transform.localScale = new Vector3(AgentSize, AgentSize, AgentSize);
        }
        if (blueAgent != null)
        {
            blueAgent.transform.localScale = new Vector3(AgentSize, AgentSize, AgentSize);
        }
        if (greenAgent != null)
        {
            greenAgent.transform.localScale = new Vector3(AgentSize, AgentSize, AgentSize);
        }

        // It is shared some information with all the agents, like number of gems, number of agents, grid positions, etc.
        SharedKnowledge.numberOfAgents = numAgent;
        SharedKnowledge.totalnumberOfGems = numJewel * numColor;
        SharedKnowledge.numberOfRedGems = numJewel;
        SharedKnowledge.numberOfBlueGems = numJewel;
        SharedKnowledge.numberOfGreenGems = numJewel;



        GenerateGrid();
        SharedKnowledge.grid = grid;
        SharedKnowledge.MissingPos = new List<Vector3>();
        foreach (Vector3 position in grid)
        {
            SharedKnowledge.MissingPos.Add(position);
        }
        spawnChests();
        SpawnAgents();
        SpawnJewels();
    }

    //Creates a Grid considering the size of the agent, and size of the display case
    void GenerateGrid()
    {
        // Calculate the area of th grid (90% of the stage) because of walls
        float gridUStage = UStage * GRID_PERCENT;
        float gridVStage = VStage * GRID_PERCENT;

        // Calculare the number of positions in the grid
        gridWidth = Mathf.FloorToInt(gridUStage / AgentSize);
        gridHeight = Mathf.FloorToInt(gridVStage / AgentSize);

        // Creates the vector for the grid
        grid = new Vector3[gridWidth, gridHeight];

        Vector3 stageCenter = stage != null ? stage.transform.position : Vector3.zero;
        float totalGridWidth = gridWidth * AgentSize;
        float totalGridHeight = gridHeight * AgentSize;

        // Adjust the grid soo that it is centered in the stage
        float startX = stageCenter.x - (totalGridWidth / 2) + (AgentSize / 2);
        float startZ = stageCenter.z - (totalGridHeight / 2) + (AgentSize / 2);

        // Fill the grid with positions
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                grid[x, z] = new Vector3(startX + x * AgentSize, stageCenter.y, startZ + z * AgentSize);
            }
        }
    }


    // Function to spawn the agents in the grid using the prefabs
    void SpawnAgents()
    {
        //Variable to identify the next color to spawn
        int next = 1;

        //Spawen agents
        for (int i = 0; i < numAgent; i++)
        {
            //Obtain a position on the grid
            Vector3 pos = GetRandomFreePosition();

            GameObject agentInstance = null;

            //Instantiate the agent according to the color order Red, Blue and Green
            if (next == 1 && rebAgent != null || numColor == 1)
            {
                agentInstance = Instantiate(rebAgent, pos, Quaternion.identity);
                next = 2;
            }
            else if (next == 2 && blueAgent != null && numColor >=2)
            {
                agentInstance = Instantiate(blueAgent, pos, Quaternion.identity);
                next = 3;
                if (numColor == 2) {
                    next = 1;
                }
            }
            else if (next == 3 && greenAgent != null && numColor == 3)
            {
                agentInstance = Instantiate(greenAgent, pos, Quaternion.identity);
                next = 1;
            }

            // Set the scale and rotation of the agent
            if (agentInstance != null)
            {
                agentInstance.transform.localScale = transform.localScale * AgentSize;
                agentInstance.transform.localRotation = Quaternion.Euler(-90 * i, agentInstance.transform.localRotation.eulerAngles.y, agentInstance.transform.localRotation.eulerAngles.z);
                // Add position to the shared knowledge
                SharedKnowledge.AgentPositions.Add(pos);
                SharedKnowledge.VisitedPositions.Add(pos);
                SharedKnowledge.MissingPos.Remove(pos);
                AgentController agentController = agentInstance.GetComponent<AgentController>();
            }

            // Mark the position as occupied
            occupiedPositions.Add(pos);
        }
    }


    // Function to spawn the chests in the corners of the grid using the prefabs
    void spawnChests()
    {
        if (stage == null) return;

        // Get the center of the stage
        Vector3 stageCenter = stage.transform.position;

        float gridUStage = UStage * GRID_PERCENT;
        float gridVStage = VStage * GRID_PERCENT;

        float halfWidth = gridUStage / 2;
        float halfHeight = gridVStage / 2;

        // Obtain the edges
        Vector3[] corners = new Vector3[]
        {
        new Vector3(stageCenter.x - halfWidth, stageCenter.y, stageCenter.z - halfHeight), // Position the chest down left
        new Vector3(stageCenter.x + halfWidth, stageCenter.y, stageCenter.z - halfHeight), // Position the chest down right
        new Vector3(stageCenter.x - halfWidth, stageCenter.y, stageCenter.z + halfHeight), // Position the chest up left
        new Vector3(stageCenter.x + halfWidth, stageCenter.y, stageCenter.z + halfHeight)  // Position the chest up right
        };

        // Create the chests in the corners
        if (redChest != null)
        {
            GameObject chest = Instantiate(redChest, corners[0], Quaternion.identity);
            chest.transform.localScale = chest.transform.localScale * AgentSize;
            chest.transform.localRotation = Quaternion.Euler(-90, chest.transform.localRotation.eulerAngles.y, chest.transform.localRotation.eulerAngles.z);
            occupiedPositions.Add(corners[0]);

            //Upload the position to the shared knowledge
            SharedKnowledge.redChest = corners[0];
            SharedKnowledge.VisitedPositions.Add(corners[0]);
            SharedKnowledge.MissingPos.Remove(corners[0]);
            SharedKnowledge.BlockedPositionsRed.Add(corners[0]);
            SharedKnowledge.BlockedPositionsBlue.Add(corners[0]);
            SharedKnowledge.BlockedPositionsGreen.Add(corners[0]);
        }

        if (blueChest != null && numColor >=2)
        {
            GameObject chest = Instantiate(blueChest, corners[1], Quaternion.identity);
            chest.transform.localScale = chest.transform.localScale * AgentSize;
            chest.transform.localRotation = Quaternion.Euler(-90, chest.transform.localRotation.eulerAngles.y, chest.transform.localRotation.eulerAngles.z);
            occupiedPositions.Add(corners[1]);
            SharedKnowledge.blueChest = corners[1];
            SharedKnowledge.VisitedPositions.Add(corners[1]);
            SharedKnowledge.MissingPos.Remove(corners[1]);
            SharedKnowledge.BlockedPositionsRed.Add(corners[0]);
            SharedKnowledge.BlockedPositionsBlue.Add(corners[0]);
            SharedKnowledge.BlockedPositionsGreen.Add(corners[0]);
        }

        if (greenChest != null && numColor ==3)
        {
            GameObject chest = Instantiate(greenChest, corners[2], Quaternion.identity);
            chest.transform.localScale =chest.transform.localScale* AgentSize;
            chest.transform.localRotation = Quaternion.Euler(-90, -180 , chest.transform.localRotation.eulerAngles.z);
            occupiedPositions.Add(corners[2]);
            SharedKnowledge.greenChest = corners[2];
            SharedKnowledge.VisitedPositions.Add(corners[2]);
            SharedKnowledge.MissingPos.Remove(corners[2]);
            SharedKnowledge.BlockedPositionsRed.Add(corners[0]);
            SharedKnowledge.BlockedPositionsBlue.Add(corners[0]);
            SharedKnowledge.BlockedPositionsGreen.Add(corners[0]);
        }
    }


    //Function to avoid having more than one object in the same position
    Vector3 GetRandomFreePosition()
    {
        List<Vector3> freePositions = new List<Vector3>();

        // Find all free positions in the grid
        foreach (Vector3 p in grid)
        {
            if (!occupiedPositions.Contains(p))
                freePositions.Add(p);
        }

        // If there are no free positions, return Vector3.zero, this can happen if there are too many objects for the grid size
        if (freePositions.Count == 0)
        {
            Debug.LogWarning("No available Positions on the grid"); 
            return Vector3.zero;
        }

        // Return a random free position
        return freePositions[Random.Range(0, freePositions.Count)];
    }


    //Methos to spawn the jewels in the grid using the prefabs
    void SpawnJewels()
    {
        // Red
        SpawnJewelsByColor(new GameObject[] { (GameObject)redJewelGem, (GameObject)redJewelRing, (GameObject)redJewelRup }, numJewel);
        // Blue
        if (numColor >= 2)
        {
            SpawnJewelsByColor(new GameObject[] { (GameObject)blueJewelGem, (GameObject)blueJewelRing, (GameObject)blueJewelRup }, numJewel);
        }
        // Green
        if (numColor == 3)
        {
            SpawnJewelsByColor(new GameObject[] { (GameObject)greenJewelGem, (GameObject)greenJewelRing, (GameObject)greenJewelRup }, numJewel);
        }
    }

    //Method to spawn jewels of a specific color, it recieves all the prefabs of that color and the number of jewels to spawn
    void SpawnJewelsByColor(GameObject[] prefabs, int count)
    {

        // Spawn the specified number of jewels
        for (int i = 0; i < count; i++)
        {
            //Gets a position on the grid
            Vector3 pos = GetRandomFreePosition();

            if (pos != Vector3.zero) 
            {
                // Instantiate a random jewel prefab at the selected position
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject jewel = Instantiate(prefab, pos, Quaternion.identity);
                jewel.transform.localScale = jewel.transform.localScale * JewelSize;
                jewel.GetComponent<GemType>().setPos(pos);
                jewel.transform.localRotation = Quaternion.Euler(-90, jewel.transform.localRotation.eulerAngles.y, jewel.transform.localRotation.eulerAngles.z);

                // Mark as occupied
                occupiedPositions.Add(pos);
            }
            else
            {
                //In case there are no more positions available
                Debug.LogWarning("No valid position available for spawning jewels.");
            }
        }
    }

}
