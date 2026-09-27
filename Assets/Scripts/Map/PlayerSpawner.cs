using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int spawnIndex = 0;
    [SerializeField] private ArenaCamera arenaCamera; // drag Main Camera here

    void Start()
    {
        Vector2Int spawnGridPos = GridManager.Instance.SpawnPoints[spawnIndex];
        Vector3 worldPos = GridManager.Instance.GridToWorld(spawnGridPos);

        GameObject player = Instantiate(playerPrefab, worldPos, Quaternion.identity);

        if (arenaCamera != null)
            arenaCamera.SetTarget(player.transform);
    }
}