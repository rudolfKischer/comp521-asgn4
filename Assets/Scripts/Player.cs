using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PlayerBehaviourState {
    Attacking,
    Shooting,
    AcquiringTreasure,
    PursuingTreasure,
    PursuingMinotaur,
    PursuingLineOfSight,
    FleeingToCover,
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

public class Planner {
    // this is a SHOP planner
    // with total order planning
    // we want to take in a player, and the current state of the world
    // we would take in the goal, but the goal is always the same, to capture the treasure
    // by bringing it back to spawn 
    
    // the world state is represented by 
    // - IsNearMinotaur
		// - IsNearTreasure
		// - IsTreasureGrounded
		// - IsHoldingTreasure
		// - MinotaurIsVisible
		// - IsRanged
		// - IsTreasureSeekerAssigned

    // the planning is as follow
    // if there is already a treasure seeker assigned
    //    - if ranged,navigate to the minotaur and shoot it
    //    - if melee, navigate to the minotaur and attack it

    //    - if we complete the plan, we ask for a new plan

    // if there is no treasure seeker assigned, we will be the treasure seeker
    //   - we will navigate to the treasure
    //   - we will pick up the treasure
    //   - we will navigate to the spawn
    //   - we will drop the treasure

    //   - if we drop the treasure, we need to unassign the treasure seeker

    public List<PlayerBehaviourState> getPlan(GameObject player) {
       Player playerScript = player.GetComponent<Player>();

      bool playerSeekerAssigned = Player.playerSeekerAssigned;
      bool isRange = playerScript.isRange;

      float lastTakenDamageTime = playerScript.lastTakenDamageTime;
      float treasurePickupCooldown = playerScript.treasurePickupCooldown;
      bool isClosestPlayer = playerScript.isClosestPlayerToTreasure();
      // if there is already a treasure seeker assigned
      if (playerSeekerAssigned || (lastTakenDamageTime < treasurePickupCooldown && lastTakenDamageTime != -1.0f || !isClosestPlayer)) {
        // if ranged,navigate to the minotaur and shoot it
        if (isRange) {
          // if we are too close to the minotaur or they have an attack cooldown, flee to cover

          if (playerScript.isCloseToMinotaur() || playerScript.onAttackCooldown()) {
            return new List<PlayerBehaviourState> {
              PlayerBehaviourState.FleeingToCover,
            };
          }

          return new List<PlayerBehaviourState> {
            PlayerBehaviourState.PursuingLineOfSight,
            PlayerBehaviourState.Shooting,
            PlayerBehaviourState.FleeingToCover,
          };
        } else {
          if (playerScript.onAttackCooldown()) {
            return new List<PlayerBehaviourState> {
              PlayerBehaviourState.PursuingMinotaur,
            };
          }
          // if melee, navigate to the minotaur and attack it
          return new List<PlayerBehaviourState> {
            PlayerBehaviourState.PursuingMinotaur,
            PlayerBehaviourState.Attacking,
          };
        }
      } else {
        // if there is no treasure seeker assigned, we will be the treasure seeker
        //   - we will navigate to the treasure
        //   - we will pick up the treasure
        //   - we will navigate to the spawn
        //   - we will drop the treasure

        //   - if we drop the treasure, we need to unassign the treasure seeker
        return new List<PlayerBehaviourState> {
          PlayerBehaviourState.PursuingTreasure,
          PlayerBehaviourState.AcquiringTreasure,
          PlayerBehaviourState.ReturningToSpawn,
          PlayerBehaviourState.DroppingTreasure,
        };
      }



    }




}


[RequireComponent(typeof(PathFinder))]
public class Player : MonoBehaviour
{
    [SerializeField]
    private float planRefreshCoolDown = 0.1f;
    private float lastPlanRefreshTime = -1.0f;

    [SerializeField]
    public float takenDamageCooldown = 0.2f;
    public float lastTakenDamageTime = -1.0f;

    
    private GameObject[] spawnPoints;

    public float treasureSpawnMinDistance = 0.5f;


    public static bool playerSeekerAssigned = false;
    public bool isSeeker = false;

    // text mesh pro element
    [SerializeField]
    private TMPro.TextMeshPro textMeshPro;

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
    private float treasurePickupTime = 3.0f;
    [SerializeField, Range(1, 10)]
    public float treasurePickupCooldown = 3.0f;

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
    public bool carryingTreasure = false;

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


    public bool isClosestPlayerToTreasure() {
      GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); 
      GameObject closestPlayer = null;
      float closestDistance = float.MaxValue;
      foreach (GameObject player in players) {
        // if the player is not enable or dead, skip
        if (!player.activeSelf) { continue; }
        if (player.GetComponent<Player>().isDead) { continue; }
        Player playerScript = player.GetComponent<Player>();
        if (playerScript.carryingTreasure) { continue; }
        float distance = GetPlanarDistance(player, treasure);
        float random = Random.Range(0.0f, 1.0f);
        if (random < 0.5f) { continue; }
        if (distance < closestDistance) {
          closestDistance = distance;
          closestPlayer = player;
        }
      }

      // 1/4 chance it return false anyway
      return closestPlayer == gameObject;
    }

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
      // Debug.Log("Distance to minotaur: " + distance);
      // Debug.Log("meleeAttackRange: " + meleeAttackRange);
      if (distance > meleeAttackRange) { pathFinder.SetGoal(minotaur); return; }
      // Debug.Log("Attacking minotaur");
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

    private void FleeToCover() {
      // pick a the closest cover spot, that is not within some minimum distance of the minotaur
      // and navigate to it
      // if there is a line of sight, keep moving away from the minotaur


      // Find the closest cover spot that is not within some minimum distance of the minotaur
      GameObject closestCoverSpot = GetCoverSpot();
      if (closestCoverSpot != null)
      {
        pathFinder.SetGoal(closestCoverSpot);
        return;
      }
    }

    public bool isCloseToMinotaur() {
      float distance = GetPlanarDistance(transform.gameObject, minotaur);
      return distance < minimumProjectileAttackRange;
    }

    private void PursueLineOfSight() {
      if (!HasLineOfSight()) {
        pathFinder.SetGoal(minotaur);
        return;
      }
      pathFinder.SetGoal(gameObject);
      if (!isFacingMinotaur()) {
        // move towards facing the minotaur
        Quaternion targetRotation = Quaternion.LookRotation(minotaur.transform.position - transform.position);
        float maxRotationSpeed = 360.0f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxRotationSpeed * Time.deltaTime);
        return;
      }
    }

    private void Shoot()
    {
      // lastAttackTime = -1.0f;

      StartCoroutine(ShootAnimation());
      Minotaur minotaurScript = minotaur.GetComponent<Minotaur>();
      minotaurScript.TakeDamage(1, gameObject);
      lastAttackTime = Time.time;
      // if we are close enough to the minotaur, attack it
    }

    // private void Shoot()
    // {
    //   lastAttackTime = -1.0f;
    //   // check for line , if not navigate to minotaur
    //   Vector3 directionToMinotaur = minotaur.transform.position - transform.position;

    //   float distance = GetPlanarDistance(transform.gameObject, minotaur);
    //   if (distance < minimumProjectileAttackRange)
    //   {
    //     // move away from the minotaur
    //     // pick a the closest cover spot, that is not within some minimum distance of the minotaur
    //     // and navigate to it

    //     // Find the closest cover spot that is not within some minimum distance of the minotaur
    //     GameObject closestCoverSpot = GetCoverSpot();
    //     if (closestCoverSpot != null)
    //     {
    //       pathFinder.SetGoal(closestCoverSpot);
    //       return;
    //     }
    //   }

    //   if (!HasLineOfSight())
    //   {
    //     pathFinder.SetGoal(minotaur);
    //     return;
    //   }



    //   pathFinder.SetGoal(gameObject);
    //   // check that we are facing the minotaur
      

    //   if (!isFacingMinotaur()) {
    //     // move towards facing the minotaur
    //     Quaternion targetRotation = Quaternion.LookRotation(directionToMinotaur);
    //     float maxRotationSpeed = 360.0f;
    //     transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxRotationSpeed * Time.deltaTime);
    //     return;
    //   }


    //   StartCoroutine(ShootAnimation());
    //   Minotaur minotaurScript = minotaur.GetComponent<Minotaur>();
    //   minotaurScript.TakeDamage(1, gameObject);
    //   lastAttackTime = Time.time;
    //   // if we are close enough to the minotaur, attack it
    // }

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

        // if we take damage, we enter a cool down
        // in this time, we dont get new plan
        // and we delete our old plan
        // if (lastTakenDamageTime == -1.0f) {
        lastTakenDamageTime = Time.time;
        plan = new List<PlayerBehaviourState>();
        if (isSeeker) {
          playerSeekerAssigned = false;
          isSeeker = false;
        }
        // }

        //if is holding treasure, drop it
        if (carryingTreasure) {
          DropTreasure();
        }

        if (currentHealth <= 0) {
          // disable the player
          isDead = true;
          if (isSeeker) {
            playerSeekerAssigned = false;
            isSeeker = false;
          }
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

      // if we still have a pickup cooldown, dont pick up the treasure
      float elapsedTimePickupCooldown = Time.time - lastTakenDamageTime;
      if (elapsedTimePickupCooldown < treasurePickupCooldown && lastTakenDamageTime != -1.0f) {
        return;
      }

      // if we are close to the treasure, and the timer hasnt started, start the timer
      // pathFinder.SetGoal(transform.gameObject);
      if (enteredPickupStartTime == -1.0f) {
        enteredPickupStartTime = Time.time;
      }
      float elapsedTime = Time.time - enteredPickupStartTime;
      if (elapsedTime > treasurePickupTime ) { PickUpTreasure();}
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
      playerSeekerAssigned = true;
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
      playerSeekerAssigned = false;
    }

    float GetPlanarDistance(GameObject a, GameObject b) {
        return Vector3.Distance(new Vector3(a.transform.position.x, 0, a.transform.position.z), 
                                new Vector3(b.transform.position.x, 0, b.transform.position.z));
    }

    public bool onAttackCooldown() {
      float elapsedTime = Time.time - lastAttackTime;
      return lastAttackTime != -1.0f && elapsedTime < attackCooldown;
    }

    void DefineBehaviours() {
        stateBehaviours[PlayerBehaviourState.PursuingTreasure] = new PlayerAction(
            "PursuingTreasure",
            () => { pathFinder.SetGoal(treasure); isSeeker = true; playerSeekerAssigned = true; },
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
              // Debug.Log("Distance to minotaur: " + distance);
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
              // float elapsedTime = Time.time - lastAttackTime;
              // return lastAttackTime == -1.0f || elapsedTime > attackCooldown;
              return !onAttackCooldown();
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
              if (!isRange) { return false; }
              // float elapsedTime = Time.time - lastAttackTime;
              // return lastAttackTime == -1.0f || elapsedTime > attackCooldown;
              return !onAttackCooldown();
            },
            () => { // return true if we are passed the attack cooldown
              // check if we have line of sight
              return lastAttackTime != -1.0f;
            }
        );
        stateBehaviours[PlayerBehaviourState.PursuingLineOfSight] = new PlayerAction(
            "PursuingLineOfSight",
            () => { PursueLineOfSight(); },
            () => { 
              // must not be close to the minotaur
              return !isCloseToMinotaur();
            },
            () => { 
              // if we have line of sight, and we are facing the minotaur, return true
              return HasLineOfSight() && isFacingMinotaur();
            }
        );
        stateBehaviours[PlayerBehaviourState.FleeingToCover] = new PlayerAction(
            "FleeingToCover",
            () => { FleeToCover(); },
            () => { 
              // must be close to the minotaur
              return true;
            },
            () => { 
              // we must not be too close to the minotaur, and not have line of sight
              //check that we have made it to cover
              bool isCloseToCover = GetPlanarDistance(transform.gameObject, GetCoverSpot()) < 0.1f;
              return !isCloseToMinotaur() || !HasLineOfSight();
            }
        );

    }

    void updateText()
    {
      // make the text face the main camera
      // set the text to the current state

      // use the negative of the forward vector to make the text face the camera
      Vector3 text_forward = Camera.main.transform.forward;
      textMeshPro.transform.forward = text_forward;
      // set the test to the current plan
      // enumerate the steps in the plan
      string planString = "";
      foreach (PlayerBehaviourState state in plan) {
        // display the one at the top of the list in a different color
        if (state == plan[0]) {
          planString += "<color=red>";
        }
        planString += state.ToString() + "\n";
        if (state == plan[0]) {
          planString += "</color>";
        }
      }

      //display the health
      planString += "Health: " + currentHealth + "\n";
      textMeshPro.text = planString;
    }


    //========================================================================= 
    void Start()
    {

        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

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
        plan = new List<PlayerBehaviourState>();

        DefineBehaviours();

        // set x and z position to spawn
        transform.position = new Vector3(
        spawnPoint.transform.position.x, 
        transform.position.y, 
        spawnPoint.transform.position.z);
        
    }

    void Update()
    {

        updateText();
        if (isDead) {
          //disable
          gameObject.SetActive(false);
        }
        // if we are in attack , we dont get a new plan
        if (lastTakenDamageTime != -1.0f) {
          float elapsedTime = Time.time - lastTakenDamageTime;
          if (elapsedTime > takenDamageCooldown) {
            lastTakenDamageTime = -1.0f;
          } else {
            return;
          }
        }

        float elapsedTimeSinceLastPlanRefresh = Time.time - lastPlanRefreshTime;
        bool shouldRefreshPlan = elapsedTimeSinceLastPlanRefresh > planRefreshCoolDown || lastPlanRefreshTime == -1.0f;

        if (plan.Count == 0 && !treasureAtSpawnPoint() && shouldRefreshPlan) {
          lastPlanRefreshTime = Time.time;
          plan = new Planner().getPlan(gameObject);
          // Debug.Log("New plan: " + plan);
        }
        if (plan.Count == 0) { return; }
        var currentBehaviour = plan[0];

        // Debug.Log("Current Behaviour: " + currentBehaviour);

        if (!stateBehaviours[currentBehaviour].precondition() && shouldRefreshPlan) { 
          // if the precondition is not met, clear our plan
          lastPlanRefreshTime = Time.time;
          plan.Clear();
          return; 
        }

        stateBehaviours[currentBehaviour].action();
        if (stateBehaviours[currentBehaviour].postcondition() && shouldRefreshPlan) {
            lastPlanRefreshTime = Time.time;
            plan.RemoveAt(0);
        }

        //line of sight
        // Debug.Log("Has line of sight: " + HasLineOfSight());

        


        
    }
}
