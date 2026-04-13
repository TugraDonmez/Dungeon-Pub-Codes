using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcText;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    private ShopUI shopUI;
    private RelationshipManager relationshipManager;
    private DialogueProfile currentProfile;

    public TMP_Text relationText;

    private void Awake()
    {
        shopUI = FindObjectOfType<ShopUI>();
        relationshipManager = FindObjectOfType<RelationshipManager>();
    }

    public void StartDialogue(DialogueProfile profile)
    {
        currentProfile = profile;
        dialoguePanel.SetActive(true);
        npcText.text = "Ne yapmak istersin?";
        LoadDialogueNodes(profile.dialogueOptions);
    }

    private void LoadDialogueNodes(List<DialogueNode> nodes)
    {
        ClearChoices();

        foreach (var node in nodes)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            buttonText.text = node.playerLine;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                npcText.text = node.npcResponse;
                HandleDialogueAction(node, currentProfile);

                // Eğer Leave seçeneği ise, hiçbir şey yükleme
                if (node.actionType == DialogueActionType.Leave)
                    return;

                if (node.nextNodes != null && node.nextNodes.Count > 0)
                {
                    LoadDialogueNodes(node.nextNodes);
                }
                else
                {
                    ClearChoices();
                }
            });

        }
    }

    public void ShowMessage(string message)
    {
        dialoguePanel.SetActive(true);
        npcText.text = message;
        ClearChoices();
    }

    private void ClearChoices()
    {
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void HandleDialogueAction(DialogueNode node, DialogueProfile profile)
    {
        switch (node.actionType)
        {
            case DialogueActionType.OpenShop:
                ShopProfile shop = FindObjectOfType<NPCInteractable>().shopProfile;
                dialoguePanel.SetActive(false);
                if (shop != null)
                    shopUI.OpenShop(shop);
                break;

            case DialogueActionType.IncreaseRelation:
                relationshipManager.ChangeRelation(profile.npcId, node.relationChangeAmount);
                relationText.text = "İlişki Seviyesi: " + relationshipManager.GetRelation(profile.npcId);
                break;
            case DialogueActionType.DecreaseRelation:
                relationshipManager.ChangeRelation(profile.npcId, node.relationChangeAmount);
                relationText.text = "İlişki Seviyesi: " + relationshipManager.GetRelation(profile.npcId);
                break;
            case DialogueActionType.Leave:
                dialoguePanel.SetActive(false);
                break;
            case DialogueActionType.GiveQuest:
                Debug.Log("Görev verilecek (daha sonra eklenecek)");
                break;

            case DialogueActionType.None:
            default:
                break;

        }
    }
}
