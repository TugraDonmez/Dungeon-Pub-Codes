using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    [Header("Crafting Settings")]
    public List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Allowed Items")]
    public List<string> allowedCraftingItems = new List<string>();

    private static CraftingSystem instance;
    public static CraftingSystem Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CraftingSystem>();
            return instance;
        }
    }

    // Craft edilecek miktarı saklamak için
    public int craftMultiplier { get; private set; } = 1;

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

    public bool CanPlaceInCraftingSlot(string itemID)
    {
        return allowedCraftingItems.Contains(itemID);
    }

    public CraftingRecipe FindMatchingRecipe(CraftingSlot[,] grid)
    {
        Debug.Log("FindMatchingRecipe çağrıldı");
        craftMultiplier = 1; // Reset multiplier

        foreach (var recipe in recipes)
        {
            Debug.Log($"Recipe kontrol ediliyor: {recipe.recipeName}, usePattern: {recipe.usePattern}");

            if (recipe.usePattern)
            {
                // Pattern-based crafting - DÜZELTİLDİ
                if (DoesPatternMatch(recipe, grid))
                {
                    Debug.Log($"Pattern eşleşti: {recipe.recipeName}, Multiplier: {craftMultiplier}");
                    return recipe;
                }
            }
            else
            {
                // Shapeless crafting - DÜZELTİLDİ
                if (DoesShapelessMatch(recipe, grid))
                {
                    Debug.Log($"Shapeless eşleşti: {recipe.recipeName}, Multiplier: {craftMultiplier}");
                    return recipe;
                }
            }
        }

        Debug.Log("Hiç recipe bulunamadı");
        craftMultiplier = 1;
        return null;
    }

    private bool DoesPatternMatch(CraftingRecipe recipe, CraftingSlot[,] grid)
    {
        Debug.Log("Pattern matching başladı");

        int minMultiplier = int.MaxValue;
        bool hasAnyIngredient = false;

        for (int y = 0; y < 3; y++) // Y koordinatı satır
        {
            for (int x = 0; x < 3; x++) // X koordinatı sütun
            {
                string expectedItemID = recipe.pattern.GetSlot(x, y);
                string actualItemID = grid[x, y].IsEmpty ? "" : grid[x, y].item.id;

                Debug.Log($"Pattern kontrol [{x},{y}]: Expected='{expectedItemID}', Actual='{actualItemID}'");

                // Boş slot kontrolü
                if (string.IsNullOrEmpty(expectedItemID))
                {
                    if (!string.IsNullOrEmpty(actualItemID))
                    {
                        Debug.Log($"Pattern uyuşmadı [{x},{y}] - Boş olması gereken yerde item var");
                        return false;
                    }
                }
                else
                {
                    // Item bekleniyor
                    if (expectedItemID != actualItemID)
                    {
                        Debug.Log($"Pattern uyuşmadı [{x},{y}] - Farklı item");
                        return false;
                    }

                    // Miktar kontrolü - en az 1 olmalı
                    if (grid[x, y].amount < 1)
                    {
                        Debug.Log($"Pattern uyuşmadı [{x},{y}] - Yetersiz miktar");
                        return false;
                    }

                    hasAnyIngredient = true;
                    // Bu pozisyondaki miktara göre kaç kez craft yapılabilir hesapla
                    minMultiplier = Mathf.Min(minMultiplier, grid[x, y].amount);
                }
            }
        }

        if (!hasAnyIngredient)
        {
            Debug.Log("Pattern'de hiç ingredient bulunamadı");
            return false;
        }

        craftMultiplier = minMultiplier;
        Debug.Log($"Pattern tamamen eşleşti! Multiplier: {craftMultiplier}");
        return true;
    }

    private bool DoesShapelessMatch(CraftingRecipe recipe, CraftingSlot[,] grid)
    {
        Debug.Log("Shapeless matching başladı");

        // Grid'deki tüm itemları say
        Dictionary<string, int> gridItems = new Dictionary<string, int>();

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (!grid[x, y].IsEmpty)
                {
                    string itemID = grid[x, y].item.id;
                    if (gridItems.ContainsKey(itemID))
                        gridItems[itemID] += grid[x, y].amount;
                    else
                        gridItems[itemID] = grid[x, y].amount;

                    Debug.Log($"Grid item bulundu [{x},{y}]: {itemID} x{grid[x, y].amount}");
                }
            }
        }

        // Recipe'deki gereklilikleri kontrol et ve minimum multiplier'ı hesapla
        int minMultiplier = int.MaxValue;
        bool hasValidIngredients = true;

        foreach (var ingredient in recipe.ingredients)
        {
            Debug.Log($"Gerekli ingredient: {ingredient.itemID} x{ingredient.amount}");

            if (!gridItems.ContainsKey(ingredient.itemID))
            {
                Debug.Log($"Gerekli ingredient bulunamadı: {ingredient.itemID}");
                hasValidIngredients = false;
                break;
            }

            if (gridItems[ingredient.itemID] < ingredient.amount)
            {
                Debug.Log($"Yetersiz miktar: {ingredient.itemID} - Gerekli: {ingredient.amount}, Mevcut: {gridItems[ingredient.itemID]}");
                hasValidIngredients = false;
                break;
            }

            // Bu ingredient'e göre kaç kez craft yapılabilir
            int possibleCrafts = gridItems[ingredient.itemID] / ingredient.amount;
            minMultiplier = Mathf.Min(minMultiplier, possibleCrafts);
        }

        if (!hasValidIngredients)
        {
            return false;
        }

        // Grid'de recipe'de olmayan extra item var mı kontrol et
        foreach (var gridItem in gridItems)
        {
            bool foundInRecipe = false;
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient.itemID == gridItem.Key)
                {
                    foundInRecipe = true;
                    break;
                }
            }

            if (!foundInRecipe)
            {
                Debug.Log($"Extra item bulundu: {gridItem.Key}");
                return false; // Extra item var
            }
        }

        craftMultiplier = minMultiplier;
        Debug.Log($"Shapeless recipe tamamen eşleşti! Multiplier: {craftMultiplier}");
        return true;
    }
}