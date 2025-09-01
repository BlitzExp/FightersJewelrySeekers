using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Main class controlling the agent's behavior
public class AgentController : MonoBehaviour
{

    // Color of the agent
    public enum ColorOption { Blue, Red, Green }


    //Personalization of the agent
    [SerializeField] private ColorOption AgentColor;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private GameObject gemTakingPos;

    //Position where the agent is moving to
    private Vector3 targetPosition;

    // The gem if it is carring one
    private GameObject carriedGem = null;
    private bool deliveringGem = false;

    // Rotation control
    private bool isRotating = false;
    private Quaternion targetRotation;

    // Stuck detection
    [SerializeField] private float stuckThreshold = 3f;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    // Gem reservation system
    private bool goingToGem = false;
    private Vector3? reservedGemPos = null;

    // Collision cooldown
    private float collTimer = 0f;

    // Initialization
    void Start()
    {
        targetPosition = transform.position;
        SharedKnowledge.AgentPositions.Add(transform.position);
        lastPosition = transform.position;
    }

    void Update()
    {
        // Actualizar posición en conocimiento compartido
        SharedKnowledge.AgentPositions.Remove(transform.position);
        SharedKnowledge.AgentPositions.Add(transform.position);

        // Mantain the orientation of the agent in z and x axis to 0
        if (transform.rotation.eulerAngles.z != 0 || transform.rotation.eulerAngles.x != 0)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z = 0; euler.x = 0;
            transform.rotation = Quaternion.Euler(euler);
        }

