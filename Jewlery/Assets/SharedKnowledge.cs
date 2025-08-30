using UnityEngine;
using System.Collections.Generic;

public class SharedKnowledge : MonoBehaviour
{
    static public int numberOfAgents = 3;
    static public int totalnumberOfGems;
    static public int numberOfGreenGems;
    static public int numberOfRedGems;
    static public int numberOfBlueGems;

    static public HashSet<Vector3> VisitedPositions = new HashSet<Vector3>();
    static public HashSet<Vector3> BlockedPositions = new HashSet<Vector3>(); 
    static public List<Vector3> AgentPositions = new List<Vector3>();
    static public List<Vector3> AgentNextPositions = new List<Vector3>();
    static public List<Vector3> MissingPos = new List<Vector3>();

    static public Vector3 blueChest;
    static public Vector3 redChest;
    static public Vector3 greenChest;

    static public List<Vector3> redGemsFound = new List<Vector3>();
    static public List<Vector3> greenGemsFound = new List<Vector3>();
    static public List<Vector3> blueGemsFound = new List<Vector3>();

    static public Vector3[,] grid;

    void Start()
    {
        Debug.Log("SharedKnowledge initialized");
    }
}
