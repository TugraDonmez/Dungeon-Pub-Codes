// CookingRecipe.cs - Mevcut yapınızı koruyarak
using UnityEngine;

[CreateAssetMenu(menuName = "Cooking/Recipe")]
public class CookingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeID;
    public CookingType cookingType;

    [Header("Ingredients")]
    public string ingredientID;
    public string liquidID; // Sadece kızartma için, fırın için boş bırakın

    [Header("Output")]
    public string outputItemID;
    public float cookingTime = 5f;

    [Header("Visual & Info")]
    public string recipeName;
    public Sprite recipeIcon;
    public string description;

    // Müşteri sistemi için gerekli metodlar
    public bool HasIngredient(string ingredientId)
    {
        return ingredientID == ingredientId;
    }

    public bool HasLiquid(string liquidId)
    {
        return !string.IsNullOrEmpty(liquidID) && liquidID == liquidId;
    }

    public bool RequiresLiquid()
    {
        return !string.IsNullOrEmpty(liquidID);
    }

    // UI'da gösterilecek isim
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(recipeName))
            return recipeName;
        return name; // ScriptableObject'in ismi
    }
    public bool MatchesOutputItem(Item item)
    {
        return item != null && outputItemID.Equals(item.id, System.StringComparison.OrdinalIgnoreCase);
    }
}


public enum CookingType
{
    Frying,    // Kızartma
    Baking     // Fırın
}
