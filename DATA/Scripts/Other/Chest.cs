using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject chestUI;
    public Inventory chestInventory;
    public ItemDatabase itemDatabase;
    public GameObject Target;
    private Animator animator;

    [Header("Chest Settings")]
    public string chestID = "chest_01";

    // Chest durumunu takip etmek için
    private bool isChestOpen = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (chestInventory != null)
        {
            chestInventory.saveFileName = "chest_" + chestID;
            LoadChestData();
        }
    }

    public void Interact()
    {
        if (isChestOpen)
        {
            CloseChest();
        }
        else
        {
            OpenChest();
        }
    }

    public void OpenChest()
    {
        if (isChestOpen) return; // Zaten açık

        isChestOpen = true;
        chestUI.SetActive(true);
        LoadChestData(); // Her açıldığında yükle

        // UI'ı yeniden çiz
        var chestUI_Component = chestUI.GetComponent<InventoryUI>();
        if (chestUI_Component != null)
        {
            chestUI_Component.DrawInventory();
        }

        animator.SetBool("Open", true);
        Camera.main.GetComponent<PlayerCamera>().target = Target.transform;

        Debug.Log($"Chest açıldı: {chestID}");
    }

    public void CloseChest()
    {
        if (!isChestOpen) return; // Zaten kapalı

        SaveChestData(); // Kapatmadan önce kaydet

        isChestOpen = false;
        chestUI.SetActive(false);
        animator.SetBool("Open", false);
        Camera.main.GetComponent<PlayerCamera>().target = GameObject.FindGameObjectWithTag("Player").transform;

        Debug.Log($"Chest kapatıldı: {chestID}");
    }

    private void LoadChestData()
    {
        if (chestInventory != null && itemDatabase != null)
        {
            chestInventory.Load(itemDatabase);
            Debug.Log($"Chest data yüklendi: {chestID}");
        }
    }

    private void SaveChestData()
    {
        if (chestInventory != null)
        {
            chestInventory.Save();
            Debug.Log($"Chest data kaydedildi: {chestID}");
        }
    }

    // Güvenlik kayıtları
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isChestOpen)
            SaveChestData();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isChestOpen)
            SaveChestData();
    }

    private void OnDestroy()
    {
        if (isChestOpen)
            SaveChestData();
    }

    // Debug için
    [ContextMenu("Force Save Chest")]
    private void ForceSave()
    {
        SaveChestData();
    }

    [ContextMenu("Force Load Chest")]
    private void ForceLoad()
    {
        LoadChestData();
        var chestUI_Component = chestUI.GetComponent<InventoryUI>();
        if (chestUI_Component != null)
        {
            chestUI_Component.DrawInventory();
        }
    }
}