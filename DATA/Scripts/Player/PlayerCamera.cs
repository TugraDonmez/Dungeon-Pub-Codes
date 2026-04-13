using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.2f;
    public float lookAheadDistance = 2f;
    public float returnSpeed = 2f; // Geri çekilme hızı
    public Vector3 offset;

    private Vector3 velocity = Vector3.zero;
    private Vector2 lastTargetPosition;
    private Vector3 currentLookAhead = Vector3.zero;
    private Vector3 targetLookAhead = Vector3.zero;

    void Start()
    {
        if (target != null)
            lastTargetPosition = target.position;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        // Hareket yönü
        Vector2 moveDelta = (Vector2)target.position - lastTargetPosition;

        if (moveDelta.sqrMagnitude > 0.001f)
        {
            // Hareket varsa ileriye bak
            targetLookAhead = new Vector3(moveDelta.normalized.x, moveDelta.normalized.y, 0) * lookAheadDistance;
        }
        else
        {
            // Hareket yoksa geri çekil
            targetLookAhead = Vector3.zero;
        }

        // Yavaşça geri çekilme veya ileriye geçiş
        currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, Time.fixedDeltaTime * returnSpeed);

        lastTargetPosition = target.position;

        // Kameranın gitmesi gereken pozisyon
        Vector3 targetPosition = target.position + offset + currentLookAhead;
        targetPosition.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

}
