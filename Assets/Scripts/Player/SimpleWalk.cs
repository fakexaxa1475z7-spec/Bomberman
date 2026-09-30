using UnityEngine;

public class SimpleWalk : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float lifetime = 20f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}