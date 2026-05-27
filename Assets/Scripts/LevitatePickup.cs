using UnityEngine;

public class LevitatePickup : MonoBehaviour
{
    public float floatHeight = 0.3f;
    public float floatSpeed = 2f;

    public float rotateSpeed = 90f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}