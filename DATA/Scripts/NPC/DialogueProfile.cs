using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "NPC/Dialogue")]
public class DialogueProfile : ScriptableObject
{
    public string npcId;

    [Header("Minimum İlişki Seviyesi")]
    public int minRelation = -10;

    [Header("Maksimum İlişki Seviyesi")]
    public int maxRelation = 10;

    public List<DialogueNode> dialogueOptions;
}

[System.Serializable]
public class DialogueNode
{
    [TextArea] public string playerLine;
    [TextArea] public string npcResponse;
    public int relationChangeAmount;
    public DialogueActionType actionType;

    public ShopProfile relatedShop;

    public List<DialogueNode> nextNodes;
}

public enum DialogueActionType
{
    None,
    OpenShop,
    GiveQuest,
    IncreaseRelation,
    DecreaseRelation,
    Leave,
}
