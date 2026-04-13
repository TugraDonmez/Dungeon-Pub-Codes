using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // Y koordinatına göre sıralama
        // Çarpanı artırıp azaltabilirsin (1000 gibi büyük yaparsan daha hassas olur)
        spriteRenderer.sortingOrder = -(int)(transform.position.y * 100);
    }
}
