using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NPCRelationDisplay : MonoBehaviour
{
    public string npcName;
    public TMP_Text relationText; // Eğer UI Text kullanıyorsan: public Text relationText;

    private RelationshipManager relManager;

    void Start()
    {
        relManager = FindObjectOfType<RelationshipManager>();
        UpdateRelationText();
    }

    void Update()
    {
        // İsteğe bağlı: her frame güncellenmesin dersen bu kısmı silebilirsin
        UpdateRelationText();
    }

    void UpdateRelationText()
    {
        if (relManager == null || relationText == null) return;

        int relation = relManager.GetRelation(npcName);
        relationText.text = $"İlişki: {relation}";
    }

}
