using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OvenCookingManager : MonoBehaviour
{
    [Header("Frying Slots")]
    public CookingSlot fryingIngredientSlot;
    public CookingSlot fryingLiquidSlot;
    public CookingSlot fryingOutputSlot;

    [Header("Baking Slots")]
    public CookingSlot bakingIngredientSlot;
    public CookingSlot bakingOutputSlot;

    [Header("UI References")]
    public CookingSlotUI fryingIngredientUI;
    public CookingSlotUI fryingLiquidUI;
    public CookingSlotUI fryingOutputUI;
    public CookingSlotUI bakingIngredientUI;
    public CookingSlotUI bakingOutputUI;

    [Header("Progress Bars")]
    public Slider fryingProgressBar;
    public Slider bakingProgressBar;

    [Header("ItemDatabase")]
    public ItemDatabase itemDatabase;

    private Coroutine fryingCoroutine;
    private Coroutine bakingCoroutine;

    private void Start()
    {
        InitializeSlots();
        SetupUI();

        // Progress barları başlangıçta gizle
        if (fryingProgressBar != null) fryingProgressBar.gameObject.SetActive(false);
        if (bakingProgressBar != null) bakingProgressBar.gameObject.SetActive(false);
    }

    private void InitializeSlots()
    {
        fryingIngredientSlot = new CookingSlot();
        fryingLiquidSlot = new CookingSlot();
        fryingOutputSlot = new CookingSlot();
        bakingIngredientSlot = new CookingSlot();
        bakingOutputSlot = new CookingSlot();
    }

    private void SetupUI()
    {
        if (fryingIngredientUI != null)
            fryingIngredientUI.Setup(fryingIngredientSlot, CookingSlotType.FryingIngredient, this);
        if (fryingLiquidUI != null)
            fryingLiquidUI.Setup(fryingLiquidSlot, CookingSlotType.Liquid, this);
        if (fryingOutputUI != null)
            fryingOutputUI.Setup(fryingOutputSlot, CookingSlotType.Output, this);
        if (bakingIngredientUI != null)
            bakingIngredientUI.Setup(bakingIngredientSlot, CookingSlotType.BakingIngredient, this);
        if (bakingOutputUI != null)
            bakingOutputUI.Setup(bakingOutputSlot, CookingSlotType.Output, this);
    }

    public void CheckAndStartFrying()
    {
        if (fryingCoroutine != null) return; // Zaten pişiyor

        if (!fryingIngredientSlot.IsEmpty && !fryingLiquidSlot.IsEmpty && fryingOutputSlot.IsEmpty)
        {
            var recipe = CookingSystem.Instance.GetRecipe(
                CookingType.Frying,
                fryingIngredientSlot.item.id,
                fryingLiquidSlot.item.id
            );

            if (recipe != null)
            {
                fryingCoroutine = StartCoroutine(CookingProcess(CookingType.Frying, recipe));
            }
        }
    }

    public void CheckAndStartBaking()
    {
        if (bakingCoroutine != null) return; // Zaten pişiyor

        if (!bakingIngredientSlot.IsEmpty && bakingOutputSlot.IsEmpty)
        {
            var recipe = CookingSystem.Instance.GetRecipe(
                CookingType.Baking,
                bakingIngredientSlot.item.id
            );

            if (recipe != null)
            {
                bakingCoroutine = StartCoroutine(CookingProcess(CookingType.Baking, recipe));
            }
        }
    }

    private IEnumerator CookingProcess(CookingType cookingType, CookingRecipe recipe)
    {
        float cookingTime = recipe.cookingTime;
        float elapsed = 0f;

        // Progress barını göster ve başlat
        Slider progressBar = cookingType == CookingType.Frying ? fryingProgressBar : bakingProgressBar;
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        // Pişirme süreci - malzeme kontrolü eklendi
        while (elapsed < cookingTime)
        {
            elapsed += Time.deltaTime;

            // ÖNEMLI: Her frame malzemelerin hala yerinde olup olmadığını kontrol et
            bool ingredientsStillPresent = CheckIngredientsPresent(cookingType);
            if (!ingredientsStillPresent)
            {
                Debug.Log("Malzemeler kaldırıldı, pişirme iptal ediliyor!");

                // Progress barını gizle
                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(false);
                }

                // Coroutine referansını temizle
                if (cookingType == CookingType.Frying)
                    fryingCoroutine = null;
                else
                    bakingCoroutine = null;

                yield break; // Coroutine'i sonlandır
            }

            // Progress bar güncelle
            if (progressBar != null)
            {
                progressBar.value = elapsed / cookingTime;
            }

            yield return null;
        }

        // Pişirme tamamlandı
        CompleteCooking(cookingType, recipe);

        // Progress barını gizle
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }

        // Coroutine referansını temizle
        if (cookingType == CookingType.Frying)
            fryingCoroutine = null;
        else
            bakingCoroutine = null;
    }

    // YENİ METOD: Malzemelerin hala yerinde olup olmadığını kontrol eder
    private bool CheckIngredientsPresent(CookingType cookingType)
    {
        if (cookingType == CookingType.Frying)
        {
            return !fryingIngredientSlot.IsEmpty && !fryingLiquidSlot.IsEmpty;
        }
        else if (cookingType == CookingType.Baking)
        {
            return !bakingIngredientSlot.IsEmpty;
        }
        return false;
    }

    private void CompleteCooking(CookingType cookingType, CookingRecipe recipe)
    {
        Item outputItem = itemDatabase.GetItemByID(recipe.outputItemID);
        if (outputItem == null) return;

        if (cookingType == CookingType.Frying)
        {
            // Malzemeleri tüket
            fryingIngredientSlot.Clear();
            fryingLiquidSlot.Clear();

            // Çıktıyı yerleştir
            fryingOutputSlot.SetItem(outputItem, 1);

            // UI güncelle
            fryingIngredientUI.UpdateUI();
            fryingLiquidUI.UpdateUI();
            fryingOutputUI.UpdateUI();
        }
        else if (cookingType == CookingType.Baking)
        {
            // Malzemeyi tüket
            bakingIngredientSlot.Clear();

            // Çıktıyı yerleştir
            bakingOutputSlot.SetItem(outputItem, 1);

            // UI güncelle
            bakingIngredientUI.UpdateUI();
            bakingOutputUI.UpdateUI();
        }
    }

    public void OnSlotChanged(CookingSlotType slotType)
    {
        // Slot değiştiğinde otomatik pişirme kontrolü
        if (slotType == CookingSlotType.FryingIngredient || slotType == CookingSlotType.Liquid)
        {
            CheckAndStartFrying();
        }
        else if (slotType == CookingSlotType.BakingIngredient)
        {
            CheckAndStartBaking();
        }
    }

    // YENİ METOD: Malzeme kaldırıldığında çağrılır
    public void OnIngredientRemoved(CookingSlotType slotType)
    {
        // Kızartma malzemesi kaldırılırsa kızartmayı iptal et
        if (slotType == CookingSlotType.FryingIngredient || slotType == CookingSlotType.Liquid)
        {
            CancelFrying();
        }
        // Fırın malzemesi kaldırılırsa fırınlamayı iptal et
        else if (slotType == CookingSlotType.BakingIngredient)
        {
            CancelBaking();
        }
    }

    // Pişirme işlemini iptal etme metotları güncellendi
    public void CancelFrying()
    {
        if (fryingCoroutine != null)
        {
            StopCoroutine(fryingCoroutine);
            fryingCoroutine = null;
            if (fryingProgressBar != null)
            {
                fryingProgressBar.gameObject.SetActive(false);
                fryingProgressBar.value = 0f; // Progress'i sıfırla
            }
            Debug.Log("Kızartma iptal edildi!");
        }
    }

    public void CancelBaking()
    {
        if (bakingCoroutine != null)
        {
            StopCoroutine(bakingCoroutine);
            bakingCoroutine = null;
            if (bakingProgressBar != null)
            {
                bakingProgressBar.gameObject.SetActive(false);
                bakingProgressBar.value = 0f; // Progress'i sıfırla
            }
            Debug.Log("Fırınlama iptal edildi!");
        }
    }
}