using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AgentController : MonoBehaviour
{
    public enum ColorOption { Blue, Red, Green }

    [SerializeField] private ColorOption AgentColor;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private GameObject gemTakingPos;

    private Vector3 targetPosition;
    private GameObject carriedGem = null;
    private bool deliveringGem = false;

    private bool isRotating = false;
    private Quaternion targetRotation;

    [SerializeField] private float stuckThreshold = 3f;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    private bool goingToGem = false;
    private Vector3? reservedGemPos = null;

    private float collTimer = 0f;

    void Start()
    {
        targetPosition = transform.position;
        SharedKnowledge.AgentPositions.Add(transform.position);
        lastPosition = transform.position;
    }

    void Update()
    {
        SharedKnowledge.AgentPositions.Remove(transform.position);
        SharedKnowledge.AgentPositions.Add(transform.position);

        if (transform.rotation.eulerAngles.z != 0 || transform.rotation.eulerAngles.x != 0)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z = 0; euler.x = 0;
            transform.rotation = Quaternion.Euler(euler);
        }

        if (isRotating)
            RotateTowardsTarget();
        else
        {
            MoveToTarget();

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                SharedKnowledge.AgentNextPositions.Remove(targetPosition);

                if (deliveringGem) DeliverGem();
                else FindNextTarget();
            }
        }

        if (Vector3.Distance(transform.position, lastPosition) < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckThreshold)
            {
                Debug.Log($"{name} estaba atascado, forzando nuevo destino...");
                ForceReset();
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        if (collTimer > 0) collTimer -= Time.deltaTime;
        if (collTimer < 0f) {
            collTimer = 0f;
        }
    }

    void ForceReset()
    {
        stuckTimer = 0f;

        if (carriedGem != null)
        {
            deliveringGem = true;
            SetNewTarget(GetChestPosition());
            return;
        }

        if (reservedGemPos.HasValue)
        {
            // liberar gema reservada
            ReleaseReservedGem(reservedGemPos.Value);
            reservedGemPos = null;
            goingToGem = false;
        }

        Vector3 nextPos = FindUnvisitedNeighbor();
        SetNewTarget(nextPos);
    }

    // --- Movimiento ---
    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    void RotateTowardsTarget()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
        }
    }

    void SetNewTarget(Vector3 newTarget)
    {
        SharedKnowledge.numberOfMovements++;
        Vector3 direction = (newTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            isRotating = true;
        }
        targetPosition = newTarget;
    }

    // --- Lógica de búsqueda ---
    void FindNextTarget()
    {
        if (carriedGem != null)
        {
            deliveringGem = true;
            SetNewTarget(GetChestPosition());
            return;
        }

        Vector3? gemPos = FindGemOfMyColor();
        if (gemPos.HasValue)
        {
            SetNewTarget(gemPos.Value);
            return;
        }

        Vector3 nextPos = FindUnvisitedNeighbor();
        SetNewTarget(nextPos);
        SharedKnowledge.VisitedPositions.Add(nextPos);
        SharedKnowledge.MissingPos.Remove(nextPos);
    }

    Vector3 FindUnvisitedNeighbor()
    {
        int rows = SharedKnowledge.grid.GetLength(0);
        int cols = SharedKnowledge.grid.GetLength(1);

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

        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(curI + 1, curJ),
            new Vector2Int(curI, curJ + 1),
            new Vector2Int(curI - 1, curJ),
            new Vector2Int(curI, curJ - 1)
        };

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

        // fallback: si no hay vecinos válidos → elegir una celda aleatoria que no sea un cofre
        return GetRandomExplorationCell();
    }

    Vector3? FindGemOfMyColor()
    {
        List<Vector3> gems = null;
        switch (AgentColor)
        {
            case ColorOption.Blue: gems = SharedKnowledge.blueGemsFound; break;
            case ColorOption.Red: gems = SharedKnowledge.redGemsFound; break;
            case ColorOption.Green: gems = SharedKnowledge.greenGemsFound; break;
        }

        if (gems != null && gems.Count > 0 && carriedGem == null)
        {
            float minDist = Mathf.Infinity;
            Vector3? bestGem = null;

            foreach (var gemPos in gems)
            {
                if (SharedKnowledge.ReservedGems.Contains(gemPos)) continue; // 🚫 ya reservada
                float dist = Vector3.Distance(transform.position, gemPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestGem = gemPos;
                }
            }

            if (bestGem.HasValue)
            {
                goingToGem = true;
                reservedGemPos = bestGem.Value;
                SharedKnowledge.ReservedGems.Add(bestGem.Value); // ✅ reservar
                Debug.Log($"{name} reservó gema {AgentColor} en {bestGem.Value}");
                return bestGem.Value;
            }
        }
        return null;
    }

    Vector3 GetChestPosition()
    {
        if (AgentColor == ColorOption.Blue) return SharedKnowledge.blueChest;
        else if (AgentColor == ColorOption.Red) return SharedKnowledge.redChest;
        else return SharedKnowledge.greenChest;
    }

    void DeliverGem() { }

    public void OnDetectionTriggerEnter(Collider other)
    {
        if (carriedGem != null && other.CompareTag("Chest"))
        {
            ChestTag chest = other.GetComponent<ChestTag>();
            if (chest != null && chest.getChestColor().ToString() == AgentColor.ToString())
            {
                SharedKnowledge.missingGems--;
                Destroy(carriedGem);
                carriedGem = null;
                deliveringGem = false;

                if (reservedGemPos.HasValue) reservedGemPos = null;

                Vector3 altTarget = FindUnvisitedNeighbor();
                SetNewTarget(altTarget);
                SharedKnowledge.VisitedPositions.Add(altTarget);
            }
        }

        if (other.CompareTag("Gem"))
        {
            GemType gem = other.GetComponent<GemType>();
            if (gem != null)
            {
                Vector3 gemGridPos = gem.getPos();

                // no es de mi color → esquivar
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

                // es de mi color → recoger
                if (carriedGem == null && !gem.IsCollected)
                {
                    carriedGem = other.gameObject;
                    gem.CollectGem();
                    SharedKnowledge.VisitedPositions.Add(gemGridPos);

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
                    // liberar reserva
                    //SharedKnowledge.ReservedGems.Remove(gemGridPos);

                    Rigidbody rb = carriedGem.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                    Collider col = carriedGem.GetComponent<Collider>();
                    if (col != null) col.enabled = false;

                    carriedGem.transform.SetParent(gemTakingPos.transform);
                    carriedGem.transform.localPosition = Vector3.zero;

                    if (goingToGem && reservedGemPos != gemGridPos)
                        ReleaseReservedGem(reservedGemPos.Value);

                    goingToGem = false;
                    reservedGemPos = null;

                    deliveringGem = true;
                    SetNewTarget(GetChestPosition());
                }
            }
        }
    }

    public void OnDetectionObstacleTriggerEnter(Collider other)
    {
        if (other.CompareTag("Romba"))
        {
            Debug.Log($"{name} encontró otro agente {other.name}");

            // Evitar múltiples reacciones seguidas
            if (collTimer <= 0f)
            {
                collTimer = 0.5f;

                // Decidir quién rota y quién se queda
                if (string.Compare(name, other.name) < 0) // El que tenga "menor nombre" rota
                {
                    Debug.Log($"{name} rota 90° para evitar bloqueo con {other.name}");
                    StartCoroutine(RotateAndMoveAway());
                }
                else
                {
                    Debug.Log($"{name} se queda detenido 0.4s porque {other.name} rotará");
                    StartCoroutine(StopForSeconds(0.4f));
                }
            }
        }
    }

    // Corutina: detenerse
    private IEnumerator StopForSeconds(float seconds)
    {
        float originalSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(seconds);
        speed = originalSpeed;
    }

    // Corutina: rotar 90° y moverse un paso en esa dirección
    private IEnumerator RotateAndMoveAway()
    {
        float originalSpeed = speed;
        speed = 0f;  // detener mientras rota

        // Rotar 90° en Y (a la derecha)
        transform.Rotate(Vector3.up, 90f);

        yield return new WaitForSeconds(0.3f);

        speed = originalSpeed;

        // calcular nueva posición un paso hacia adelante
        Vector3 newDir = transform.forward;
        Vector3 candidate = transform.position + newDir;

        // liberar la posición bloqueada
        SharedKnowledge.AgentNextPositions.Remove(targetPosition);
        SharedKnowledge.VisitedPositions.Add(transform.position);

        // marcar como nuevo destino
        SetNewTarget(candidate);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Romba") || other.CompareTag("Gem")) 
        {
            Debug.Log($"{name} colisionó con {other.name}");
            Debug.Log($"Estab llevando gema {deliveringGem}");

            if (carriedGem == null && other.CompareTag("Gem")) 
            {
                GemType gem = other.GetComponent<GemType>();
                if (gem != null && gem.GemColorValue.ToString() == AgentColor.ToString())
                {
                    return;
                }
                
            }

            if (collTimer <= 0f)
            {
                collTimer = 0.5f; // evitar múltiples colisiones rápidas
                SharedKnowledge.numberOfCollisions++;
            }
        }
        
    }

    // --- Utilidad ---
    private void ReleaseReservedGem(Vector3 pos)
    {
        SharedKnowledge.ReservedGems.Remove(pos);
    }

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
