using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObstaclePatch : MonoBehaviour
{

    [Header("Obstacle Settings")]
    [SerializeField]
    GameObject obstaclePrefab;
    [SerializeField, Range(1, 200)]
    int obstacleCount = 10;
    //obsacle min and max size
    [SerializeField, Range(0.1f, 20.0f)]
    float obstacleMinSize = 0.1f;
    [SerializeField, Range(0.1f, 20.0f)]
    float obstacleMaxSize = 0.5f;
    //obstacle min and max height
    [SerializeField, Range(0.1f, 10.0f)]
    float obstacleMinHeight = 0.1f;
    [SerializeField, Range(0.1f, 10.0f)]
    float obstacleMaxHeight = 1.0f;
    [SerializeField, Range(0.0f, 5.0f)]
    float obstacleMinDistance = 0.5f;
    [SerializeField]
    bool randomizeObstacleRotation = true;


    private BoxCollider obstaclePatchBounds;
    private float terrainWidth;
    private float terrainLength;


    float GetBoundingRadius(GameObject obstacle) {
      return Mathf.Sqrt(obstacle.transform.localScale.x*obstacle.transform.localScale.x + obstacle.transform.localScale.z*obstacle.transform.localScale.z);
    }

    bool CheckInBounds(GameObject obstacle) {

      BoxCollider obstaclebounds = obstacle.GetComponent<BoxCollider>();
      if (obstaclebounds == null) { return false; }
      Vector3 obstacleMin = obstaclebounds.bounds.min + obstaclePatchBounds.center;
      Vector3 obstacleMax = obstaclebounds.bounds.max + obstaclePatchBounds.center;
      Vector3 patchMin = obstaclePatchBounds.bounds.min;
      Vector3 patchMax = obstaclePatchBounds.bounds.max;
      if (obstacleMin.x < patchMin.x || obstacleMax.x > patchMax.x) { return false; }
      if (obstacleMin.z < patchMin.z || obstacleMax.z > patchMax.z) { return false; }
      return true;
    }

    bool CheckCollision(GameObject obstacle, GameObject obstacles) {
      float boundingRadius = GetBoundingRadius(obstacle);
      //check if the obstaacles collider is outside the obstacle patch bounds
      // use the colliders
      if (!CheckInBounds(obstacle)) { return true; }

      return false;

      // foreach (Transform child in obstacles.transform) {
      //   if (child == obstacle.transform) { continue; }
      //   float distance = Vector3.Distance(obstacle.transform.position, child.position);
      //   float otherBoundingRadius = GetBoundingRadius(child.gameObject);
      //   if (distance < boundingRadius + otherBoundingRadius + obstacleMinDistance) {
      //     return true;
      //   }
      // }
      // return false;
    }

    Vector3 GetRandomObstaclePosition() {
      return new Vector3(
        Random.Range(-terrainWidth/2, terrainWidth/2),
        0,
        Random.Range(-terrainLength/2, terrainLength/2)
      );
    }

    Vector3 GetRandomObstacleScale() {
      return new Vector3(
        Random.Range(obstacleMinSize, obstacleMaxSize),
        Random.Range(obstacleMinHeight, obstacleMaxHeight),
        Random.Range(obstacleMinSize, obstacleMaxSize)
      );
    }


    void CreateObstacle(GameObject obstacles) {

      GameObject obstacle = Instantiate(obstaclePrefab);
      obstacle.transform.parent = obstacles.transform;
      obstacle.name = "Obstacle";

      int i = 0;
      int maxTries = 100;

      while (true) {
        i++;
        if (i > maxTries) { break; }
        obstacle.transform.localScale = GetRandomObstacleScale();
        obstacle.transform.localPosition = GetRandomObstaclePosition();
        //add vertical offset
        obstacle.transform.position += new Vector3(0, obstacle.transform.localScale.y/2, 0);

        if (!CheckCollision(obstacle, obstacles)) {
          break;
        }
      }

      if (randomizeObstacleRotation) {
        obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
      }
    }

    void CreateObstacles() {
      GameObject obstacles = new GameObject();
      obstacles.name = "Obstacles";
      obstacles.transform.parent = transform;
      obstacles.transform.position = transform.position;
      if (obstaclePrefab == null) { return; }
      for (int i = 0; i < obstacleCount; i++) {
        CreateObstacle(obstacles);
      }
      obstacles.transform.position = transform.position;
    }

    public void Recreate() {
      while (transform.childCount > 0) {
        DestroyImmediate(transform.GetChild(0).gameObject);

      }
      obstaclePatchBounds = GetComponent<BoxCollider>();
      terrainWidth = obstaclePatchBounds.size.x;
      terrainLength = obstaclePatchBounds.size.z;
      CreateObstacles();
    }


    //====================================================================================================
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
