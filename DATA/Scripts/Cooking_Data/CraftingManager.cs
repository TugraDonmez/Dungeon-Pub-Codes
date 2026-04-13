using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Header("Crafting Grid (3x3)")]
    public CraftingSlot[,] craftingGrid = new CraftingSlot[3, 3];

    [Header("Output Slot")]
    public CraftingSlot outputSlot;

    [Header("UI References - Otomatik Bulunacak")]
    [SerializeField] private Transform gridParent; // Grid slotlarının parent'ı
    [SerializeField] private Transform outputParent; // Output slotun parent'ı
    public CraftingSlotUI[,] craftingGridUI = new CraftingSlotUI[3, 3];
    public CraftingSlotUI outputSlotUI;

    [Header("ItemDatabase")]
    public ItemDatabase itemDatabase;

    [Header("Auto Setup - Inspector'da Kullan")]
    [Tooltip("Bu butona basarak UI referanslarını otomatik bulabilirsiniz")]
    public bool autoFindUIReferences = false;

    private void Start()
    {
        InitializeSlots();

        // Eğer UI referansları null ise otomatik bul
        if (AreUIReferencesNull())
        {
            Debug.Log("UI referansları null, otomatik arama yapılıyor...");
            AutoFindUIReferences();
        }

        SetupUI();
    }

    private void OnValidate()
    {
        // Inspector'da autoFindUIReferences true yapıldığında otomatik ara
        if (autoFindUIReferences && Application.isPlaying)
        {
            AutoFindUIReferences();
            autoFindUIReferences = false;
        }
    }

    private bool AreUIReferencesNull()
    {
        // Grid UI kontrolü
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (craftingGridUI[x, y] != null)
                    return false; // En az biri dolu ise null değil
            }
        }
        return true; // Hepsi null
    }

    public void AutoFindUIReferences()
    {
        Debug.Log("UI referansları otomatik aranıyor...");

        // Tüm CraftingSlotUI'ları bul
        CraftingSlotUI[] allSlots = GetComponentsInChildren<CraftingSlotUI>();

        Debug.Log($"Toplam {allSlots.Length} CraftingSlotUI bulundu");

        foreach (var slotUI in allSlots)
        {
            // SlotUI'ın kendine özel setup bilgilerini kontrol et
            var slotTransform = slotUI.transform;
            string slotName = slotTransform.name.ToLower();

            Debug.Log($"Slot kontrol ediliyor: {slotName}");

            // Output slot kontrolü
            if (slotName.Contains("output") || slotName.Contains("result"))
            {
                outputSlotUI = slotUI;
                Debug.Log($"Output slot bulundu: {slotName}");
                continue;
            }

            // Grid slot kontrolü - isimden koordinat çıkar
            if (ExtractGridPosition(slotName, out int x, out int y))
            {
                craftingGridUI[x, y] = slotUI;
                Debug.Log($"Grid slot [{x},{y}] atandı: {slotName}");
            }
            else
            {
                // Eğer isimden çıkaramazsa, parent hiyerarşisine göre pozisyon bul
                TryFindPositionByHierarchy(slotUI, out x, out y);
                if (x >= 0 && x < 3 && y >= 0 && y < 3)
                {
                    craftingGridUI[x, y] = slotUI;
                    Debug.Log($"Grid slot [{x},{y}] hiyerarşi ile atandı: {slotName}");
                }
            }
        }

        // Sonuçları kontrol et
        CheckUIAssignments();
    }

    private bool ExtractGridPosition(string slotName, out int x, out int y)
    {
        x = -1;
        y = -1;

        // Yaygın isimlendirme kalıpları:
        // "slot_0_1", "slot01", "craftingslot_1_2", "gridslot12" vb.

        // Rakamları bul
        var numbers = new List<int>();
        string currentNumber = "";

        foreach (char c in slotName)
        {
            if (char.IsDigit(c))
            {
                currentNumber += c;
            }
            else
            {
                if (currentNumber.Length > 0)
                {
                    if (int.TryParse(currentNumber, out int num))
                        numbers.Add(num);
                    currentNumber = "";
                }
            }
        }

        // Son rakamı da ekle
        if (currentNumber.Length > 0)
        {
            if (int.TryParse(currentNumber, out int num))
                numbers.Add(num);
        }

        // En az 2 rakam olmalı (x, y için)
        if (numbers.Count >= 2)
        {
            x = numbers[0];
            y = numbers[1];
            return x >= 0 && x < 3 && y >= 0 && y < 3;
        }

        return false;
    }

    private void TryFindPositionByHierarchy(CraftingSlotUI slotUI, out int x, out int y)
    {
        x = -1;
        y = -1;

        // Transform'un sibling index'ini kullanarak pozisyon bul
        Transform parent = slotUI.transform.parent;
        if (parent != null)
        {
            int siblingIndex = slotUI.transform.GetSiblingIndex();

            // 3x3 grid'de sibling index'ten koordinat hesapla
            if (siblingIndex >= 0 && siblingIndex < 9)
            {
                x = siblingIndex % 3;
                y = siblingIndex / 3;
            }
        }
    }

    private void CheckUIAssignments()
    {
        Debug.Log("UI atama sonuçları kontrol ediliyor...");

        int assignedCount = 0;
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (craftingGridUI[x, y] != null)
                {
                    assignedCount++;
                }
                else
                {
                    Debug.LogWarning($"CraftingGridUI [{x},{y}] hala null!");
                }
            }
        }

        Debug.Log($"Toplam {assignedCount}/9 grid slot atandı");

        if (outputSlotUI == null)
        {
            Debug.LogWarning("OutputSlotUI hala null!");
        }
        else
        {
            Debug.Log("OutputSlotUI başarıyla atandı");
        }
    }

    private void InitializeSlots()
    {
        // 3x3 grid initialize
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                craftingGrid[x, y] = new CraftingSlot();
            }
        }

        // Output slot initialize
        outputSlot = new CraftingSlot();

        Debug.Log("Crafting slots initialized");
    }

    private void SetupUI()
    {
        Debug.Log("UI Setup başlıyor...");

        // Grid UI setup
        int successCount = 0;
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (craftingGridUI[x, y] != null)
                {
                    craftingGridUI[x, y].Setup(craftingGrid[x, y], CraftingSlotType.Ingredient, this, x, y);
                    successCount++;
                    Debug.Log($"CraftingGridUI [{x},{y}] setup completed");
                }
                else
                {
                    Debug.LogWarning($"CraftingGridUI [{x},{y}] is null! UI setup atlandı.");
                }
            }
        }

        Debug.Log($"Grid UI Setup: {successCount}/9 slot başarılı");

        // Output UI setup
        if (outputSlotUI != null)
        {
            outputSlotUI.Setup(outputSlot, CraftingSlotType.Output, this, -1, -1);
            Debug.Log("Output slot UI setup completed");
        }
        else
        {
            Debug.LogWarning("Output slot UI is null! Manuel atama gerekli.");
        }
    }

    public void CheckForRecipe()
    {
        Debug.Log("CheckForRecipe başladı");

        // Önce output'u tamamen temizle
        ClearOutput();

        // Grid boş mu kontrol et
        bool hasAnyItem = false;
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (!craftingGrid[x, y].IsEmpty)
                {
                    hasAnyItem = true;
                    break;
                }
            }
            if (hasAnyItem) break;
        }

        // Grid tamamen boşsa recipe arama
        if (!hasAnyItem)
        {
            Debug.Log("Grid boş, recipe aranmıyor");
            return;
        }

        // Debug: Grid durumunu göster
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = craftingGrid[x, y];
                string itemInfo = slot.IsEmpty ? "Empty" : $"{slot.item.id} x{slot.amount}";
                Debug.Log($"Grid [{x},{y}]: {itemInfo}");
            }
        }

        // Recipe ara
        var recipe = CraftingSystem.Instance.FindMatchingRecipe(craftingGrid);

        if (recipe != null)
        {
            Debug.Log($"Recipe bulundu: {recipe.recipeName}");

            Item outputItem = itemDatabase.GetItemByID(recipe.outputItemID);
            if (outputItem != null)
            {
                // Multiplier'ı kullanarak output miktarını hesapla
                int finalOutputAmount = recipe.outputAmount * CraftingSystem.Instance.craftMultiplier;

                outputSlot.SetItem(outputItem, finalOutputAmount);
                Debug.Log($"Output slot set: {outputItem.itemName} x{finalOutputAmount} (Base: {recipe.outputAmount} x Multiplier: {CraftingSystem.Instance.craftMultiplier})");

                if (outputSlotUI != null)
                {
                    outputSlotUI.UpdateUI();
                }
            }
            else
            {
                Debug.LogError($"Output item bulunamadı: {recipe.outputItemID}");
            }
        }
        else
        {
            Debug.Log("Hiç recipe eşleşmedi");
        }
    }


    public void CraftItem()
    {
        Debug.Log("CraftItem başladı - Malzemeler tüketiliyor");

        // Mevcut recipe'yi bul
        var currentRecipe = CraftingSystem.Instance.FindMatchingRecipe(craftingGrid);
        if (currentRecipe == null)
        {
            Debug.LogError("Recipe bulunamadı, craft iptal edildi!");
            return;
        }

        int multiplier = CraftingSystem.Instance.craftMultiplier;
        Debug.Log($"Recipe bulundu: {currentRecipe.recipeName}, Multiplier: {multiplier}");

        // Malzemeleri tüket
        if (currentRecipe.usePattern)
        {
            // Pattern-based recipe - her pozisyon için multiplier kadar tüket
            Debug.Log("Pattern-based recipe malzemeleri tüketiliyor...");

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    string expectedItem = currentRecipe.pattern.GetSlot(x, y);

                    if (!string.IsNullOrEmpty(expectedItem) && !craftingGrid[x, y].IsEmpty)
                    {
                        Debug.Log($"Pattern malzeme tüketiliyor [{x},{y}]: {expectedItem}, Önceki miktar: {craftingGrid[x, y].amount}, Tüketilecek: {multiplier}");

                        craftingGrid[x, y].amount -= multiplier;

                        Debug.Log($"Pattern malzeme tüketildi [{x},{y}]: {expectedItem}, Yeni miktar: {craftingGrid[x, y].amount}");

                        if (craftingGrid[x, y].amount <= 0)
                        {
                            craftingGrid[x, y].Clear();
                            Debug.Log($"Pattern slot temizlendi [{x},{y}]");
                        }
                    }
                }
            }
        }
        else
        {
            // Shapeless recipe - gerekli miktarları multiplier ile çarp ve tüket
            Debug.Log("Shapeless recipe malzemeleri tüketiliyor...");

            foreach (var ingredient in currentRecipe.ingredients)
            {
                int totalToConsume = ingredient.amount * multiplier;
                int remainingToConsume = totalToConsume;

                Debug.Log($"Tüketilecek: {ingredient.itemID} x{totalToConsume} (Base: {ingredient.amount} x Multiplier: {multiplier})");

                for (int x = 0; x < 3 && remainingToConsume > 0; x++)
                {
                    for (int y = 0; y < 3 && remainingToConsume > 0; y++)
                    {
                        if (!craftingGrid[x, y].IsEmpty && craftingGrid[x, y].item.id == ingredient.itemID)
                        {
                            int consumeAmount = Mathf.Min(remainingToConsume, craftingGrid[x, y].amount);

                            Debug.Log($"Shapeless malzeme tüketiliyor [{x},{y}]: {ingredient.itemID}, Önceki miktar: {craftingGrid[x, y].amount}, Tüketilecek: {consumeAmount}");

                            craftingGrid[x, y].amount -= consumeAmount;
                            remainingToConsume -= consumeAmount;

                            Debug.Log($"Shapeless malzeme tüketildi [{x},{y}]: {ingredient.itemID}, Yeni miktar: {craftingGrid[x, y].amount}");

                            if (craftingGrid[x, y].amount <= 0)
                            {
                                craftingGrid[x, y].Clear();
                                Debug.Log($"Shapeless slot temizlendi [{x},{y}]");
                            }
                        }
                    }
                }

                if (remainingToConsume > 0)
                {
                    Debug.LogError($"Yeterli malzeme bulunamadı: {ingredient.itemID}, Eksik: {remainingToConsume}");
                }
            }
        }

        // UI'ları güncelle
        Debug.Log("UI güncellemeleri yapılıyor...");
        UpdateAllGridUI();

        // Yeni recipe kontrolü yap (kalan malzemelerle başka bir şey yapılabilir mi?)
        StartCoroutine(DelayedRecipeCheck());

        Debug.Log("CraftItem tamamlandı - Malzemeler tüketildi");
    }

    // Gecikmiş recipe kontrolü için coroutine
    private System.Collections.IEnumerator DelayedRecipeCheck()
    {
        yield return new WaitForEndOfFrame();
        CheckForRecipe();
    }


    private void UpdateAllGridUI()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (craftingGridUI[x, y] != null)
                {
                    craftingGridUI[x, y].UpdateUI();
                }
            }
        }
    }

    public void OnSlotChanged()
    {
        Debug.Log("OnSlotChanged çağrıldı - Recipe kontrolü yapılıyor");
        CheckForRecipe();
    }

    // Grid pozisyonunu UI referansları için ayarlama (Inspector'da kullanım için)
    public void SetGridUI(int x, int y, CraftingSlotUI slotUI)
    {
        if (x >= 0 && x < 3 && y >= 0 && y < 3)
        {
            craftingGridUI[x, y] = slotUI;
        }
    }

    // Tüm slotları temizle
    public void ClearAllSlots()
    {
        Debug.Log("Tüm slotlar temizleniyor");

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (!craftingGrid[x, y].IsEmpty)
                {
                    // Eşyaları envantere geri ver
                    ReturnItemToInventory(craftingGrid[x, y].item, craftingGrid[x, y].amount);
                    craftingGrid[x, y].Clear();
                }
            }
        }

        outputSlot.Clear();
        UpdateAllGridUI();
        if (outputSlotUI != null) outputSlotUI.UpdateUI();
    }
    public void ClearOutput()
    {
        if (outputSlot != null)
        {
            outputSlot.Clear();
            if (outputSlotUI != null)
            {
                outputSlotUI.UpdateUI();
            }
            Debug.Log("Output slot temizlendi");
        }
    }

    private void ReturnItemToInventory(Item item, int amount)
    {
        var playerInventory = FindObjectOfType<PlayerInventoryManager>();
        if (playerInventory != null)
        {
            bool success = playerInventory.TryAddItem(item, amount);
            Debug.Log($"Envantere geri verildi: {item.itemName} x{amount} - Başarılı: {success}");
        }
    }
}