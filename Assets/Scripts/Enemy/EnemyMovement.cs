using UnityEngine;
using System.Collections.Generic;

public enum EnemyType { Normal, Fast }

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;
    [SerializeField] private float normalSpeed = 2f;
    [SerializeField] private float fastSpeed = 4f;

    private float moveSpeed;
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector2Int currentDirection;
    private bool isMoving = false;

    private static readonly Vector2Int[] AllDirections =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public Vector2Int CurrentGridPos => currentGridPos;

    void Start()
    {
        moveSpeed = enemyType == EnemyType.Fast ? fastSpeed : normalSpeed;

        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        targetGridPos = currentGridPos;
        transform.position = GridManager.Instance.GridToWorld(currentGridPos);

        currentDirection = PickRandomDirection(Vector2Int.zero);
    }

    void Update()
    {
        if (!isMoving)
            TryStartMove();
        else
            MoveTowardsTarget();
    }

    void TryStartMove()
    {
        // A zero direction means we're boxed in — never treat it as valid movement
        if (currentDirection != Vector2Int.zero &&
            GridManager.Instance.IsWalkable(currentGridPos + currentDirection))
        {
            targetGridPos = currentGridPos + currentDirection;
            isMoving = true;
            FaceDirection(currentDirection);
            return;
        }

        Vector2Int newDir = PickRandomDirection(currentDirection);
        if (newDir != Vector2Int.zero)
        {
            currentDirection = newDir;
            targetGridPos = currentGridPos + currentDirection;
            isMoving = true;
            FaceDirection(currentDirection);
        }
        else
        {
            // Still boxed in — keep direction as zero so we retry every frame,
            // which lets the enemy escape automatically once a nearby block gets bombed
            currentDirection = Vector2Int.zero;
        }
    }

    Vector2Int PickRandomDirection(Vector2Int excludeReverse)
    {
        List<Vector2Int> options = new List<Vector2Int>();
        Vector2Int reverseOf = excludeReverse * -1;

        foreach (var dir in AllDirections)
        {
            if (dir == reverseOf) continue; // avoid immediately backtracking unless forced
            if (GridManager.Instance.IsWalkable(currentGridPos + dir))
                options.Add(dir);
        }

        if (options.Count == 0)
        {
            if (GridManager.Instance.IsWalkable(currentGridPos + reverseOf))
                return reverseOf; // dead end — allow reversing as last resort
            return Vector2Int.zero;
        }

        return options[Random.Range(0, options.Count)];
    }

    void MoveTowardsTarget()
    {
        Vector3 targetWorldPos = GridManager.Instance.GridToWorld(targetGridPos);
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            transform.position = targetWorldPos;
            currentGridPos = targetGridPos;
            isMoving = false;
        }
    }

    void FaceDirection(Vector2Int dir)
    {
        Vector3 worldDir = GridManager.Instance.GridDirectionToWorld(dir);
        if (worldDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(worldDir);
    }
}