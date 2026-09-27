using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float playerRadius = 0.45f;

    public float PlayerRadius => playerRadius;

    [Header("Corner Assist")]
    [SerializeField] private bool cornerAssist = true;
    [SerializeField] private float cornerAssistStrength = 3f;

    private BombPlacer bombPlacer;

    public float CurrentSpeed { get; private set; }

    public Vector2Int CurrentGridPos => GridManager.Instance.WorldToGrid(transform.position);

    void Awake()
    {
        bombPlacer = GetComponent<BombPlacer>();
    }

    void Update()
    {
        Vector2 input = ReadInput();
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);
        float distance = moveSpeed * Time.deltaTime;

        bool movedX = false;
        bool movedZ = false;

        if (input != Vector2.zero)
        {
            movedX = TryMoveAxis(new Vector3(moveDir.x, 0f, 0f), distance);
            movedZ = TryMoveAxis(new Vector3(0f, 0f, moveDir.z), distance);

            if (cornerAssist && !movedX && !movedZ)
                ApplyCornerAssist(moveDir, distance);

            FaceDirection(moveDir);
        }

        CurrentSpeed = (movedX || movedZ) ? moveSpeed : 0f;
    }

    bool TryMoveAxis(Vector3 axisDir, float distance)
{
    if (axisDir == Vector3.zero) return false;

    Vector3 candidate = transform.position + axisDir * distance;

    if (IsFree(candidate))
    {
        transform.position = candidate;
        return true;
    }
    return false;
}

    Vector2 ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) > Mathf.Abs(v))
            return new Vector2(Mathf.Sign(h), 0f);
        if (Mathf.Abs(v) > 0f)
            return new Vector2(0f, Mathf.Sign(v)); // world space: +Z is up-screen, no flip needed

        return Vector2.zero;
    }

    bool IsFree(Vector3 worldPos)
    {
        Vector2Int? exempt = bombPlacer != null ? bombPlacer.GetExemptTile() : null;
        return GridManager.Instance.IsWorldPositionFree(worldPos, playerRadius, exempt);
    }

    // Nudges the player toward the centre of the corridor they're trying to enter,
    // so a near-miss alignment doesn't snag on a corner.
    void ApplyCornerAssist(Vector3 moveDir, float distance)
    {
        float tile = GridManager.Instance.TileSize;
        Vector3 pos = transform.position;

        if (Mathf.Abs(moveDir.x) > 0f)
        {
            float targetZ = Mathf.Round(pos.z / tile) * tile;

            // Would moving in the pressed direction work if we were perfectly aligned?
            Vector3 alignedAhead = new Vector3(pos.x + moveDir.x * distance, pos.y, targetZ);
            if (!IsFree(alignedAhead)) return; // genuine wall — don't nudge

            float corrected = Mathf.MoveTowards(pos.z, targetZ, cornerAssistStrength * Time.deltaTime);
            Vector3 candidate = new Vector3(pos.x, pos.y, corrected);
            if (IsFree(candidate)) transform.position = candidate;
        }
        else if (Mathf.Abs(moveDir.z) > 0f)
        {
            float targetX = Mathf.Round(pos.x / tile) * tile;

            Vector3 alignedAhead = new Vector3(targetX, pos.y, pos.z + moveDir.z * distance);
            if (!IsFree(alignedAhead)) return;

            float corrected = Mathf.MoveTowards(pos.x, targetX, cornerAssistStrength * Time.deltaTime);
            Vector3 candidate = new Vector3(corrected, pos.y, pos.z);
            if (IsFree(candidate)) transform.position = candidate;
        }
    }

    void FaceDirection(Vector3 worldDir)
    {
        if (worldDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(worldDir);
    }


}