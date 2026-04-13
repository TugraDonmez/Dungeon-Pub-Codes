using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSorter : MonoBehaviour
{
    private SpriteRenderer sr;
    private Transform player;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Player objenin altındaysa (y daha küçük) → player önde
        if (player.position.y < transform.position.y)
        {
            sr.sortingOrder = -1; // obje arkada
        }
        else
        {
            sr.sortingOrder = 1; // obje önde
        }
    }
}
