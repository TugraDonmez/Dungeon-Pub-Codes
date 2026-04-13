using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float stepDistance = 1f;
    [SerializeField] private float waitTimeMin = 1f;
    [SerializeField] private float waitTimeMax = 3f;
    [SerializeField] private float stuckTimeout = 3f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float searchTime = 5f;
    [SerializeField] private float searchRadius = 2f;
    [SerializeField] private LayerMask wallLayerMask = 1;
    [SerializeField] private LayerMask playerLayerMask = 1 << 3;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackDamage = 10f;

    // Components
    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private Enemy enemyHealth; // Reference to Enemy component

    // State Management
    private EnemyState currentState;
    private Vector2 initialPosition;
    private Transform player;
    private Coroutine patrolCoroutine;

    // Tracking System
    private Vector2 lastKnownPlayerPosition;
    private float lastSeenTime;
    private bool hasLastKnownPosition = false;

    // Return to Home System
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private int failedAttempts = 0;

    // Movement
    private Vector2 targetPosition;
    private bool isMoving = false;

    // Combat
    private float lastAttackTime;

    // Knockback system
    private bool isKnockedBack = false;
    private bool isStunned = false;

    // 8 directional movement vectors
    private readonly Vector2[] directions = {
        Vector2.up,
        Vector2.right,
        Vector2.down,
        Vector2.left,
        new Vector2(1, 1).normalized,
        new Vector2(1, -1).normalized,
        new Vector2(-1, -1).normalized,
        new Vector2(-1, 1).normalized
    };

    private enum EnemyState
    {
        Patrolling,
        Chasing,
        Searching,
        Attacking,
        KnockedBack // New state for knockback
    }

    void Start()
    {
        InitializeComponents();
        InitializeState();
    }

    void Update()
    {
        UpdateStateMachine();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        enemyHealth = GetComponent<Enemy>();
        initialPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (enemyHealth == null)
        {
            Debug.LogError("EnemyAI requires an Enemy component!");
        }
    }

    private void InitializeState()
    {
        currentState = EnemyState.Patrolling;
        StartCoroutine(PatrolBehavior());
    }

    private void UpdateStateMachine()
    {
        // Check knockback state first
        if (enemyHealth != null && enemyHealth.IsKnockedBack && currentState != EnemyState.KnockedBack)
        {
            ChangeState(EnemyState.KnockedBack);
            return;
        }

        // If we're knocked back, don't do anything else
        if (currentState == EnemyState.KnockedBack)
        {
            return;
        }

        // Continuous player detection for all states
        bool canSeePlayer = CanSeePlayer();

        // Update last known position if we can see player
        if (canSeePlayer && player != null)
        {
            lastKnownPlayerPosition = player.position;
            lastSeenTime = Time.time;
            hasLastKnownPosition = true;
        }

        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolling(canSeePlayer);
                break;
            case EnemyState.Chasing:
                HandleChasing(canSeePlayer);
                break;
            case EnemyState.Searching:
                HandleSearching(canSeePlayer);
                break;
            case EnemyState.Attacking:
                HandleAttacking(canSeePlayer);
                break;
        }
    }

    // Methods called by Enemy component
    public void OnKnockbackStarted()
    {
        Debug.Log($"{gameObject.name} received knockback!");
        isKnockedBack = true;
        isStunned = true;

        // Stop all movement coroutines
        StopAllCoroutines();
        isMoving = false;

        ChangeState(EnemyState.KnockedBack);
    }

    public void OnKnockbackFinished()
    {
        Debug.Log($"{gameObject.name} knockback finished, can move again");
        isKnockedBack = false;

        // Resume previous behavior - check what we should be doing
        ResumeBehaviorAfterKnockback();
    }

    public void OnStunFinished()
    {
        Debug.Log($"{gameObject.name} stun finished, can attack again");
        isStunned = false;
    }

    public void OnDeath()
    {
        // Stop all coroutines when enemy dies
        StopAllCoroutines();
    }

    private void ResumeBehaviorAfterKnockback()
    {
        // Check what state we should be in after knockback
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange && !isStunned)
            {
                ChangeState(EnemyState.Attacking);
            }
            else
            {
                ChangeState(EnemyState.Chasing);
            }
        }
        else if (hasLastKnownPosition)
        {
            ChangeState(EnemyState.Searching);
        }
        else
        {
            ChangeState(EnemyState.Patrolling);
        }
    }

    #region Patrolling State
    private void HandlePatrolling(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            Debug.Log($"{gameObject.name} spotted the player!");
            ChangeState(EnemyState.Chasing);
            return;
        }
    }

    private IEnumerator PatrolBehavior()
    {
        while (currentState == EnemyState.Patrolling)
        {
            // Check if we can move normally (not knocked back)
            if (enemyHealth != null && !enemyHealth.CanMoveNormally())
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            float waitTime = Random.Range(waitTimeMin, waitTimeMax);
            yield return new WaitForSeconds(waitTime);

            if (currentState != EnemyState.Patrolling) yield break;

            int attempts = 0;
            bool foundValidPosition = false;

            while (attempts < 8 && !foundValidPosition)
            {
                Vector2 randomDirection = GetRandomPatrolDirection();
                Vector2 newTargetPosition = (Vector2)transform.position + randomDirection * stepDistance;

                if (Vector2.Distance(newTargetPosition, initialPosition) <= patrolRadius &&
                    IsPathClear(transform.position, newTargetPosition) &&
                    IsPositionValid(newTargetPosition))
                {
                    yield return StartCoroutine(MoveToPosition(newTargetPosition));
                    foundValidPosition = true;
                }
                attempts++;
            }

            if (!foundValidPosition)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private Vector2 GetRandomPatrolDirection()
    {
        return directions[Random.Range(0, directions.Length)];
    }
    #endregion

    #region Chasing State
    private void HandleChasing(bool canSeePlayer)
    {
        if (player == null)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }

        if (!canSeePlayer)
        {
            Debug.Log($"{gameObject.name} lost sight of player! Going to search at last known position.");
            if (hasLastKnownPosition)
            {
                ChangeState(EnemyState.Searching);
            }
            else
            {
                ChangeState(EnemyState.Patrolling);
            }
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            Debug.Log($"{gameObject.name} is close enough to attack!");
            ChangeState(EnemyState.Attacking);
            return;
        }

        // Only move if we can move normally
        if (!isMoving && enemyHealth != null && enemyHealth.CanMoveNormally())
        {
            Debug.Log($"{gameObject.name} is moving towards player!");
            MoveTowardsTarget(player.position);
        }
    }
    #endregion

    #region Searching State
    private void HandleSearching(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            Debug.Log($"{gameObject.name} found the player again!");
            ChangeState(EnemyState.Chasing);
            return;
        }

        if (Time.time - lastSeenTime > searchTime)
        {
            Debug.Log($"{gameObject.name} gave up searching. Returning to original patrol area.");
            hasLastKnownPosition = false;
            StartCoroutine(ReturnToPatrolArea());
            return;
        }

        // Only move if we can move normally
        if (!isMoving && enemyHealth != null && enemyHealth.CanMoveNormally())
        {
            SearchForPlayer();
        }
    }

    private void SearchForPlayer()
    {
        float distanceToLastKnown = Vector2.Distance(transform.position, lastKnownPlayerPosition);

        if (distanceToLastKnown > 0.5f)
        {
            Debug.Log($"{gameObject.name} moving to last known player position");
            MoveTowardsTarget(lastKnownPlayerPosition);
        }
        else
        {
            Debug.Log($"{gameObject.name} searching around last known position");
            Vector2 randomSearchDirection = directions[Random.Range(0, directions.Length)];
            Vector2 searchTarget = lastKnownPlayerPosition + randomSearchDirection * searchRadius;

            if (IsPathClear(transform.position, searchTarget) && IsPositionValid(searchTarget))
            {
                StartCoroutine(MoveToPosition(searchTarget));
            }
            else
            {
                StartCoroutine(WaitAndRetry());
            }
        }
    }

    private IEnumerator ReturnToPatrolArea()
    {
        Debug.Log($"{gameObject.name} is returning to original patrol area");

        lastPosition = transform.position;
        stuckTimer = 0f;
        failedAttempts = 0;

        float distanceToHome = Vector2.Distance(transform.position, initialPosition);

        if (distanceToHome <= patrolRadius)
        {
            Debug.Log($"{gameObject.name} reached patrol area and resuming patrol");
            ChangeState(EnemyState.Patrolling);
            yield break;
        }

        yield return StartCoroutine(TryReturnStrategies());
    }

    private IEnumerator TryReturnStrategies()
    {
        yield return StartCoroutine(TryDirectReturn());
        if (currentState != EnemyState.Searching) yield break;

        yield return StartCoroutine(TryWaypointReturn());
        if (currentState != EnemyState.Searching) yield break;

        yield return StartCoroutine(EmergencyReturn());
    }

    private IEnumerator TryDirectReturn()
    {
        Debug.Log($"{gameObject.name} trying direct return strategy");
        float strategyStartTime = Time.time;
        float maxStrategyTime = 5f;

        while (Vector2.Distance(transform.position, initialPosition) > patrolRadius &&
               currentState == EnemyState.Searching &&
               Time.time - strategyStartTime < maxStrategyTime)
        {
            // Check if we can move normally
            if (enemyHealth != null && !enemyHealth.CanMoveNormally())
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if (CanSeePlayer())
            {
                Debug.Log($"{gameObject.name} spotted player while returning! Canceling return and chasing.");
                ChangeState(EnemyState.Chasing);
                yield break;
            }

            if (IsStuck())
            {
                Debug.Log($"{gameObject.name} is stuck, abandoning direct return strategy");
                yield break;
            }

            bool moved = TryMoveTowardsHome();

            if (!moved)
            {
                failedAttempts++;
                if (failedAttempts >= 3)
                {
                    Debug.Log($"{gameObject.name} too many failed attempts, abandoning direct strategy");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                failedAttempts = 0;
            }

            yield return new WaitForSeconds(0.2f);
        }

        if (Vector2.Distance(transform.position, initialPosition) <= patrolRadius)
        {
            Debug.Log($"{gameObject.name} successfully returned home via direct strategy!");
            ChangeState(EnemyState.Patrolling);
        }
    }

    private IEnumerator TryWaypointReturn()
    {
        Debug.Log($"{gameObject.name} trying waypoint return strategy");

        Vector2[] waypoints = GenerateReturnWaypoints();

        foreach (Vector2 waypoint in waypoints)
        {
            if (currentState != EnemyState.Searching) yield break;

            if (CanSeePlayer())
            {
                Debug.Log($"{gameObject.name} spotted player while returning! Canceling return and chasing.");
                ChangeState(EnemyState.Chasing);
                yield break;
            }

            Debug.Log($"{gameObject.name} moving to waypoint: {waypoint}");

            yield return StartCoroutine(MoveToWaypoint(waypoint));

            if (Vector2.Distance(transform.position, initialPosition) <= patrolRadius)
            {
                Debug.Log($"{gameObject.name} reached home via waypoint strategy!");
                ChangeState(EnemyState.Patrolling);
                yield break;
            }
        }
    }

    private Vector2[] GenerateReturnWaypoints()
    {
        Vector2 currentPos = transform.position;
        List<Vector2> waypoints = new List<Vector2>();

        float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        foreach (float angle in angles)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 waypoint = initialPosition + direction * (patrolRadius * 0.8f);

            if (Vector2.Distance(currentPos, waypoint) > 1f && IsPositionValid(waypoint))
            {
                waypoints.Add(waypoint);
            }
        }

        waypoints.Sort((a, b) => Vector2.Distance(currentPos, a).CompareTo(Vector2.Distance(currentPos, b)));

        return waypoints.ToArray();
    }

    private IEnumerator MoveToWaypoint(Vector2 waypoint)
    {
        float waypointStartTime = Time.time;
        float maxWaypointTime = 3f;

        while (Vector2.Distance(transform.position, waypoint) > 0.5f &&
               currentState == EnemyState.Searching &&
               Time.time - waypointStartTime < maxWaypointTime)
        {
            // Check if we can move normally
            if (enemyHealth != null && !enemyHealth.CanMoveNormally())
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            Vector2 direction = (waypoint - (Vector2)transform.position).normalized;
            Vector2 targetPos = (Vector2)transform.position + direction * stepDistance;

            if (IsPathClear(transform.position, targetPos) && IsPositionValid(targetPos))
            {
                yield return StartCoroutine(MoveToPosition(targetPos));
            }
            else
            {
                Vector2 altDir = new Vector2(-direction.y, direction.x);
                Vector2 altPos = (Vector2)transform.position + altDir * stepDistance;

                if (IsPathClear(transform.position, altPos) && IsPositionValid(altPos))
                {
                    yield return StartCoroutine(MoveToPosition(altPos));
                }
                else
                {
                    yield return new WaitForSeconds(0.3f);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator EmergencyReturn()
    {
        Debug.Log($"{gameObject.name} using emergency return - teleporting to safe position");

        Vector2 safePosition = FindSafePositionInPatrolArea();

        if (safePosition != Vector2.zero)
        {
            transform.position = safePosition;
            Debug.Log($"{gameObject.name} emergency teleported to patrol area");
            ChangeState(EnemyState.Patrolling);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} could not find safe return position! Staying in search mode.");
        }

        yield break;
    }

    private Vector2 FindSafePositionInPatrolArea()
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f * Mathf.Deg2Rad;
            float radius = patrolRadius * 0.5f;
            Vector2 testPos = initialPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            if (IsPositionValid(testPos))
            {
                return testPos;
            }
        }

        return Vector2.zero;
    }

    private bool TryMoveTowardsHome()
    {
        Vector2 directionToHome = (initialPosition - (Vector2)transform.position).normalized;
        Vector2 targetPos = (Vector2)transform.position + directionToHome * stepDistance;

        if (IsPathClear(transform.position, targetPos) && IsPositionValid(targetPos))
        {
            StartCoroutine(MoveToPosition(targetPos));
            return true;
        }

        Vector2[] alternatives = {
            new Vector2(-directionToHome.y, directionToHome.x),
            new Vector2(directionToHome.y, -directionToHome.x),
        };

        foreach (Vector2 altDir in alternatives)
        {
            Vector2 altPos = (Vector2)transform.position + altDir * stepDistance;
            if (IsPathClear(transform.position, altPos) && IsPositionValid(altPos))
            {
                StartCoroutine(MoveToPosition(altPos));
                return true;
            }
        }

        return false;
    }

    private bool IsStuck()
    {
        float distanceMoved = Vector2.Distance(transform.position, lastPosition);

        if (distanceMoved < 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                return true;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        return false;
    }
    #endregion

    #region Movement System
    private void MoveTowardsTarget(Vector2 targetPosition)
    {
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;

        float chaseStepDistance = stepDistance * 0.8f;
        Vector2 targetPos = (Vector2)transform.position + directionToTarget * chaseStepDistance;

        if (IsPathClear(transform.position, targetPos) && IsPositionValid(targetPos))
        {
            Debug.Log($"{gameObject.name} moving directly towards target");
            StartCoroutine(MoveToPosition(targetPos));
        }
        else
        {
            Debug.Log($"{gameObject.name} path blocked, finding alternative");
            Vector2 alternativePath = FindAlternativePath(directionToTarget);
            if (alternativePath != Vector2.zero)
            {
                Vector2 altTargetPos = (Vector2)transform.position + alternativePath;
                if (IsPositionValid(altTargetPos))
                {
                    StartCoroutine(MoveToPosition(altTargetPos));
                }
                else
                {
                    Debug.Log($"{gameObject.name} alternative path also blocked");
                    StartCoroutine(WaitAndRetry());
                }
            }
            else
            {
                Debug.Log($"{gameObject.name} no alternative path found");
                StartCoroutine(WaitAndRetry());
            }
        }
    }

    private IEnumerator WaitAndRetry()
    {
        isMoving = true;
        yield return new WaitForSeconds(0.3f);
        isMoving = false;
    }

    private Vector2 FindAlternativePath(Vector2 blockedDirection)
    {
        Vector2[] alternatives = {
            new Vector2(-blockedDirection.y, blockedDirection.x),
            new Vector2(blockedDirection.y, -blockedDirection.x),
        };

        foreach (Vector2 altDir in alternatives)
        {
            Vector2 testPos = (Vector2)transform.position + altDir.normalized * stepDistance;
            if (IsPathClear(transform.position, testPos) && IsPositionValid(testPos))
            {
                return altDir.normalized * stepDistance;
            }
        }

        return Vector2.zero;
    }

    private IEnumerator MoveToPosition(Vector2 targetPos)
    {
        // Don't move if knocked back
        if (enemyHealth != null && !enemyHealth.CanMoveNormally())
        {
            isMoving = false;
            yield break;
        }

        isMoving = true;
        targetPosition = targetPos;

        Vector2 startPos = transform.position;
        float journey = 0f;
        float journeyTime = Vector2.Distance(startPos, targetPos) / moveSpeed;

        Debug.Log($"{gameObject.name} starting movement from {startPos} to {targetPos}");

        while (journey <= journeyTime)
        {
            // Check if we got knocked back during movement
            if (enemyHealth != null && !enemyHealth.CanMoveNormally())
            {
                Debug.Log($"{gameObject.name} movement interrupted by knockback");
                break;
            }

            journey += Time.deltaTime;
            float fractionOfJourney = journey / journeyTime;

            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, fractionOfJourney);
            rb.MovePosition(currentPos);

            yield return null;

            if (currentState == EnemyState.Attacking)
            {
                Debug.Log($"{gameObject.name} movement interrupted by attack state");
                break;
            }
        }

        Debug.Log($"{gameObject.name} finished movement");
        isMoving = false;
    }
    #endregion

    #region Attacking State
    private void HandleAttacking(bool canSeePlayer)
    {
        if (player == null)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // If player moved away, chase again
        if (distanceToPlayer > attackRange)
        {
            if (canSeePlayer)
            {
                ChangeState(EnemyState.Chasing);
            }
            else if (hasLastKnownPosition)
            {
                ChangeState(EnemyState.Searching);
            }
            else
            {
                ChangeState(EnemyState.Patrolling);
            }
            return;
        }

        // If player is not visible, search or chase
        if (!canSeePlayer)
        {
            if (hasLastKnownPosition)
            {
                ChangeState(EnemyState.Searching);
            }
            else
            {
                ChangeState(EnemyState.Chasing);
            }
            return;
        }

        // Attack if cooldown is ready and not stunned
        if (Time.time - lastAttackTime >= attackCooldown &&
            (enemyHealth == null || enemyHealth.CanAttack()) && player.GetComponent<PlayerMovement>().isDashing == false)
        {
            Attack();
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;

        // Stop any movement during attack
        if (isMoving)
        {
            StopAllCoroutines();
            isMoving = false;
        }

        // Attack animation or effect here
        Debug.Log($"{gameObject.name} attacks player for {attackDamage} damage!");

        // Try to damage player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }
    #endregion

    #region Detection System
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange)
        {
            return false;
        }

        // Raycast to check for walls between enemy and player
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, wallLayerMask);

        // Debug line to see the raycast
        Color rayColor = hit.collider == null ? Color.green : Color.red;
        Debug.DrawRay(transform.position, directionToPlayer * distanceToPlayer, rayColor);

        bool canSee = hit.collider == null;

        if (canSee && currentState == EnemyState.Patrolling)
        {
            Debug.Log($"{gameObject.name} can see player at distance {distanceToPlayer:F2}");
        }

        return canSee;
    }

    private bool IsPathClear(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        float distance = Vector2.Distance(from, to);

        // Use a slightly smaller distance to avoid edge cases
        RaycastHit2D hit = Physics2D.Raycast(from, direction, distance - 0.1f, wallLayerMask);

        bool pathClear = hit.collider == null;
        Debug.DrawRay(from, direction * (distance - 0.1f), pathClear ? Color.blue : Color.yellow, 0.1f);

        return pathClear;
    }

    private bool IsPositionValid(Vector2 position)
    {
        // Check if the position itself is not inside a wall
        Collider2D wallCollider = Physics2D.OverlapCircle(position, enemyCollider.bounds.size.x * 0.4f, wallLayerMask);
        return wallCollider == null;
    }
    #endregion

    #region State Management
    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        EnemyState previousState = currentState;

        // Exit current state - stop any ongoing coroutines
        switch (currentState)
        {
            case EnemyState.Patrolling:
                if (patrolCoroutine != null)
                {
                    StopCoroutine(patrolCoroutine);
                    patrolCoroutine = null;
                }
                break;
            case EnemyState.Searching:
                // Stop any return coroutine if it's running
                StopAllCoroutines();
                isMoving = false;
                break;
            case EnemyState.KnockedBack:
                // Don't stop knockback coroutine, it handles itself
                break;
        }

        currentState = newState;

        // Enter new state
        switch (newState)
        {
            case EnemyState.Patrolling:
                patrolCoroutine = StartCoroutine(PatrolBehavior());
                break;
            case EnemyState.Chasing:
                Debug.Log($"{gameObject.name} started chasing player!");
                break;
            case EnemyState.Searching:
                Debug.Log($"{gameObject.name} started searching for player at last known position!");
                break;
            case EnemyState.Attacking:
                Debug.Log($"{gameObject.name} is attacking!");
                break;
            case EnemyState.KnockedBack:
                Debug.Log($"{gameObject.name} is knocked back!");
                break;
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw patrol area with different color if returning
        Gizmos.color = currentState == EnemyState.Searching && Time.time - lastSeenTime > searchTime ? Color.cyan : Color.blue;
        Vector2 center = Application.isPlaying ? initialPosition : (Vector2)transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);

        // Draw return path if returning to patrol area
        if (Application.isPlaying && currentState == EnemyState.Searching && Time.time - lastSeenTime > searchTime)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, initialPosition);
        }

        // Draw line to player if detected
        if (Application.isPlaying && CanSeePlayer() && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Draw last known position and search area
        if (Application.isPlaying && hasLastKnownPosition)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.3f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, searchRadius);
        }

        // Draw knockback state indicator
        if (Application.isPlaying && currentState == EnemyState.KnockedBack)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, 0.8f);
        }
    }
    #endregion
}