        // Movement and rotation
        if (isRotating)
            RotateTowardsTarget();
        else
        {
            MoveToTarget();

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                SharedKnowledge.AgentNextPositions.Remove(targetPosition);

                if (!deliveringGem) FindNextTarget();
            }
        }

        // Detection of being stuck
        if (Vector3.Distance(transform.position, lastPosition) < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckThreshold)
            {
                ForceReset();
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        //Collider timer, to avoid multiple collisions with the same object
        if (collTimer > 0) collTimer -= Time.deltaTime;
        if (collTimer < 0f) {
            collTimer = 0f;
        }
    }

    // For the cases where the agent gets stcuk on a place
    void ForceReset()
    {
        stuckTimer = 0f;

        // When carrying a gem, go to the chest
        if (carriedGem != null)
        {
            deliveringGem = true;
            SetNewTarget(GetChestPosition());
            return;
        }

        //In case it was going to a gem, release the reservation
        if (reservedGemPos.HasValue)
        {
            // liberar gema reservada
            ReleaseReservedGem(reservedGemPos.Value);
            reservedGemPos = null;
            goingToGem = false;
        }

        // Choose a new random target
        Vector3 nextPos = FindUnvisitedNeighbor();
        SetNewTarget(nextPos);
    }

    // Movement of the agent to the target position
    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    // Rotation of the agent to face the target position
    void RotateTowardsTarget()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
        }
    }

    // Set a new target position and calculate the required rotation
    void SetNewTarget(Vector3 newTarget)
    {
        // Increse the number of movements
        SharedKnowledge.numberOfMovements++;
        Vector3 direction = (newTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            isRotating = true;
        }
        targetPosition = newTarget;
    }

    // Fucntion to find the next target position to go to
    void FindNextTarget()
    {
        // When carrying a gem, go to the chest
        if (carriedGem != null)
        {
            deliveringGem = true;
            SetNewTarget(GetChestPosition());
            return;
        }

        // If there is a gem of my color, go to it
        Vector3? gemPos = FindGemOfMyColor();
        if (gemPos.HasValue)
        {
            SetNewTarget(gemPos.Value);
            return;
        }

        //In case it is seraching for a gem, then find a new target
        Vector3 nextPos = FindUnvisitedNeighbor();
        SetNewTarget(nextPos);
        SharedKnowledge.VisitedPositions.Add(nextPos);
        SharedKnowledge.MissingPos.Remove(nextPos);
    }


    // Function to search for the nearest position that has not been visited yet
    Vector3 FindUnvisitedNeighbor()
    {
        // Obtain the dimensions of the grid
        int rows = SharedKnowledge.grid.GetLength(0);
        int cols = SharedKnowledge.grid.GetLength(1);

        // Find the closest grid cell to the current position
        Vector3 currentPos = transform.position;
        int curI = -1, curJ = -1;

        float minDist = Mathf.Infinity;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                float d = Vector3.Distance(currentPos, SharedKnowledge.grid[i, j]);
                if (d < minDist)
                {
                    minDist = d;
                    curI = i; curJ = j;
                }
            }
        }

        // Check the four neighboring cells (up, down, left, right)
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(curI + 1, curJ),
            new Vector2Int(curI, curJ + 1),
            new Vector2Int(curI - 1, curJ),
            new Vector2Int(curI, curJ - 1)
        };

        // Select the first valid neighbor that has not been visited or blocked
        foreach (var n in neighbors)
        {
            if (n.x >= 0 && n.x < rows && n.y >= 0 && n.y < cols)
            {
                Vector3 candidate = SharedKnowledge.grid[n.x, n.y];
                if (!SharedKnowledge.VisitedPositions.Contains(candidate) &&
                    !SharedKnowledge.AgentNextPositions.Contains(candidate) &&
                    !SharedKnowledge.AgentPositions.Contains(candidate))
                {
                    if (AgentColor == ColorOption.Red && !SharedKnowledge.BlockedPositionsRed.Contains(candidate)) 
                    {
                        SharedKnowledge.AgentNextPositions.Add(candidate);
                        return candidate;
                    }
                    else if (AgentColor == ColorOption.Blue && !SharedKnowledge.BlockedPositionsBlue.Contains(candidate))
                    {
                        SharedKnowledge.AgentNextPositions.Add(candidate);
                        return candidate;
                    }
                    else if (AgentColor == ColorOption.Green && !SharedKnowledge.BlockedPositionsGreen.Contains(candidate))
                    {
                        SharedKnowledge.AgentNextPositions.Add(candidate);
                        return candidate;
                    }
                }
            }
        }

        // In case there are not unvisited neighbors, pick a random cell
        return GetRandomExplorationCell();
    }

    //Function to check if there has been found a gem of the agent's color

    Vector3? FindGemOfMyColor()
    {
        List<Vector3> gems = null;

        //Checks in the shared knowledge if there is any gem of the agent's color
        switch (AgentColor)
        {
            case ColorOption.Blue: gems = SharedKnowledge.blueGemsFound; break;
            case ColorOption.Red: gems = SharedKnowledge.redGemsFound; break;
            case ColorOption.Green: gems = SharedKnowledge.greenGemsFound; break;
        }

        // If there is any gem of the agent's color, go to the closest one that is not reserved
        if (gems != null && gems.Count > 0 && carriedGem == null)
        {
            float minDist = Mathf.Infinity;
            Vector3? bestGem = null;


            // Find the closest gem that is not reserved
            foreach (var gemPos in gems)
            {
                if (SharedKnowledge.ReservedGems.Contains(gemPos)) continue; 
                float dist = Vector3.Distance(transform.position, gemPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestGem = gemPos;
                }
            }


            // Reserve the gem
            if (bestGem.HasValue)
            {
                goingToGem = true;
                reservedGemPos = bestGem.Value;
                SharedKnowledge.ReservedGems.Add(bestGem.Value); 
                return bestGem.Value;
            }
        }
        return null;
    }


    // Get the position of the chest corresponding to the agent's color
    Vector3 GetChestPosition()
    {
        if (AgentColor == ColorOption.Blue) return SharedKnowledge.blueChest;
        else if (AgentColor == ColorOption.Red) return SharedKnowledge.redChest;
        else if (AgentColor == ColorOption.Green) return SharedKnowledge.greenChest;
        return Vector3.zero;
    }


    // Fucntion in case the detection zone found an object in its area 
    public void OnDetectionTriggerEnter(Collider other)
    {
        // If it is a chest and the agent is carrying a gem, deliver it
        if (carriedGem != null && other.CompareTag("Chest"))
        {
            ChestTag chest = other.GetComponent<ChestTag>();
            // Check if it is the correct color
            if (chest != null && chest.getChestColor().ToString() == AgentColor.ToString())
            {
                // Deliver the gem
                SharedKnowledge.missingGems--;
                Destroy(carriedGem);
                carriedGem = null;
                deliveringGem = false;

                if (reservedGemPos.HasValue) reservedGemPos = null;

                // Find a new target
                Vector3 altTarget = FindUnvisitedNeighbor();
                SetNewTarget(altTarget);
                SharedKnowledge.VisitedPositions.Add(altTarget);
            }
        }

        // If it is a gem
        if (other.CompareTag("Gem"))
        {
            GemType gem = other.GetComponent<GemType>();
            if (gem != null)
            {
                Vector3 gemGridPos = gem.getPos();

                // If it is not of the agent's color, mark as blocked, share the gem position  and return
                if (gem.GemColorValue.ToString() != AgentColor.ToString())
                {
                    if (gem.GemColorValue.ToString() == "Red") 
                    {
                        if (!SharedKnowledge.redGemsFound.Contains(gemGridPos))
                          SharedKnowledge.BlockedPositionsBlue.Add(gemGridPos);
                          SharedKnowledge.BlockedPositionsGreen.Add(gemGridPos);
                          SharedKnowledge.VisitedPositions.Add(gemGridPos);
                           SharedKnowledge.redGemsFound.Add(gemGridPos);
                    }
                    else if (gem.GemColorValue.ToString() == "Blue")
                    {
                        if (!SharedKnowledge.blueGemsFound.Contains(gemGridPos))
                          SharedKnowledge.BlockedPositionsRed.Add(gemGridPos);
                          SharedKnowledge.BlockedPositionsGreen.Add(gemGridPos);
                        SharedKnowledge.VisitedPositions.Add(gemGridPos);
                        SharedKnowledge.blueGemsFound.Add(gemGridPos);
                    }
                    else if (gem.GemColorValue.ToString() == "Green")
                    {
                        if (!SharedKnowledge.greenGemsFound.Contains(gemGridPos))
                          SharedKnowledge.BlockedPositionsRed.Add(gemGridPos);
                          SharedKnowledge.BlockedPositionsBlue.Add(gemGridPos);
                        SharedKnowledge.VisitedPositions.Add(gemGridPos);
                        SharedKnowledge.greenGemsFound.Add(gemGridPos);
                    }
                    return;
                }

                // If it is of the color and has not been collected, pick it up
                if (carriedGem == null && !gem.IsCollected)
                {

                    carriedGem = other.gameObject;
                    gem.CollectGem();

                    // Updates the shared knowledge
                    SharedKnowledge.VisitedPositions.Add(gemGridPos);

                    // Remove from found gems and unmark as blocked, also prevents duplicates and having multiple agents going for the same gem
                    if (SharedKnowledge.redGemsFound.Contains(gemGridPos))
                    {
                        SharedKnowledge.redGemsFound.Remove(gemGridPos);
                        if (SharedKnowledge.BlockedPositionsBlue.Contains(gemGridPos))
                            SharedKnowledge.BlockedPositionsBlue.Remove(gemGridPos);
                        if (SharedKnowledge.BlockedPositionsGreen.Contains(gemGridPos))
                            SharedKnowledge.BlockedPositionsGreen.Remove(gemGridPos);

                    }
                    else if (SharedKnowledge.greenGemsFound.Contains(gemGridPos)) 
                    {
                        SharedKnowledge.greenGemsFound.Remove(gemGridPos);
                        if (SharedKnowledge.BlockedPositionsRed.Contains(gemGridPos))
                            SharedKnowledge.BlockedPositionsRed.Remove(gemGridPos);
                        if (SharedKnowledge.BlockedPositionsBlue.Contains(gemGridPos))
                            SharedKnowledge.BlockedPositionsBlue.Remove(gemGridPos);
                    }
                    else if (SharedKnowledge.blueGemsFound.Contains(gemGridPos))
                    {
                        SharedKnowledge.blueGemsFound.Remove(gemGridPos);
                        if (SharedKnowledge.BlockedPositionsRed.Contains(gemGridPos))
                            SharedKnowledge.BlockedPositionsRed.Remove(gemGridPos);
                        if (SharedKnowledge.BlockedPositionsGreen.Contains(gemGridPos))
                            SharedKnowledge.BlockedPositionsGreen.Remove(gemGridPos);
                    }

                    // Attach the gem to the agent
                    Rigidbody rb = carriedGem.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                    Collider col = carriedGem.GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                    carriedGem.transform.SetParent(gemTakingPos.transform);
                    carriedGem.transform.localPosition = Vector3.zero;

                    // If it was going to a gem, release the reservation
                    if (goingToGem && reservedGemPos != gemGridPos)
                        ReleaseReservedGem(reservedGemPos.Value);
                    goingToGem = false;
                    reservedGemPos = null;
                    deliveringGem = true;

                    // Set the chest as the new target
                    SetNewTarget(GetChestPosition());
                }
            }
        }
    }

    //In case the agent detects an obstacle in front of it
    public void OnDetectionObstacleTriggerEnter(Collider other)
    {
        // If it is anothe agent, avoid blocking each other
        if (other.CompareTag("Romba"))
        {
            // Evitar múltiples reacciones seguidas
            if (collTimer <= 0f)
            {
                collTimer = 0.5f;

                //Determine which agent should rotate based on the name
                if (string.Compare(name, other.name) < 0)
                {
                    StartCoroutine(RotateAndMoveAway());
                }
                else
                {
                    StartCoroutine(StopForSeconds(0.4f));
                }
            }
        }
    }

    // Gorutine to stop the movement of an agent
    private IEnumerator StopForSeconds(float seconds)
    {
        float originalSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(seconds);
        speed = originalSpeed;
    }

    // Gorutine to rotate a agent and move it away from the obstacle
    private IEnumerator RotateAndMoveAway()
    {
        float originalSpeed = speed;
        speed = 0f; 
        transform.Rotate(Vector3.up, 90f);
        yield return new WaitForSeconds(0.3f);
        speed = originalSpeed;
        Vector3 newDir = transform.forward;
        Vector3 candidate = transform.position + newDir;
        SharedKnowledge.AgentNextPositions.Remove(targetPosition);
        SharedKnowledge.VisitedPositions.Add(transform.position);
        SetNewTarget(candidate);
    }

    // Function to detect collisions with the agent
    private void OnTriggerEnter(Collider other)
    {
        // This calls the other collision method, because when this collider is triggered, the other one is not always triggered and it should
        OnDetectionTriggerEnter(other);
        if (other.CompareTag("Romba") || other.CompareTag("Gem")) 
        {
            // To avoid counting when the agent is picking up a gem of its color
            if (carriedGem == null && other.CompareTag("Gem")) 
            {
                GemType gem = other.GetComponent<GemType>();
                if (gem != null && gem.GemColorValue.ToString() == AgentColor.ToString())
                {
                    return;
                }
                
            }

            // To avoid multiple collisions with the same object in a short time being counted
            if (collTimer <= 0f)
            {
                collTimer = 0.5f;
                SharedKnowledge.numberOfCollisions++;
            }
        }
        
    }

    // Functions to manage the reservation of gems
    private void ReleaseReservedGem(Vector3 pos)
    {
        SharedKnowledge.ReservedGems.Remove(pos);
    }


    // Function in case there are no unvisited neighbors, to pick a random cell in the grid
    private Vector3 GetRandomExplorationCell()
    {
        int rows = SharedKnowledge.grid.GetLength(0);
        int cols = SharedKnowledge.grid.GetLength(1);
        Vector3 candidate;

        do
        {
            int i = Random.Range(0, rows);
            int j = Random.Range(0, cols);
            candidate = SharedKnowledge.grid[i, j];
        }
        while (candidate == SharedKnowledge.blueChest ||
               candidate == SharedKnowledge.redChest ||
               candidate == SharedKnowledge.greenChest);

        return candidate;
    }
}
