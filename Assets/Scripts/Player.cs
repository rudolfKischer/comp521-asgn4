using System.Collections;
using System.Collections.Generic;
using UnityEngine;


enum PlayerBehaviourState {
    Attacking,
    Shooting,
    AcquiringTreasure,
    PursuingTreasure,
    PursuingMinotaur,
    ReturningToSpawn,
    DroppingTreasure
}

public class PlayerAction {
    public System.Action action;
    // precondition funtion, return true if the precondition is met
    public System.Func<bool> precondition;
    // Returns true if the post condition is met
    public System.Func<bool> postcondition;

    public PlayerAction(string name, 
    System.Action action, 
    System.Func<bool> precondition, 
    System.Func<bool> postcondition) {
        this.action = action;
        this.precondition = precondition;
        this.postcondition = postcondition;
    }
}

[RequireComponent(typeof(PathFinder))]
public class Player : MonoBehaviour
{
    [SerializeField]
    public GameObject meleePlayerPrefab;
    public GameObject rangePlayerPrefab;

    [SerializeField]
    public int maxHealth = 5;
    [SerializeField]
    public bool isRange = false;
    [SerializeField]
    public GameObject spawnPoint;

    [SerializeField, Range(1, 10)]
    public float projectileAttackRange = 5f;

    [SerializeField, Range(1, 10)]
    private float treasureRadius = 2.0f;
    [SerializeField, Range(1, 10)]
    private float minotaurRadius = 0.3f;
    [SerializeField, Range(1, 10)]
    private float spawnRadius = 0.1f;

    [SerializeField, Range(1, 10)]  
    private float treasurePickupRadius = 3.0f;
    [SerializeField, Range(1, 10)]
    private float treasurePickupTime = 2.0f;

    [SerializeField]
    private Material takeDamageMaterial;
    [SerializeField]
    private float takeDamageMaterialDuration = 0.25f;

    private float enteredPickupStartTime = - 1.0f;

    private GameObject treasure;
    private GameObject oldTreasureParent;
    private PathFinder pathFinder;
    private GameObject minotaur;

    private int currentHealth;
    private bool carryingTreasure = false;

    private Dictionary<PlayerBehaviourState, PlayerAction> stateBehaviours = new Dictionary<PlayerBehaviourState, PlayerAction>();

    private List<PlayerBehaviourState> plan = new List<PlayerBehaviourState>();

    private IEnumerator TakeDamageMaterialCoroutine() {

        Material originalMaterial = GetComponent<Renderer>().material;
        GetComponent<Renderer>().material = takeDamageMaterial;
        yield return new WaitForSeconds(takeDamageMaterialDuration);
        GetComponent<Renderer>().material = originalMaterial;
    }

    public void TakeDamage(int damage) {
        currentHealth -= damage;
        if (currentHealth <= 0) {
          // disable the player
          gameObject.SetActive(false);
        } else {
          StartCoroutine(TakeDamageMaterialCoroutine());
        }
    }



    // melee atack
    void AcquireTreasure() {
      pathFinder.SetGoal(gameObject);
      // Idle nearby the treasure
      if (GetPlanarDistance(transform.gameObject, treasure) > treasurePickupRadius) {
        pathFinder.SetGoal(treasure);
        enteredPickupStartTime = -1.0f;
        return;
      }
      // if we are close to the treasure, and the timer hasnt started, start the timer
      // pathFinder.SetGoal(transform.gameObject);
      if (enteredPickupStartTime == -1.0f) {
        enteredPickupStartTime = Time.time;
      }
      float elapsedTime = Time.time - enteredPickupStartTime;
      if (elapsedTime > treasurePickupTime) { PickUpTreasure(); Debug.Log("Picked up treasure"); }
    }

    void PickUpTreasure() {
      // make the treasure a child of the player
      // move it to above the players head
      if (treasure.transform.parent != null) {
          oldTreasureParent = treasure.transform.parent.gameObject;
      }
      treasure.transform.parent = transform;
      treasure.transform.position = transform.position + new Vector3(0, 2, 0);
      carryingTreasure = true;
    }

