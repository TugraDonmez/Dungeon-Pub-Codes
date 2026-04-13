using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeID;
    public string recipeName;

    [Header("Output")]
    public string outputItemID;
    public int outputAmount = 1;

    [Header("Recipe Type")]
    public bool usePattern = true; // Pattern kullanılsın mı?

    [Header("Recipe Pattern (Sadece usePattern=true ise)")]
    [Tooltip("3x3 grid pattern. Use item IDs or leave empty for empty slots")]
    public CraftingPattern pattern;

    [Header("Shapeless Recipe (Sadece usePattern=false ise)")]
    [Tooltip("Gerekli malzemeler - sıra önemli değil")]
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
}

[System.Serializable]
public class CraftingIngredient
{
    public string itemID;
    public int amount = 1;
}

[System.Serializable]
public class CraftingPattern
{
    [Header("Row 1 (Y=0)")]
    public string slot00 = "";  // [0,0]
    public string slot10 = "";  // [1,0]
    public string slot20 = "";  // [2,0]

    [Header("Row 2 (Y=1)")]
    public string slot01 = "";  // [0,1]
    public string slot11 = "";  // [1,1]
    public string slot21 = "";  // [2,1]

    [Header("Row 3 (Y=2)")]
    public string slot02 = "";  // [0,2]
    public string slot12 = "";  // [1,2]
    public string slot22 = "";  // [2,2]

    public string GetSlot(int x, int y)
    {
        // X = sütun (0,1,2), Y = satır (0,1,2)
        switch (y * 3 + x) // Y*3+X formatında indeksleme
        {
            case 0: return slot00; // [0,0]
            case 1: return slot10; // [1,0]
            case 2: return slot20; // [2,0]
            case 3: return slot01; // [0,1]
            case 4: return slot11; // [1,1]
            case 5: return slot21; // [2,1]
            case 6: return slot02; // [0,2]
            case 7: return slot12; // [1,2]
            case 8: return slot22; // [2,2]
            default: return "";
        }
    }
}