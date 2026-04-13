using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    public ShopProfile shopProfile;
    public DialogueProfile dialogueProfile;
    public List<DialogueProfile> dialogueProfiles;


    public void Interact()
    {
        var relManager = FindObjectOfType<RelationshipManager>();
        int relation = relManager.GetRelation(shopProfile.npcName);

        // Uygun olanı seç
        DialogueProfile selectedProfile = dialogueProfiles
            .FirstOrDefault(p => relation >= p.minRelation && relation <= p.maxRelation);

        if (selectedProfile != null)
        {
            Debug.Log($"[{shopProfile.npcName}] için uygun diyalog profili bulundu: {selectedProfile.name}");
            var dialogueManager = FindObjectOfType<DialogueManager>();
            dialogueManager.StartDialogue(selectedProfile);
        }
        else
        {
            Debug.LogWarning($"[{shopProfile.npcName}] için ilişki seviyesi ({relation}) uygun bir diyalog profili bulunamadı.");

            var dialogueManager = FindObjectOfType<DialogueManager>();
            dialogueManager.ShowMessage("Bu NPC seninle konuşmak istemiyor.");
        }

    }


}
