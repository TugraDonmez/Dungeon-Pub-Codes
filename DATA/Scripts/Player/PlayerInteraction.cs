// PlayerInteraction.cs - Oyuncunun müşterilerle etkileşimini yöneten script (Güncellenmiş)
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactableLayer = -1; // Hangi layer'larda interactable aranacak
    public Transform interactionTransform; // Interaction için kullanılacak transform referansı

    [Header("UI References")]
    public GameObject interactionPrompt; // "Press E to serve" UI elementi
    public TMPro.TMP_Text interactionText;

    private IInteractable currentInteractable;
    private CustomerWithMovement currentCustomer;
    private InteractableHighlight currentHighlight; // Mevcut vurgulanmış obje

    // Interaction için kullanılacak transform'u döndüren property
    private Transform InteractionTransform
    {
        get
        {
            // Eğer özel bir transform atanmışsa onu kullan, yoksa kendi transform'unu kullan
            return interactionTransform != null ? interactionTransform : transform;
        }
    }

    private void Update()
    {
        FindNearestInteractable();
        HandleInteractionInput();
    }

    private void FindNearestInteractable()
    {
        IInteractable nearestInteractable = null;
        CustomerWithMovement nearestCustomer = null;
        InteractableHighlight nearestHighlight = null;
        float nearestDistance = interactionRange;

        Vector3 interactionPosition = InteractionTransform.position;

        // Collider2D tabanlı arama
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(interactionPosition, interactionRange, interactableLayer);

        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) continue; // Kendini atla

            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float distance = Vector2.Distance(interactionPosition, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                    nearestCustomer = collider.GetComponent<CustomerWithMovement>();
                    nearestHighlight = collider.GetComponent<InteractableHighlight>();
                }
            }
        }

        // Eğer hiç interactable bulunamazsa, manuel olarak müşterileri kontrol et
        if (nearestInteractable == null)
        {
            CustomerWithMovement[] allCustomers = FindObjectsOfType<CustomerWithMovement>();
            foreach (var customer in allCustomers)
            {
                if (customer.IsInInteractionRange(InteractionTransform))
                {
                    float distance = Vector2.Distance(interactionPosition, customer.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestInteractable = customer;
                        nearestCustomer = customer;
                        nearestHighlight = customer.GetComponent<InteractableHighlight>();
                    }
                }
            }
        }

        UpdateCurrentInteractable(nearestInteractable, nearestCustomer, nearestHighlight);
    }

    private void UpdateCurrentInteractable(IInteractable newInteractable, CustomerWithMovement newCustomer, InteractableHighlight newHighlight)
    {
        // Önceki highlight'ı kaldır
        if (currentHighlight != null && currentHighlight != newHighlight)
        {
            currentHighlight.SetHighlighted(false);
        }

        // Yeni highlight'ı ayarla
        if (newHighlight != null && newHighlight != currentHighlight)
        {
            newHighlight.SetHighlighted(true);
        }

        if (currentInteractable != newInteractable)
        {
            currentInteractable = newInteractable;
            currentCustomer = newCustomer;
            currentHighlight = newHighlight;
            UpdateInteractionUI();
        }
        else
        {
            currentHighlight = newHighlight;
        }
    }

    private void UpdateInteractionUI()
    {
        bool shouldShowPrompt = (currentInteractable != null);

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(shouldShowPrompt);
        }

        if (interactionText != null && shouldShowPrompt)
        {
            string promptText = GetInteractionPromptText();
            interactionText.text = promptText;
        }
    }

    private string GetInteractionPromptText()
    {
        if (currentCustomer != null)
        {
            var activeHandSlot = HandSlotManager.Instance?.GetActiveHandSlot();

            if (activeHandSlot != null && !activeHandSlot.IsEmpty)
            {
                Item heldItem = activeHandSlot.item;

                if (heldItem != null)
                {
                    // Tarif bul
                    CookingRecipe recipe = FindRecipeByOutputID(heldItem.id);

                    if (recipe != null)
                    {
                        // Doğru sipariş mi?
                        if (currentCustomer.currentOrder?.requestedRecipe == recipe)
                        {
                            return $"Press {interactKey} to serve {recipe.GetDisplayName()} ✓";
                        }
                        else
                        {
                            return $"Press {interactKey} to serve {recipe.GetDisplayName()} (Wrong order!)";
                        }
                    }
                    else
                    {
                        return $"{heldItem.itemName} is not a valid cooked dish!";
                    }
                }
                else
                {
                    return "No item in hand!";
                }
            }
            else
            {
                return "You need a dish to serve!";
            }
        }

        return $"Press {interactKey} to interact";
    }

    private CookingRecipe FindRecipeByOutputID(string itemId)
    {
        // Tarifleri nereden yüklediğine göre burayı değiştirebilirsin
        CookingRecipe[] allRecipes = Resources.LoadAll<CookingRecipe>("Recipes");
        foreach (var recipe in allRecipes)
        {
            if (recipe.outputItemID == itemId)
            {
                return recipe;
            }
        }
        return null;
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            // Etkileşim yaptıktan sonra highlight'ı kaldır
            if (currentHighlight != null)
            {
                currentHighlight.SetHighlighted(false);
            }

            currentInteractable.Interact();

            // Etkileşim sonrası durumu güncelle
            currentInteractable = null;
            currentCustomer = null;
            currentHighlight = null;
            UpdateInteractionUI();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Interaction range'i görselleştir
        Gizmos.color = Color.yellow;
        Vector3 gizmoPosition = InteractionTransform.position;
        Gizmos.DrawWireSphere(gizmoPosition, interactionRange);
    }

    // Public methods for external access
    public bool HasInteractableNearby()
    {
        return currentInteractable != null;
    }

    public CustomerWithMovement GetCurrentCustomer()
    {
        return currentCustomer;
    }

    public InteractableHighlight GetCurrentHighlight()
    {
        return currentHighlight;
    }

    // Interaction transform'unu dışarıdan ayarlamak için method
    public void SetInteractionTransform(Transform newTransform)
    {
        interactionTransform = newTransform;
    }

    // Mevcut interaction transform'unu almak için method
    public Transform GetInteractionTransform()
    {
        return InteractionTransform;
    }
}