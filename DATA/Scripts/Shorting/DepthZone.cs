using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Özel objeler için depth zone sistemi
/// </summary>
public class DepthZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private int frontSortingOrder = 50; // Önde olacak karakterler için
    [SerializeField] private int backSortingOrder = -50; // Arkada olacak karakterler için
    [SerializeField] private LayerMask affectedLayers = -1; // Hangi layerlardaki objeler etkilenir

    [Header("Zone Areas")]
    [SerializeField] private Transform frontArea; // Ön bölge
    [SerializeField] private Transform backArea; // Arka bölge

    private HashSet<CharacterDepthSorting> charactersInFront = new HashSet<CharacterDepthSorting>();
    private HashSet<CharacterDepthSorting> charactersInBack = new HashSet<CharacterDepthSorting>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer))
        {
            CharacterDepthSorting character = other.GetComponent<CharacterDepthSorting>();
            if (character != null)
            {
                UpdateCharacterZone(character);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer))
        {
            CharacterDepthSorting character = other.GetComponent<CharacterDepthSorting>();
            if (character != null)
            {
                UpdateCharacterZone(character);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer))
        {
            CharacterDepthSorting character = other.GetComponent<CharacterDepthSorting>();
            if (character != null)
            {
                RemoveCharacterFromZones(character);
            }
        }
    }

    private void UpdateCharacterZone(CharacterDepthSorting character)
    {
        Vector2 characterPos = character.GetSortingPosition();
        Vector2 zoneCenter = transform.position;

        // Karakterin zona göre pozisyonunu belirle
        if (characterPos.y > zoneCenter.y)
        {
            // Karakter objenin arkasında
            if (!charactersInBack.Contains(character))
            {
                charactersInFront.Remove(character);
                charactersInBack.Add(character);
                character.SetTemporarySortingOrder(backSortingOrder, 0.1f);
            }
        }
        else
        {
            // Karakter objenin önünde
            if (!charactersInFront.Contains(character))
            {
                charactersInBack.Remove(character);
                charactersInFront.Add(character);
                character.SetTemporarySortingOrder(frontSortingOrder, 0.1f);
            }
        }
    }

    private void RemoveCharacterFromZones(CharacterDepthSorting character)
    {
        charactersInFront.Remove(character);
        charactersInBack.Remove(character);
    }

    private bool IsInLayerMask(int layer)
    {
        return (affectedLayers.value & (1 << layer)) != 0;
    }
}