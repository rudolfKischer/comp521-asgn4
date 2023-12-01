using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PathFinder : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    GameObject goal;
    NavMeshAgent agent;

    void Start()
    {
      agent = GetComponent<NavMeshAgent>();
      agent.destination = goal.transform.position;
        
    }

    // Update is called once per frame
    void Update()
    {
      agent.destination = goal.transform.position;
    }



}
