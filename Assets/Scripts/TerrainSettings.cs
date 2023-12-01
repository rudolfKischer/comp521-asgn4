
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Unity.VisualScripting;
using Random = UnityEngine.Random;
using System.Xml.Serialization;



public class TerrainSettings : MonoBehaviour
{

    // This script is used to generate the terrain
    // The terrain consists of a floor and four walls
    // The terrain should is adjustable in the inspector

    // We want the be able to set the width of each wall
    // and we need to set the width and length of the floor
    // the walls should dynamicly adjust to fit the size

    // We also want the walls to be Dynamicly editable

    [Header("Terrain Settings")]
    [SerializeField]
    GameObject wallprefab;
    [SerializeField]
    GameObject foorPrefab;
    [SerializeField, Range(0.1f, 50.0f)]
    float terrainWidth = 1.0f;
    [SerializeField, Range(0.1f, 50.0f)]
    float terrainLength = 2.0f;
    [SerializeField, Range(0.01f, 1.0f)]
    float wallWidth = 0.1f;
    [SerializeField, Range(0.1f, 10.0f)]
    float wallHeight = 1.0f;
    [SerializeField, Range(0.1f, 10.0f)]
    float floorWidth = 1.0f;




    // Walls
    private GameObject[] walls = new GameObject[4];
    private GameObject floor;


    void SetFloor() {
      if (floor == null) { return; }
      floor.transform.localScale = new Vector3(terrainWidth, floorWidth, terrainLength);
      floor.transform.position = new Vector3(0, -floorWidth/2, 0);
    }

    void CreateFloor() {

      GameObject floor = Instantiate(foorPrefab);
      floor.transform.parent = transform;
      floor.name = "Floor";
      floor.tag = "terrain";
      this.floor = floor;
    }


    void SetWall(int wallNumber) {
      // wall numbers:
      // 0 = 0, +
      // 1 = +, 0
      // 2 = 0, -
      // 3 = -, 0
      // this is for updateing the wall to its correct position given its number

      // We need to set the size and position of the wall
      // the position should be on the edge of the terrain + the wall width

      // the terrain width is the x direction and the terrain length is the z direction


      GameObject wall = walls[wallNumber];

      if (wall == null) { return; }

      Vector3[] wallPositions = {
        new Vector3(0, wallHeight/2, terrainLength/2 + wallWidth/2),
        new Vector3(terrainWidth/2 + wallWidth/2, wallHeight/2, 0),
        new Vector3(0, wallHeight/2, -terrainLength/2 - wallWidth/2),
        new Vector3(-terrainWidth/2 -wallWidth/2, wallHeight/2, 0)
      };

      Vector3[] wallScales = {
        new Vector3(terrainWidth + 2*wallWidth, wallHeight, wallWidth),
        new Vector3(wallWidth, wallHeight, terrainLength + 2*wallWidth),
        new Vector3(terrainWidth + 2*wallWidth, wallHeight, wallWidth),
        new Vector3(wallWidth, wallHeight, terrainLength + 2*wallWidth)
      };

      wall.transform.position = wallPositions[wallNumber];
      wall.transform.localScale = wallScales[wallNumber];

    }

    void SetWalls() {
      for (int i = 0; i < 4; i++) {
        SetWall(i);
      }
    }

    void SetTerrain(){
      SetWalls();
      SetFloor();
    }


    void CreateWall(int wallNumber) {
        // We want to create four walls around the edge of the terrain
        String[] wallNames = {"TopWall", "RightWall", "BottomWall", "LeftWall"};
        GameObject wall = Instantiate(wallprefab);
        wall.tag = "terrain";
        walls[wallNumber] = wall;
        wall.name = wallNames[wallNumber];
        //attach the wall to the terrain
        wall.transform.parent = transform;

    }



    void CreateWalls() {
      for (int i = 0; i < 4; i++) {
        CreateWall(i);

      }
    }


    public void Recreate() {
      while (transform.childCount > 0) {
        DestroyImmediate(transform.GetChild(0).gameObject);
      }
      CreateWalls();
      CreateFloor();
      SetTerrain();
    }







    //====================================================================================================


    void Start() {
    }



}
