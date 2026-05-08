using UnityEngine;

public class RoadManager : MonoBehaviour
{
    public static RoadManager instance;

    public GameObject roadPrefab;  
    public float roadLength = 10f;  
    public float spawnSpeed = 2f;   

    private Vector3 nextSpawnPoint;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

    }

   public void SpawnRoad(float positionPassed)
    {
        nextSpawnPoint.z = positionPassed + 3 * roadLength;

        GameObject newRoad = Instantiate(roadPrefab, nextSpawnPoint, Quaternion.identity);
    }
}