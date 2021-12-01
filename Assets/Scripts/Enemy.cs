using System;

using UnityEngine;

public class Enemy : Character
{
    #region Public Vars
    public enum State
    {
        /// <summary>
        /// When in this state the enemy searches for the player
        /// </summary>
        Searching,

        /// <summary>
        /// When in this state the enemy moves to the wait range and then waits for less then two enemies to be attacking
        /// </summary>
        Waiting,

        /// <summary>
        /// When in this state the enemy attacks the player every few seconds 
        /// </summary>
        Attacking
    }

    /// <summary>
    /// Gets the Enemy's current state. Used for AI control
    /// </summary>
    public State CurrentState { get; private set; }
    #endregion

    #region Privite Vars

    private Player player;

    /// <summary>
    /// Reference to the other enemy that this enemy instance will collide with
    /// </summary>
    private Collider otherEnemy;

    /// <summary>
    /// Time until the enemy is allowed to attack again 
    /// </summary>
    private float nextAttackTime;

    /// <summary>
    /// decides when to stop chasing player
    /// </summary>
    private bool stopMoving;

    /// <summary>
    /// checks if the enemy stop trigger has been triggered 
    /// </summary>
    private bool triggered;

    /// <summary>
    /// Trigger for updating the attack count
    /// </summary>
    private bool updateAttackCount;

    #endregion

    #region Serialized Fields
    

    #region AI Vars
    [Header("AI Settings")]
    [SerializeField] private float searchRange;
    [SerializeField] private float attackWaitRange;
    [SerializeField] private float attackRange;

    [Tooltip("How often the enemy will attack")]
    [SerializeField] private float attackRate;

    #endregion
    #endregion


    public override void SingleTargetAttack()
    {
        base.SingleTargetAttack();

        if (Physics.Raycast(Ray, out HitData, attackRange, target))
        {
            // overrides the SingleTargetAttack method to damage the player
            Player _player = HitData.transform.gameObject.GetComponent<Player>();
            _player.DecreaseHealth(damage);
        }
    }


    private void Awake()
    {
        player = FindObjectOfType<Player>();
        healthBar = GetComponentInChildren<HealthBar>();
    }

    private void Start()
    {
        // sets the health bars max value to health value set in inspector
        healthBar.SetMaxHealthUI(health);
        healthBar.gameObject.SetActive(false);
    }

    // Update is called once per frame
    private void Update()
    {
        if (GetHealth() <= 0)
        {
            // cleanup when enemy dies
            SetHealth(0);
            GameManager.Instance.EnemiesAttacking -= 1;
            Destroy(gameObject);
        }

        if (triggered && !otherEnemy)
        {
            stopMoving = false;
        }

        // Handles the different states for the AI
        switch (CurrentState)
        {
            default:
            case State.Searching:
                SearchForPlayer();
                break;
            case State.Waiting:
                MoveToWaitRange();
                break;
            case State.Attacking:
                AttackPlayer();
                break;
        }

    }


    #region AI

    /*
     AI Steps:
    1. Search for player in a set search range. if player is found then proceed to step 2
    2. Move to attack wait range, if there are less then 2 enemies currently attacking then proceed to step 3
    3. Move within attack distance and start attacking every few seconds
     */

    // Draws Debug spheres in the scene view when an enemy is selected. This helps visualize the ranges for the different states
    private void OnDrawGizmosSelected()
    {
        // Search range sphere
        Gizmos.DrawWireSphere(transform.position, searchRange);

        // wait range sphere
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackWaitRange);

        // attack range sphere
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void SearchForPlayer()
    {
        stopMoving = false;

        // searches for the player in the search range
        if (Vector3.Distance(transform.position, player.transform.position) < searchRange)
        {
            CurrentState = State.Waiting;
        }
    }

    private void MoveToWaitRange()
    {

        float step = movementSpeed * Time.deltaTime;

        Vector3 playerPos = player.transform.position;
        Vector3 enemyPos = transform.position;

        Vector3 waitPos = new Vector3(playerPos.x + attackWaitRange, enemyPos.y, enemyPos.z);

        if (!stopMoving)
        {
            // moves the enemy to the wait range
            transform.position = Vector3.MoveTowards(enemyPos, waitPos, step);
        }

        // Sets state back to searching if player goes out of range
        if (Vector3.Distance(transform.position, player.transform.position) > searchRange)
        {
            CurrentState = State.Searching;
        }

        // once the wait position is reached the enemy stops moving
        if (Vector3.Distance(transform.position, waitPos) < .1f)
        {
            
            stopMoving = true;

            // if less then 2 enemies are attacking the player then the enemy starts the attack state
            if (GameManager.Instance.EnemiesAttacking < 2)
            {
                updateAttackCount = true;
                stopMoving = false;
                CurrentState = State.Attacking;
            }
        }
    }

    private void AttackPlayer()
    {
        
        float step = movementSpeed * Time.deltaTime;

        Vector3 playerPos = player.transform.position;
        Vector3 enemyPos = transform.position;
        Vector3 attackPos = new Vector3(playerPos.x + attackRange, enemyPos.y, enemyPos.z);

        // Moves to the attack range
        if (!stopMoving)
        {
            transform.position = Vector3.MoveTowards(enemyPos, attackPos, step);
        }


        // Sets state back to searching if player goes out of range
        if (Vector3.Distance(transform.position, player.transform.position) > searchRange)
        {
            CurrentState = State.Searching;
        }


        // updates the number of enemies attacking the player
        if (updateAttackCount)
        {
            GameManager.Instance.EnemiesAttacking += 1;
            updateAttackCount = false;
        }

        // Adds a delay to how often to attack the player
        if (Time.time > nextAttackTime)
        {
            SingleTargetAttack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StopTrigger"))
        {
            // setup for custom OnTriggerExit logic. Normal OnTriggerExit does not work when an object is destroyed 
            triggered = true;
            otherEnemy = other;

            stopMoving = true;
        }
    }

    // Trigger exit logic for when the enemy is not destroyed but still leaves the trigger
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StopTrigger"))
        {
            triggered = false;
            otherEnemy = null;
        }
    }

    #endregion

}
