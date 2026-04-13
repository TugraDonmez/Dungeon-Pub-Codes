using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro;

public class CustomerMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 540f; // Degrees per second
    public float arrivalDistance = 0.2f;
    public float animationSmoothTime = 0.1f;

    [Header("References")]
    public Animator animator;
    public NavMeshAgent navMeshAgent;
    public TMP_Text npcName;

    [Header("Animation Parameters")]
    public string velocityXParam = "VelocityX";
    public string velocityYParam = "VelocityY";
    public string isWalkingParam = "IsWalking";

    // Private variables
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool hasReachedTarget = false;

    // Animation smoothing
    private float currentVelX = 0f;
    private float currentVelY = 0f;
    private float velXSmoothRef = 0f;
    private float velYSmoothRef = 0f;

    // Events
    public System.Action OnMovementStarted;
    public System.Action OnMovementCompleted;
    public System.Action<Vector3> OnTargetReached;

    private void Awake()
    {
        // NavMeshAgent setup
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();


        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.angularSpeed = rotationSpeed;
            navMeshAgent.stoppingDistance = arrivalDistance;
            navMeshAgent.autoBraking = true;
            navMeshAgent.autoRepath = true;
        }

        // Animator setup
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isMoving && navMeshAgent != null)
        {
            UpdateMovementAnimation();
            CheckIfReachedTarget();
        }
    }

    /// <summary>
    /// Hedef pozisyona hareket etmeye başlar
    /// </summary>
    public bool MoveTo(Vector3 destination)
    {
        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent not found!");
            return false;
        }

        // NavMesh üzerinde geçerli bir pozisyon mu kontrol et
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 1.0f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            navMeshAgent.SetDestination(targetPosition);

            isMoving = true;
            hasReachedTarget = false;

            OnMovementStarted?.Invoke();

            Debug.Log($"Starting movement to {targetPosition}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Destination {destination} is not on NavMesh!");
            return false;
        }
    }

    /// <summary>
    /// En yakın geçerli NavMesh pozisyonuna hareket et
    /// </summary>
    public bool MoveToClosestValid(Vector3 destination)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 5.0f, NavMesh.AllAreas))
        {
            return MoveTo(hit.position);
        }
        return false;
    }

    /// <summary>
    /// Hareketi durdur
    /// </summary>
    public void StopMovement()
    {
        if (navMeshAgent != null)
        {
            navMeshAgent.ResetPath();
        }

        isMoving = false;
        SetAnimationVelocity(0f, 0f);

        if (animator != null)
        {
            animator.SetBool(isWalkingParam, false);
        }
    }

    /// <summary>
    /// Animasyon parametrelerini günceller
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (animator == null || navMeshAgent == null) return;

        // NavMeshAgent'ın hızını al
        Vector3 velocity = navMeshAgent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        // 8 yönlü animasyon için normalize et
        float normalizedX = 0f;
        float normalizedY = 0f;

        if (velocity.magnitude > 0.1f)
        {
            // Dünya koordinatlarında hareket yönünü hesapla
            Vector3 worldMoveDirection = velocity.normalized;

            // 8 yönlü sisteme dönüştür (-1 ile 1 arası)
            normalizedX = worldMoveDirection.x;
            normalizedY = worldMoveDirection.z; // Unity'de Z forward'dır

            // Animasyonun smooth olması için değerleri yumuşat
            currentVelX = Mathf.SmoothDamp(currentVelX, normalizedX, ref velXSmoothRef, animationSmoothTime);
            currentVelY = Mathf.SmoothDamp(currentVelY, normalizedY, ref velYSmoothRef, animationSmoothTime);
        }
        else
        {
            // Duruyorsa animasyonu sıfıra getir
            currentVelX = Mathf.SmoothDamp(currentVelX, 0f, ref velXSmoothRef, animationSmoothTime);
            currentVelY = Mathf.SmoothDamp(currentVelY, 0f, ref velYSmoothRef, animationSmoothTime);
        }

        SetAnimationVelocity(currentVelX, currentVelY);

        // Yürüyor mu kontrolü
        bool walking = velocity.magnitude > 0.1f;
        animator.SetBool(isWalkingParam, walking);
    }

    /// <summary>
    /// Animator parametrelerini ayarlar
    /// </summary>
    private void SetAnimationVelocity(float x, float y)
    {
        if (animator != null)
        {
            animator.SetFloat(velocityXParam, x);
            animator.SetFloat(velocityYParam, y);
        }
    }

    /// <summary>
    /// Hedefe varıp varmadığını kontrol eder
    /// </summary>
    private void CheckIfReachedTarget()
    {
        if (hasReachedTarget) return;

        // NavMeshAgent hedefine vardı mı?
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < arrivalDistance)
        {
            hasReachedTarget = true;
            isMoving = false;

            // Animasyonu durdur
            SetAnimationVelocity(0f, 0f);
            if (animator != null)
            {
                animator.SetBool(isWalkingParam, false);
            }

            OnMovementCompleted?.Invoke();
            OnTargetReached?.Invoke(targetPosition);

            Debug.Log("Reached target destination");
        }
    }

    /// <summary>
    /// Mevcut hedefe olan mesafe
    /// </summary>
    public float DistanceToTarget()
    {
        if (navMeshAgent != null && isMoving)
            return navMeshAgent.remainingDistance;
        return 0f;
    }

    /// <summary>
    /// Şu anda hareket ediyor mu?
    /// </summary>
    public bool IsMoving()
    {
        return isMoving && navMeshAgent != null && navMeshAgent.velocity.magnitude > 0.1f;
    }

    /// <summary>
    /// Path bulunabilir mi?
    /// </summary>
    public bool CanReachDestination(Vector3 destination)
    {
        if (navMeshAgent == null) return false;

        NavMeshPath path = new NavMeshPath();
        return navMeshAgent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Hareket hızını değiştir
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (navMeshAgent != null)
            navMeshAgent.speed = moveSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        if (navMeshAgent != null && navMeshAgent.hasPath)
        {
            // Path'i görselleştir
            Vector3[] pathCorners = navMeshAgent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pathCorners[i], 0.1f);
            }
        }

        // Hedef pozisyonu göster
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, arrivalDistance);
        }
    }
}
