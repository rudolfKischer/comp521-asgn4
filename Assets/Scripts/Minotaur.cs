using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;

enum MinotaurBehaviourState {
    Pursue,
    Gaurd,
    Attacking
}


[RequireComponent(typeof(PathFinder))]
public class Minotaur : MonoBehaviour
{
    /*
    * The minotaur needs to accomplish the following:
    * 1. Gaurd the treasure, whic means it must be around it when it can
    * 2. Chases players within a certain radius
    * 3. Have an Idle behaviour when not doing any thing
    * 4. Needs to be able to follow and attack players, prioritised in this order:
    *     -  Who is currently attacking the minotaur most recently
    *     -  Who is closest to the treasure
    *     -  who is closest to the minotaur
    * 5. Needs to be able to attack players using an area of effect attack
    */     

    // Default state is gaurding the treasure
    // we will walk around the treasure in a circle

    // if we are attacked we pursue an attacker
    // we continue to purse this attacker until the following conditions are met:
    // 1. The attacker is dead
    // 2. The attacker is out of the treasure
    

    // if we are not currently being attacked, and there is a player within
    // a certain radius of the treasure, we will pursue them
    // we should pursue the player closest to the treasure
    // same conditions as above apply

    // if we are not currently being attacked, and there is no player within
    // if no one is within a radius of the treasure
    // and someone is close to us, we will pursure them
    // if another player starts attacking us, we will switch to them
    // if no one is attacking us, we will return to gaurding the treasure
    // if someone enters the treasure radius, we will switch to them


    [SerializeField]
    GameObject treasure;
    [SerializeField]
    PathFinder pathFinder;

    [SerializeField, Range(1, 10)]
    float gaurdRadius = 3f;
    private GameObject circleTarget;
    private int circletracker = 0;
    private int circleResolution = 10;
    private MinotaurBehaviourState state = MinotaurBehaviourState.Gaurd;

    [Header("Treasure Pursue Settings")]
    [SerializeField, Range(1, 20)]
    float treasureMinPursueRadius = 10f;
    [SerializeField, Range(1, 20)]
    float treasureMaxPursueRadius = 20f;

    [Header("Attack Radius Settings")]
    [SerializeField, Range(1, 10)]
    float attackRadius = 2f;
    [SerializeField, Range(1, 10)]
    float pursueRadius = 5f;
    [SerializeField, Range(1, 10)]
    float attackCooldown = 2f;

    [SerializeField]
    ParticleSystem attackEffect;
    [SerializeField]
    ParticleSystem chargeEffect;
    [SerializeField]
    float attackAnimationDuration = 0.8f;


    private float attackTimer;
    private float attackAnimationTimer;
    private GameObject[] players;
    private GameObject currentTarget;

    private Dictionary<MinotaurBehaviourState, System.Action> stateBehaviours = new Dictionary<MinotaurBehaviourState, System.Action>();


    // Pursue behaviour

    






    //idle behavior
    private void CircleTreasure()
    {
        // we will walk around the treasure in a circle

        //create a empty game object that will be the move target
        // this will be moved around the treasure

        // what we will do is have come counter that will be incremented
        // every time the minotaur reaches the target
        // we will increment the counter

        // the counter will designate where in the circle the minotaur is
        
        //make the circle resolution poporpitional the the gaurd radius and the minotaur size
        circleResolution = (int)(2 * Mathf.PI * gaurdRadius / GetComponent<NavMeshAgent>().radius);
        
        Vector3 treasure_center = treasure.transform.position;
        float arc = 2 * Mathf.PI / circleResolution;
        float arc_angle = arc * circletracker;
        Vector3 circle_target = treasure_center + new Vector3(Mathf.Cos(arc_angle), 0, Mathf.Sin(arc_angle)) * gaurdRadius;
        circleTarget.transform.position = circle_target;

        // calculate the distance between the minotaur and the target, ignoreing the y axis
        Vector3 minotaur_pos = transform.position;
        minotaur_pos.y = 0;
        Vector3 target_pos = circle_target;
        target_pos.y = 0;
        float distance = Vector3.Distance(minotaur_pos, target_pos);

        // make the the threshold for eaching the target 1.5x the size of the minotaur
        float meeting_distance = GetComponent<NavMeshAgent>().radius * 1.5f;
        if (distance < meeting_distance)
        {
            circletracker++;
            circletracker = circletracker % circleResolution;
        }

        pathFinder.SetGoal(circleTarget);
    }

    // if someone is within our attack radius, we pursure them
    // if someone is in the treasure radius, we pursure the player closes to the treasure

