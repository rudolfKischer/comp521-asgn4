using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TMPro.TextMeshPro))]
public class ScreenOverlay : MonoBehaviour
{

    public GameObject treasure;
    public GameObject[] spawnPoints;

    public GameObject[] players;

    public float treasureSpawnMinDistance = 0.5f;

    TMPro.TextMeshPro t;

    bool treasureAtSpawnPoint() {
      //loop through spawn points
      for (int i = 0; i < spawnPoints.Length; i++) {
        //if treasure is at spawn point

        //get the planar distance without the y axis
        float distance = Vector3.Distance(new Vector3(spawnPoints[i].transform.position.x, 0, spawnPoints[i].transform.position.z),
         new Vector3(treasure.transform.position.x, 0, treasure.transform.position.z));

         if (distance < treasureSpawnMinDistance) {
           //return true
           return true;
         }
      }
      //return false
      return false;
    }

    bool allPlayersDead() {
      //loop through players
      for (int i = 0; i < players.Length; i++) {
        //if player is not dead
        if (players[i] == null) {
          return false;
        }
        if (players[i].GetComponent<Player>().isDead == false) {
          //return false
          return false;
        }
      }
      //return true
      return true;
    }

    // Start is called before the first frame update
    void Start()
    {
      //disable this object
      // this.gameObject.SetActive(false);

      // get all the players from the scene, they have the tage "Player"

      // we want to check all object, including children

      //find all objects with the tag "Player"
      players = GameObject.FindGameObjectsWithTag("Player");

      // we want to find all spawn points, with tag "SpawnPoint"
      spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

      t = GetComponent<TMPro.TextMeshPro>();

      if (t == null) {
        Debug.Log("Text Mesh Pro is null");
        return;
      }


    }

    // Update is called once per frame
    void Update()
    {
      // if the players are all dead, enable this object
      // and add "Players lose" to the textMesh pro element text at t
      if (t == null) {
        Debug.Log("Text Mesh Pro is null");
        return;
      }


      // it should just get appended
      if (allPlayersDead()) {
        t.text = "\nPlayers Lose!";
      }

      // if the treasure is at a spawn point, enable this object
      // and add "Treasure found" to the textMesh pro element text at t
      // it should just get appended
      if (treasureAtSpawnPoint()) {
        t.text = "\nTreasure found!";
      }
        
    }
}
