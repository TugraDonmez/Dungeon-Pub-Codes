using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingSystem : MonoBehaviour
{
    [Header("Cooking Settings")]
    public List<CookingRecipe> recipes = new List<CookingRecipe>();

    [Header("Allowed Items")]
    public List<string> allowedFryingIngredients = new List<string>();
    public List<string> allowedLiquids = new List<string>();
    public List<string> allowedBakingIngredients = new List<string>();

    private static CookingSystem instance;
    public static CookingSystem Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CookingSystem>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public bool CanPlaceInFryingSlot(string itemID)
    {
        return allowedFryingIngredients.Contains(itemID);
    }

    public bool CanPlaceInLiquidSlot(string itemID)
    {
        return allowedLiquids.Contains(itemID);
    }

    public bool CanPlaceInBakingSlot(string itemID)
    {
        return allowedBakingIngredients.Contains(itemID);
    }


    public CookingRecipe GetRecipe(CookingType type, string ingredientID, string liquidID = "")
    {
        foreach (var recipe in recipes)
        {
            if (recipe.cookingType == type && recipe.ingredientID == ingredientID)
            {
                // Kızartma için sıvı kontrolü
                if (type == CookingType.Frying && recipe.liquidID == liquidID)
                    return recipe;
                // Fırın için sadece malzeme kontrolü
                else if (type == CookingType.Baking)
                    return recipe;
            }
        }
        return null;
    }
}