    (GameObject, float) GetClosestPlayer(Vector3 position)
    {
        GameObject closest = null;
        float closest_distance = Mathf.Infinity;
        foreach (GameObject player in players)
        {
            // if player is disabled
            if (!player.activeSelf) { continue; }
            float distance = Vector3.Distance(position, player.transform.position);
            if (distance < closest_distance)
            {
                closest = player;
                closest_distance = distance;
            }
        }
        return (closest, closest_distance);
    }

    bool ShouldAttack() {
        if (Time.time - attackTimer < attackCooldown) { return false; }
        if (players.Length == 0) { return false; }
        var (closest, closest_distance) = GetClosestPlayer(transform.position);
        return closest_distance < attackRadius;
    }

    GameObject ShouldPursue() {

        if (players.Length == 0) { return null; }

        var (closest, closest_distance) = GetClosestPlayer(transform.position);
        var (closest_treasure, closest_treasure_distance) = GetClosestPlayer(treasure.transform.position);
        if (closest_distance < pursueRadius)
        {
            return closest;
        }
        else if (closest_treasure_distance < treasureMinPursueRadius)
        {
            return closest_treasure;
        }
        else
        {
            return null;
        }

    }

    void Pursue() {
        pathFinder.SetGoal(currentTarget);
    }

    void AnimateAttack() {
       // Animation :
        // 1. minotaur grows skinnier and taller slowly
        // 2. the becomes shorter and fatter quickly
        // 3. then restores to original size
        if (attackEffect == null) { return; }
        if (chargeEffect == null) { return; }

        float timeElapsed = Time.time - attackAnimationTimer;
        //for the first 80 % of the animation, play the charge effect
        if (timeElapsed < attackAnimationDuration * 0.9f) {
            chargeEffect.Play();
            return;
        }
        // for the last 20% of the animation, play the attack effect
        if (timeElapsed < attackAnimationDuration * 0.95f) {
            // chargeEffect.Stop();
            attackEffect.Play();
            return;
        }

        // for the last 5% of the animation, stop the attack effect
        if (timeElapsed < attackAnimationDuration) {
            chargeEffect.Stop();
            attackEffect.Stop();
            return;
        }



    }

    void DamagePlayer() {
        //look for players within the attack radius
        // if there are players within the attack radius, damage them
        // get theire Player script and call the TakeDamage function
        for (int i = 0; i < players.Length; i++) {
            // if the player is disabled, skip them
            if (!players[i].activeSelf) { continue; }
            GameObject player = players[i];
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < attackRadius) {
                Player playerScript = player.GetComponent<Player>();
                if (playerScript == null) { continue; }
                playerScript.TakeDamage(1);
            }
        }
    }

    void Attack() {
        // we Want to stop the minotaur from moving
        // then we want to play the attack animation
        // our attack animations can be that the minotaur will 
        // we need to remove the target of the pathfinder

        //we want to create a particle effect to indicate the attack

        pathFinder.SetGoal(this.gameObject);
        AnimateAttack();
        float elapsed = Time.time - attackAnimationTimer;
        // Debug.Log("Attack Animation Timer: " + elapsed);

        if (elapsed > attackAnimationDuration) {
            attackTimer = Time.time;
            attackAnimationTimer = 0;
            DamagePlayer();
        }

        //look for players within the attack radius
        // if there are players within the attack radius, damage them
        // get theire Player script and call the TakeDamage function
    }

    void DefiningBehaviours()
    {
        stateBehaviours.Add(MinotaurBehaviourState.Gaurd, CircleTreasure);
        stateBehaviours.Add(MinotaurBehaviourState.Pursue, Pursue);
        stateBehaviours.Add(MinotaurBehaviourState.Attacking, Attack);
    }

    void DetermineState() {
        GameObject target = ShouldPursue();
        if ( attackAnimationTimer > 0 && MinotaurBehaviourState.Attacking == state) { return; }
        if (ShouldAttack()) {
          attackAnimationTimer = Time.time;
          state = MinotaurBehaviourState.Attacking; 
          return; }
        if (target == null) { state = MinotaurBehaviourState.Gaurd; return; }
        currentTarget = target;
        state = MinotaurBehaviourState.Pursue;
    }

    void GetPlayers()
    {
        // get all the players from the scene
        players = GameObject.FindGameObjectsWithTag("Player");
    }

    //==========================================================================

    void Awake()
    {
        attackTimer = Time.time;
    }

    void Start()
    {
        circleTarget = new GameObject("CircleTarget");
        circleTarget.transform.parent = treasure.transform;
        pathFinder = GetComponent<PathFinder>();
        DefiningBehaviours();
        GetPlayers();

        //stop the animations from playing
        attackEffect.Stop();
        chargeEffect.Stop();
    }

    void Update()
    {
      DetermineState();
      // Debug.Log("Minotaur State: " + state);
      stateBehaviours[state]();
    }





}