    void DropTreasure() {
      // make the treasure a child of the player
      // move it to above the players head
      if (oldTreasureParent != null) {
          treasure.transform.parent = oldTreasureParent.transform;
      } else {
          treasure.transform.parent = null;
      }
      // put the treasure on the ground where we are currently standing
      // it y should be hald its height above the ground
      float treasureHeight = treasure.transform.localScale.y;
      treasure.transform.position = new Vector3(transform.position.x, treasureHeight / 2, transform.position.z);
      carryingTreasure = false;
    }

    float GetPlanarDistance(GameObject a, GameObject b) {
        return Vector3.Distance(new Vector3(a.transform.position.x, 0, a.transform.position.z), 
                                new Vector3(b.transform.position.x, 0, b.transform.position.z));
    }

    void DefineBehaviours() {
        stateBehaviours[PlayerBehaviourState.PursuingTreasure] = new PlayerAction(
            "PursuingTreasure",
            () => { pathFinder.SetGoal(treasure); },
            () => { return true; },
            () => { 
              // remove y component from distance calculation
              float distance = GetPlanarDistance(transform.gameObject, treasure);
              bool isClose = distance < treasureRadius;
              if (isClose) { pathFinder.SetGoal(gameObject); }
              return isClose;
            }
        );
        stateBehaviours[PlayerBehaviourState.ReturningToSpawn] = new PlayerAction(
            "ReturningToSpawn",
            () => { pathFinder.SetGoal(spawnPoint); },
            () => { return true; },
            () => { 
              float distance = GetPlanarDistance(transform.gameObject, spawnPoint);
              return distance < spawnRadius;
            }
        );
        stateBehaviours[PlayerBehaviourState.PursuingMinotaur] = new PlayerAction(
            "PursuingMinotaur",
            () => { pathFinder.SetGoal(minotaur); },
            () => { return true; },
            () => { 
              float distance = GetPlanarDistance(transform.gameObject, minotaur);
              return distance < minotaurRadius;
            }
        );
        stateBehaviours[PlayerBehaviourState.AcquiringTreasure] = new PlayerAction(
            "AcquiringTreasure",
            () => { AcquireTreasure(); },
            () => { 
              float distance = GetPlanarDistance(transform.gameObject, treasure);
              // Debug.Log("Distance to treasure: " + distance);
              // Debug.Log("Carrying treasure: " + carryingTreasure);
              // Debug.Log("treasurePickupRadius: " + treasurePickupRadius);
              // Debug.Log("distance < treasurePickupRadius: " + (distance < treasurePickupRadius));
              return distance < treasurePickupRadius;
            },
            () => { 
              return carryingTreasure ;
            }
        );
        stateBehaviours[PlayerBehaviourState.DroppingTreasure] = new PlayerAction(
            "DropTreasure",
            () => { DropTreasure(); },
            () => { return true; },
            () => { return true;}
        );
    }


    //========================================================================= 
    void Start()
    {
        if ( isRange ) { currentHealth =maxHealth / 2; } else { currentHealth =maxHealth; };
        pathFinder = GetComponent<PathFinder>();
        treasure = GameObject.FindGameObjectWithTag("Treasure");
        minotaur = GameObject.FindGameObjectWithTag("Minotaur");

        // go to treature and then to spawn
        plan = new List<PlayerBehaviourState> {
            // PlayerBehaviourState.ReturningToSpawn,
            PlayerBehaviourState.PursuingTreasure,
            PlayerBehaviourState.AcquiringTreasure,
            PlayerBehaviourState.ReturningToSpawn,
            PlayerBehaviourState.DroppingTreasure,
            PlayerBehaviourState.PursuingMinotaur
        };

        DefineBehaviours();

        // set x and z position to spawn
        transform.position = new Vector3(
        spawnPoint.transform.position.x, 
        transform.position.y, 
        spawnPoint.transform.position.z);
        
    }

    void Update()
    {
        if (plan.Count == 0) { return; }
        var currentBehaviour = plan[0];

        Debug.Log("Current Behaviour: " + currentBehaviour);

        if (!stateBehaviours[currentBehaviour].precondition()) { return; }

        stateBehaviours[currentBehaviour].action();
        if (stateBehaviours[currentBehaviour].postcondition()) {
            plan.RemoveAt(0);
        }
        
    }
}
