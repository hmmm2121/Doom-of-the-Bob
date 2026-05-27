using UnityEngine;

public class CherringScript : MonoBehaviour
{
    public float floatHeight = 1f;
    public float floatSpeed = 15f;
    public float rotateSpeed = 0;
    private Vector3 startPosition;
    private float timeOffset;

    void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}