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
    public bool isRange = true;
    [SerializeField]
    public GameObject spawnPoint;

    [SerializeField, Range(1, 10)]
    public float projectileAttackRange = 5f;
    [SerializeField, Range(1, 10)]
    // if the minotaur is closer than this, we need to move away
    public float minimumProjectileAttackRange = 3f;

    [SerializeField, Range(1, 10)]
    public float meleeAttackRange = 3f;

    [SerializeField, Range(1, 10)]
    private float treasureRadius = 2.0f;
    [SerializeField, Range(1, 10)]
    private float minotaurRadius = 2.5f;
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


    public int currentHealth;
    private bool carryingTreasure = false;

    private Dictionary<PlayerBehaviourState, PlayerAction> stateBehaviours = new Dictionary<PlayerBehaviourState, PlayerAction>();

    private List<PlayerBehaviourState> plan = new List<PlayerBehaviourState>();

    private GameObject playerModel;

    [SerializeField]
    private float attackCooldown = 1.0f;

    private float lastAttackTime = -1.0f;

    private float attackAnimationDuration = 0.5f;

    public bool isDead = false;

    private Animator swordAnimator;
    private GameObject rifleShot;

    [SerializeField]
    public GameObject coverSpotsParent;
    private List<GameObject> coverSpots = new List<GameObject>();

    private IEnumerator StopAttackAnimationCoroutine() {
        yield return new WaitForSeconds(attackAnimationDuration);
        // get the reneere of the player model
        // get the player model, and check if it has a sword "Sword"
        // if it does, 

        // rotate the sword overtime by 90 degrees
        if (swordAnimator != null) {
            swordAnimator.SetTrigger("finishAttacking");
        }

    }

    private void AttackAnimation() {
        // get the reneere of the player model
        // get the player model, and check if it has a sword "Sword"
        // if it does, 
        if (swordAnimator != null) {
            swordAnimator.SetTrigger("attackTrigger");  
        }
        StartCoroutine(StopAttackAnimationCoroutine());
    }

    private void Attack() {
      // if we are close enough to the minotaur, attack it
      float distance = GetPlanarDistance(transform.gameObject, minotaur);
      Debug.Log("Distance to minotaur: " + distance);
      Debug.Log("meleeAttackRange: " + meleeAttackRange);
      if (distance > meleeAttackRange) { pathFinder.SetGoal(minotaur); return; }
      Debug.Log("Attacking minotaur");
      // attack the minotaur
      pathFinder.SetGoal(gameObject);
      AttackAnimation();
      Minotaur minotaurScript = minotaur.GetComponent<Minotaur>();
      minotaurScript.TakeDamage(1, gameObject);
      lastAttackTime = Time.time;

    }

    private bool HasLineOfSight() {
      // check if there is a line of sight between the player and the minotaur
      // if there is, return true
      // if there isnt, return false

      // use a raycast to check if there is a line of sight
      RaycastHit hit;
      Vector3 direction = minotaur.transform.position - transform.position;
      bool raycastResult = Physics.Raycast(transform.position, direction, out hit);

      // draw Debug ray
      // Debug.DrawRay(transform.position, direction, Color.red, 1.0f);
      if (raycastResult) {
        if (hit.transform.gameObject == minotaur) {
          return true;
        }
      }
      return false;
    }

    private IEnumerator ShootAnimation() {
      // enable the shot for 0.1 seconds
      
      rifleShot.SetActive(true);
      yield return new WaitForSeconds(0.1f);
      rifleShot.SetActive(false);
    }

    bool isFacingMinotaur() {
      Vector3 directionToMinotaur = minotaur.transform.position - transform.position;
      float angleToMinotaur = Vector3.Angle(transform.forward, directionToMinotaur);
      float maxFacingAngleError = 5.0f;
      return angleToMinotaur < maxFacingAngleError;
    }

    private GameObject GetCoverSpot()
    {
      // Find the closest cover spot to us, that is not within some minimum distance of the minotaur
      // find a cover spot, that is away from the minotaur

      GameObject closestCoverSpot = null;
      float closestDistance = float.MaxValue;
      float minimumCoverSpotDistance = 10.0f;

      foreach (GameObject coverSpot in coverSpots)
      {
        float distanceToMinotaur = GetPlanarDistance(coverSpot, minotaur);
        if (distanceToMinotaur > minimumCoverSpotDistance)
        {
          float distanceToPlayer = GetPlanarDistance(coverSpot, transform.gameObject);
          if (distanceToPlayer < closestDistance)
          {

            Vector3 directionToMinotaur = minotaur.transform.position - transform.position;
            Vector3 directionToCoverSpot = coverSpot.transform.position - transform.position;
            float angleToMinotaur = Vector3.Angle(directionToMinotaur, directionToCoverSpot);
            float maxFacingAngleError = 180.0f;
            if (angleToMinotaur > maxFacingAngleError) { continue; }
            closestDistance = distanceToPlayer;
            closestCoverSpot = coverSpot;
          }
        }
      }

      // if did not finda cover spot pick the first one
      if (closestCoverSpot == null)
      {
        closestCoverSpot = coverSpots[0];
      }

      return closestCoverSpot;
    }

    private void Shoot()
    {
      lastAttackTime = -1.0f;
      // check for line , if not navigate to minotaur
      Vector3 directionToMinotaur = minotaur.transform.position - transform.position;

      float distance = GetPlanarDistance(transform.gameObject, minotaur);
      if (distance < minimumProjectileAttackRange)
      {
        // move away from the minotaur
        // pick a the closest cover spot, that is not within some minimum distance of the minotaur
        // and navigate to it

        // Find the closest cover spot that is not within some minimum distance of the minotaur
        GameObject closestCoverSpot = GetCoverSpot();
        if (closestCoverSpot != null)
        {
          pathFinder.SetGoal(closestCoverSpot);
          return;
        }
      }

      if (!HasLineOfSight())
      {
        pathFinder.SetGoal(minotaur);
        return;
      }



      pathFinder.SetGoal(gameObject);
      // check that we are facing the minotaur
      

      if (!isFacingMinotaur()) {
        // move towards facing the minotaur
        Quaternion targetRotation = Quaternion.LookRotation(directionToMinotaur);
        float maxRotationSpeed = 360.0f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxRotationSpeed * Time.deltaTime);
        return;
      }


      StartCoroutine(ShootAnimation());
      Minotaur minotaurScript = minotaur.GetComponent<Minotaur>();
      minotaurScript.TakeDamage(1, gameObject);
      lastAttackTime = Time.time;
      // if we are close enough to the minotaur, attack it
    }

    private IEnumerator TakeDamageMaterialCoroutine() {

        // get the rendere of the player model
        Renderer playerModelRenderer = playerModel.GetComponent<Renderer>();
        Material originalPlayerModelMaterial = playerModelRenderer.material;
        playerModelRenderer.material = takeDamageMaterial;

        yield return new WaitForSeconds(takeDamageMaterialDuration);

        playerModelRenderer.material = originalPlayerModelMaterial;

    }

    public void TakeDamage(int damage) {
        currentHealth -= damage;
        if (currentHealth <= 0) {
          // disable the player
          isDead = true;
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
              Debug.Log("Distance to minotaur: " + distance);
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
        stateBehaviours[PlayerBehaviourState.Attacking] = new PlayerAction(
            "Attack",
            () => { Attack(); },
            () => { 
              float elapsedTime = Time.time - lastAttackTime;
              return lastAttackTime == -1.0f || elapsedTime > attackCooldown;
            },
            () => { // return true if we are passed the attack cooldown
              float distance = GetPlanarDistance(transform.gameObject, minotaur);
              return distance <= meleeAttackRange;
            }
        );
        stateBehaviours[PlayerBehaviourState.Shooting] = new PlayerAction(
            "Shoot",
            () => { Shoot(); },
            () => { 
              float elapsedTime = Time.time - lastAttackTime;
              if (!isRange) { return false; }
              return lastAttackTime == -1.0f || elapsedTime > attackCooldown;
            },
            () => { // return true if we are passed the attack cooldown
              // check if we have line of sight
              return lastAttackTime != -1.0f;
            }
        );
    }


    //========================================================================= 
    void Start()
    {

        if (isRange) {
            playerModel = Instantiate(rangePlayerPrefab, transform);
            rifleShot = playerModel.transform.Find("shot").gameObject;
            // disable it
            rifleShot.SetActive(false);

        } else {
            playerModel = Instantiate(meleePlayerPrefab, transform);
            GameObject sword = playerModel.transform.Find("Sword").gameObject;
            if (sword != null) {
                // rotate the sword overtime by 90 degrees
                swordAnimator = sword.GetComponent<Animator>();
            }
        }
        playerModel.transform.localPosition = Vector3.zero;

        // get all the children of the cover spots parent
        // add them to the cover spots list
        foreach (Transform child in coverSpotsParent.transform) {
            coverSpots.Add(child.gameObject);
        }

        if ( isRange ) { currentHealth =maxHealth / 2; } else { currentHealth =maxHealth; };
        pathFinder = GetComponent<PathFinder>();
        treasure = GameObject.FindGameObjectWithTag("Treasure");
        minotaur = GameObject.FindGameObjectWithTag("Minotaur");

        // go to treature and then to spawn
        plan = new List<PlayerBehaviourState> {
            // PlayerBehaviourState.ReturningToSpawn,
            // PlayerBehaviourState.PursuingTreasure,
            // PlayerBehaviourState.AcquiringTreasure,
            // PlayerBehaviourState.ReturningToSpawn,
            // PlayerBehaviourState.DroppingTreasure,
            PlayerBehaviourState.Shooting,
            // PlayerBehaviourState.ReturningToSpawn,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.Shooting,
            // PlayerBehaviourState.ReturningToSpawn,

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
        if (isDead) {
          //disable
          gameObject.SetActive(false);
        }
        if (plan.Count == 0) { return; }
        var currentBehaviour = plan[0];

        Debug.Log("Current Behaviour: " + currentBehaviour);

        if (!stateBehaviours[currentBehaviour].precondition()) { return; }

        stateBehaviours[currentBehaviour].action();
        if (stateBehaviours[currentBehaviour].postcondition()) {
            plan.RemoveAt(0);
        }

        //line of sight
        Debug.Log("Has line of sight: " + HasLineOfSight());


        
    }
}
