using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class GameConfiguration : MonoBehaviour
{
    [Header("Size of the agents")]
    [SerializeField] float AgentSize;

    [Header("X dimension of the stage >(5*AgentSize)")]
    [SerializeField] float UStage;
    [Header("Y dimension of the stage >(5*AgentSize)")]
    [SerializeField] float VStage;

    [Header("Jewel Size Relative to the Agent")]
    [SerializeField] float JewelSize;
    [Header("Number of Jewels")]
    [SerializeField] int numJewel;


    private int numColor = 3;
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
    private const float GRID_PERCENT = 0.9f;
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    private void OnValidate()
    {
        if (AgentSize <= 0) { AgentSize = 1; }
        if (UStage < (5 * AgentSize)) { UStage = 5 * AgentSize; }
        if (VStage < (5 * AgentSize)) { VStage = 5 * AgentSize; }
        if (JewelSize < 0.5 * AgentSize) { JewelSize = 0.5f * AgentSize; }else if (JewelSize > AgentSize * 2) { JewelSize = AgentSize*2; }


        if (numJewel < 1) { numJewel = 1; }
        if (numAgent <3 ) { numAgent = 3; }
    }
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


        GenerateGrid();
        spawnChests();
        SpawnAgents();
        SpawnJewels();
    }

    void GenerateGrid()
    {
        // 🔹 Usar solo el 90% del escenario
        float gridUStage = UStage * GRID_PERCENT;
        float gridVStage = VStage * GRID_PERCENT;

        // Número de celdas en X y Z
        gridWidth = Mathf.FloorToInt(gridUStage / AgentSize);
        gridHeight = Mathf.FloorToInt(gridVStage / AgentSize);

        grid = new Vector3[gridWidth, gridHeight];

        // Centro del escenario
        Vector3 stageCenter = stage != null ? stage.transform.position : Vector3.zero;

        // 🔹 Calcular cuánto espacio real ocupará la grilla
        float totalGridWidth = gridWidth * AgentSize;
        float totalGridHeight = gridHeight * AgentSize;

        // 🔹 Ajustar inicio para que quede centrado
        float startX = stageCenter.x - (totalGridWidth / 2) + (AgentSize / 2);
        float startZ = stageCenter.z - (totalGridHeight / 2) + (AgentSize / 2);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                grid[x, z] = new Vector3(startX + x * AgentSize, stageCenter.y, startZ + z * AgentSize);
            }
        }

        //Debug.Log($"Grid creado (centrado 90% stage): {gridWidth} x {gridHeight} posiciones.");
    }



    void SpawnAgents()
    {
        int next = 1;

        for (int i = 0; i < numAgent; i++)
        {
            Vector3 pos = GetRandomFreePosition();

            if (next == 1 && rebAgent != null)
            {
                Instantiate(rebAgent, pos, Quaternion.identity).transform.localScale = Vector3.one * AgentSize;
                next = 2;
            }
            else if (next == 2 && blueAgent != null)
            {
                Instantiate(blueAgent, pos, Quaternion.identity).transform.localScale = Vector3.one * AgentSize;
                next = 3;
            }
            else if (next == 3 && greenAgent != null)
            {
                Instantiate(greenAgent, pos, Quaternion.identity).transform.localScale = Vector3.one * AgentSize;
                next = 1;
            }

            // Marcar posición como ocupada
            occupiedPositions.Add(pos);
        }
    }

    void spawnChests()
    {
        if (stage == null) return;

        // Centro del escenario
        Vector3 stageCenter = stage.transform.position;

        // Calcular los límites del grid (ya reducido al 90%)
        float gridUStage = UStage * GRID_PERCENT;
        float gridVStage = VStage * GRID_PERCENT;

        float halfWidth = gridUStage / 2;
        float halfHeight = gridVStage / 2;

        // Esquinas (X,Z)
        Vector3[] corners = new Vector3[]
        {
        new Vector3(stageCenter.x - halfWidth, stageCenter.y, stageCenter.z - halfHeight), // abajo izquierda
        new Vector3(stageCenter.x + halfWidth, stageCenter.y, stageCenter.z - halfHeight), // abajo derecha
        new Vector3(stageCenter.x - halfWidth, stageCenter.y, stageCenter.z + halfHeight), // arriba izquierda
        new Vector3(stageCenter.x + halfWidth, stageCenter.y, stageCenter.z + halfHeight)  // arriba derecha
        };

        // Instanciar cofres en las esquinas
        if (redChest != null)
        {
            GameObject chest = Instantiate(redChest, corners[0], Quaternion.identity);
            chest.transform.localScale = chest.transform.localScale * AgentSize;
            chest.transform.localRotation = Quaternion.Euler(-90, chest.transform.localRotation.eulerAngles.y, chest.transform.localRotation.eulerAngles.z);
            occupiedPositions.Add(corners[0]);
        }

        if (blueChest != null)
        {
            GameObject chest = Instantiate(blueChest, corners[1], Quaternion.identity);
            chest.transform.localScale = chest.transform.localScale * AgentSize;
            chest.transform.localRotation = Quaternion.Euler(-90, chest.transform.localRotation.eulerAngles.y, chest.transform.localRotation.eulerAngles.z);
            occupiedPositions.Add(corners[1]);
        }

        if (greenChest != null)
        {
            GameObject chest = Instantiate(greenChest, corners[2], Quaternion.identity);
            chest.transform.localScale =chest.transform.localScale* AgentSize;
            chest.transform.localRotation = Quaternion.Euler(-90, -180 , chest.transform.localRotation.eulerAngles.z);
            occupiedPositions.Add(corners[2]);
        }
    }


    Vector3 GetRandomFreePosition()
    {
        List<Vector3> freePositions = new List<Vector3>();

        foreach (Vector3 p in grid)
        {
            if (!occupiedPositions.Contains(p))
                freePositions.Add(p);
        }

        if (freePositions.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay posiciones libres en el grid.");
            return Vector3.zero;
        }

        return freePositions[Random.Range(0, freePositions.Count)];
    }


    // 🔹 Método para obtener una posición de la grilla
    public Vector3 GetGridPosition(int x, int z)
    {
        if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            return grid[x, z];
        else
            return Vector3.zero;
    }

    void SpawnJewels()
    {
        // Red
        SpawnJewelsByColor(new GameObject[] { (GameObject)redJewelGem, (GameObject)redJewelRing, (GameObject)redJewelRup }, numJewel);
        // Blue
        SpawnJewelsByColor(new GameObject[] { (GameObject)blueJewelGem, (GameObject)blueJewelRing, (GameObject)blueJewelRup }, numJewel);
        // Green
        SpawnJewelsByColor(new GameObject[] { (GameObject)greenJewelGem, (GameObject)greenJewelRing, (GameObject)greenJewelRup }, numJewel);
    }

    void SpawnJewelsByColor(GameObject[] prefabs, int count)
    {
        for (int i = 0; i < count; i++)
        {
            //Debug.Log("Spawning jewel...");
            Vector3 pos = GetRandomFreePosition(); // reuse grid logic

            if (pos != Vector3.zero) // Check if a valid position is returned
            {
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject jewel = Instantiate(prefab, pos, Quaternion.identity);
                jewel.transform.localScale = jewel.transform.localScale * JewelSize;
                jewel.transform.localRotation = Quaternion.Euler(-90, jewel.transform.localRotation.eulerAngles.y, jewel.transform.localRotation.eulerAngles.z);

                // Mark as occupied
                occupiedPositions.Add(pos);
            }
            else
            {
                Debug.LogWarning("⚠️ No valid position available for spawning jewels.");
            }
        }
    }

}
