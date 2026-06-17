using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{

    private float length, startPos;
    public GameObject cam;
    public float parallaxEffect;
    public float wrapPercent = 0.3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame

    void Update()
    {
        float temp = cam.transform.position.x * (1 - parallaxEffect);
        float distance = cam.transform.position.x * parallaxEffect;

        transform.position = new Vector3(
            startPos + distance,
            transform.position.y,
            transform.position.z);

        float threshold = length * wrapPercent;

        if (temp > startPos + threshold)
            startPos += length;
        else if (temp < startPos - threshold)
            startPos -= length;
    }
}