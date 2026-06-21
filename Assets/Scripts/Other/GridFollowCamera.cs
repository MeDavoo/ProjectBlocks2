using UnityEngine;

public class GridFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float cellSize = 1f;

    void LateUpdate()
    {
        Vector3 pos = target.position;

        pos.x = Mathf.Round(pos.x / cellSize) * cellSize;
        pos.y = Mathf.Round(pos.y / cellSize) * cellSize;

        transform.position = new Vector3(
            pos.x,
            pos.y,
            transform.position.z);
    }
}