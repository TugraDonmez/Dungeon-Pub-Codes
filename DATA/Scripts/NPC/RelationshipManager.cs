using UnityEngine;
using System.Collections.Generic;

public class RelationshipManager : MonoBehaviour
{
    private Dictionary<string, int> npcRelations = new();

    public int GetRelation(string npcId)
    {
        return npcRelations.TryGetValue(npcId, out var value) ? value : 0;
    }

    public void ChangeRelation(string npcId, int amount)
    {
        if (!npcRelations.ContainsKey(npcId))
            npcRelations[npcId] = 0;

        npcRelations[npcId] = Mathf.Clamp(npcRelations[npcId] + amount, -10, 10);
    }
}
