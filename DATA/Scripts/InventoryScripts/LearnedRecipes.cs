using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cooking/LearnedRecipes")]
public class LearnedRecipes : ScriptableObject
{
    [Header("Known Recipes")]
    public List<CookingRecipe> knownRecipes = new();

    public void LearnRecipe(CookingRecipe recipe)
    {
        if (!knownRecipes.Contains(recipe))
        {
            knownRecipes.Add(recipe);
            Debug.Log($"New recipe learned: {recipe.name}");
        }
    }

    public bool IsRecipeKnown(CookingRecipe recipe)
    {
        return knownRecipes.Contains(recipe);
    }

    public bool IsRecipeKnownByID(string recipeID)
    {
        return knownRecipes.Exists(r => r.recipeID == recipeID);
    }

    public CookingRecipe GetRecipeByID(string recipeID)
    {
        return knownRecipes.Find(r => r.recipeID == recipeID);
    }

    public List<CookingRecipe> GetRecipesByType(CookingType type)
    {
        return knownRecipes.FindAll(r => r.cookingType == type);
    }
}
