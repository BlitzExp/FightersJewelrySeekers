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

    void Start()
    {
        targetPosition = transform.position;
        SharedKnowledge.AgentPositions.Add(transform.position);
    }

    void Update()
    {
        // actualizar posición del agente en memoria compartida
        SharedKnowledge.AgentPositions.Remove(transform.position);
        SharedKnowledge.AgentPositions.Add(transform.position);

        if (isRotating)
        {
            RotateTowardsTarget();
        }
        else
        {
            MoveToTarget();

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                if (deliveringGem)
                    DeliverGem();
                else
                    FindNextTarget();
            }
        }
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
            isRotating = true;   // detener movimiento hasta terminar de rotar
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

            // 🚫 si el cofre está ocupado → buscar alternativa
            if (SharedKnowledge.BlockedPositions.Contains(chestPos) ||
                SharedKnowledge.AgentPositions.Contains(chestPos))
            {
                Vector3 altTarget = FindUnvisitedNeighbor();
                SetNewTarget(altTarget);
            }
            else
            {
                SetNewTarget(chestPos);
            }
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

        foreach (var n in neighbors)
        {
            if (n.x >= 0 && n.x < rows && n.y >= 0 && n.y < cols)
            {
                Vector3 candidate = SharedKnowledge.grid[n.x, n.y];
                if (!SharedKnowledge.VisitedPositions.Contains(candidate) &&
                    !SharedKnowledge.AgentNextPositions.Contains(candidate) &&
                    !SharedKnowledge.BlockedPositions.Contains(candidate) &&
                    !SharedKnowledge.AgentPositions.Contains(candidate)) // 🚫 evitar agentes
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

        if (gems != null && gems.Count > 0)
        {
            Vector3 gemPos = gems[0];
            gems.RemoveAt(0);
            return gemPos;
        }
        return null;
    }

    Vector3 GetChestPosition()
    {
        return AgentColor switch
        {
            ColorOption.Blue => SharedKnowledge.blueChest,
            ColorOption.Red => SharedKnowledge.redChest,
            ColorOption.Green => SharedKnowledge.greenChest,
            _ => transform.position
        };
    }

    // --- Entregar gema ---
    void DeliverGem()
    {
        if (Vector3.Distance(transform.position, GetChestPosition()) < 0.2f && carriedGem != null)
        {
            // liberar celda de obstáculo
            SharedKnowledge.BlockedPositions.Remove(carriedGem.transform.position);

            // quitar de memoria compartida
            switch (AgentColor)
            {
                case ColorOption.Blue: SharedKnowledge.blueGemsFound.Remove(carriedGem.transform.position); break;
                case ColorOption.Red: SharedKnowledge.redGemsFound.Remove(carriedGem.transform.position); break;
                case ColorOption.Green: SharedKnowledge.greenGemsFound.Remove(carriedGem.transform.position); break;
            }

            Destroy(carriedGem);
            carriedGem = null;
            deliveringGem = false;
        }
    }

    // --- Detección ---
    public void OnDetectionTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gem"))
        {
            GemType gem = other.GetComponent<GemType>();

            if (gem != null)
            {
                // Registrar en memoria compartida
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

                // 🚫 Si la gema no es de mi color → bloquear y esquivar
                if (gem.GemColorValue.ToString() != AgentColor.ToString())
                {
                    SharedKnowledge.BlockedPositions.Add(other.transform.position);
                    Vector3 altTarget = FindUnvisitedNeighbor();
                    SetNewTarget(altTarget);
                    return;
                }

                // ✅ Si es de mi color → recoger
                if (carriedGem == null && !gem.IsCollected)
                {
                    carriedGem = other.gameObject;
                    gem.CollectGem();

                    // quitar física
                    Rigidbody rb = carriedGem.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;

                    // quitar collider
                    Collider col = carriedGem.GetComponent<Collider>();
                    if (col != null) col.enabled = false;

                    // poner como hijo del agente
                    carriedGem.transform.SetParent(gemTakingPos.transform);
                    carriedGem.transform.localPosition = Vector3.zero;
                }
            }
        }
    }
}
