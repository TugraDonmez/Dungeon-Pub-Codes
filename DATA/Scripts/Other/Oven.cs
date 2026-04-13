using UnityEngine;

public class Oven : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject ovenUI;
    public GameObject Target;
    private Animator animator;

    [Header("Oven Settings")]
    public string ovenID = "oven_01"; // Her fırın için benzersiz ID

    [Header("Cooking Manager")]
    public OvenCookingManager cookingManager; // Pişirme yöneticisi referansı

    private void Start()
    {
        // Eğer cooking manager atanmamışsa, UI içinde ara
        if (cookingManager == null && ovenUI != null)
        {
            cookingManager = ovenUI.GetComponentInChildren<OvenCookingManager>();
        }
    }

    public void Interact()
    {
        if (ovenUI.activeInHierarchy)
        {
            CloseOven();
        }
        else
        {
            OpenOven();
        }
    }

    public void OpenOven()
    {
        ovenUI.SetActive(true);
        Camera.main.GetComponent<PlayerCamera>().target = Target.transform;

        // Cooking manager'ı başlat
        if (cookingManager != null)
        {
            cookingManager.enabled = true;
        }
    }

    public void CloseOven()
    {
        ovenUI.SetActive(false);
        Camera.main.GetComponent<PlayerCamera>().target = GameObject.FindGameObjectWithTag("Player").transform;
    }
}
