using UnityEngine;

public class BlockDropItem : MonoBehaviour
{
    [SerializeField] private int scoreValue = 50;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerHealth>() != null)
        {
            HUDManager.Instance?.AddScore(scoreValue);
            Destroy(gameObject);
        }
    }
}