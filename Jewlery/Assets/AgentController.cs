using UnityEngine;
using System.Collections.Generic;

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

    [SerializeField] private float stuckThreshold = 3f; // segundos sin moverse
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        targetPosition = transform.position;
        SharedKnowledge.AgentPositions.Add(transform.position);
        lastPosition = transform.position;
    }

    void Update()
    {
        // actualizar posición en memoria compartida
        SharedKnowledge.AgentPositions.Remove(transform.position);
        SharedKnowledge.AgentPositions.Add(transform.position);

        if (transform.rotation.eulerAngles.z != 0) 
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
        if (transform.rotation.eulerAngles.x != 0) 
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = 0;
            transform.rotation = Quaternion.Euler(euler);
        }

        if (isRotating)
        {
            RotateTowardsTarget();
        }
        else
        {
            MoveToTarget();

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                // liberar reserva
                SharedKnowledge.AgentNextPositions.Remove(targetPosition);

                if (deliveringGem)
                    DeliverGem();
                else
                    FindNextTarget();
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

    }

    void ForceReset()
    {
        stuckTimer = 0f;

        // si lleva una gema → intentar ir al cofre de nuevo
        if (carriedGem != null)
        {
            deliveringGem = true;
            Vector3 chestPos = GetChestPosition();
            SetNewTarget(chestPos);
            return;
        }

        // si no → buscar un vecino alternativo
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
            Vector3 chestPos = GetChestPosition();
            SetNewTarget(chestPos);
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
                    curI = i;
                    curJ = j;
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

        // vecinos libres no visitados
        foreach (var n in neighbors)
        {
            if (n.x >= 0 && n.x < rows && n.y >= 0 && n.y < cols)
            {
                Vector3 candidate = SharedKnowledge.grid[n.x, n.y];
                if (!SharedKnowledge.VisitedPositions.Contains(candidate) &&
                    !SharedKnowledge.AgentNextPositions.Contains(candidate) &&
                    !SharedKnowledge.BlockedPositions.Contains(candidate) &&
                    !SharedKnowledge.AgentPositions.Contains(candidate))
                {
                    SharedKnowledge.AgentNextPositions.Add(candidate);
                    return candidate;
                }
            }
        }

        // fallback: vecinos ya visitados
        foreach (var n in neighbors)
        {
            if (n.x >= 0 && n.x < rows && n.y >= 0 && n.y < cols)
            {
                Vector3 candidate = SharedKnowledge.grid[n.x, n.y];
                if (!SharedKnowledge.AgentNextPositions.Contains(candidate) &&
                    !SharedKnowledge.BlockedPositions.Contains(candidate))
                {
                    SharedKnowledge.AgentNextPositions.Add(candidate);
                    return candidate;
                }
            }
        }

        return SharedKnowledge.grid[curI, curJ];
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
            Vector3 gemPos = gems[0];
            gems.RemoveAt(0);
            return gemPos;
        }
        return null;
    }

    // --- Cofre dinámico ---
    Vector3 GetChestPosition()
    {
        if (AgentColor == ColorOption.Blue)
            return SharedKnowledge.blueChest;
        else if (AgentColor == ColorOption.Red)
            return SharedKnowledge.redChest;
        else
            return SharedKnowledge.greenChest; 
    }

    // --- Entregar gema ---
    void DeliverGem()
    {
        // ya no destruye aquí, se maneja en OnTriggerEnter con Chest
    }

    // --- Detección ---
    public void OnDetectionTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gem"))
        {
            GemType gem = other.GetComponent<GemType>();
            if (gem != null)
            {
                // registrar en memoria compartida
                switch (gem.GemColorValue)
                {
                    case GemType.ColorOption.Blue:
                        if (!SharedKnowledge.blueGemsFound.Contains(other.transform.position))
                            SharedKnowledge.blueGemsFound.Add(other.transform.position);
                        break;
                    case GemType.ColorOption.Red:
                        if (!SharedKnowledge.redGemsFound.Contains(other.transform.position))
                            SharedKnowledge.redGemsFound.Add(other.transform.position);
                        break;
                    case GemType.ColorOption.Green:
                        if (!SharedKnowledge.greenGemsFound.Contains(other.transform.position))
                            SharedKnowledge.greenGemsFound.Add(other.transform.position);
                        break;
                }

                // 🚫 no es de mi color → esquivar
                if (gem.GemColorValue.ToString() != AgentColor.ToString())
                {
                    SharedKnowledge.BlockedPositions.Add(other.transform.position);
                    Vector3 altTarget = FindUnvisitedNeighbor();
                    SetNewTarget(altTarget);
                    return;
                }

                // ✅ es de mi color → recoger
                if (carriedGem == null && !gem.IsCollected)
                {
                    carriedGem = other.gameObject;
                    gem.CollectGem();

                    Rigidbody rb = carriedGem.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                    Collider col = carriedGem.GetComponent<Collider>();
                    if (col != null) col.enabled = false;

                    carriedGem.transform.SetParent(gemTakingPos.transform);
                    carriedGem.transform.localPosition = Vector3.zero;
                }
            }
        }

        if (carriedGem != null && other.CompareTag("Chest"))
        {
            ChestTag chest = other.GetComponent<ChestTag>();
            if (chest != null && chest.getChestColor().ToString() == AgentColor.ToString())
            {
                // ✅ entregar gema
                Destroy(carriedGem);
                carriedGem = null;
                deliveringGem = false;
                Vector3 altTarget = FindUnvisitedNeighbor();
                SetNewTarget(altTarget);
                SharedKnowledge.VisitedPositions.Add(altTarget);
            }
        }
    }

    // --- Colisión con cofres ---
    private void OnTriggerEnter(Collider other)
    {
        SharedKnowledge.numberOfCollisions = SharedKnowledge.numberOfCollisions + 1;
    }
}
