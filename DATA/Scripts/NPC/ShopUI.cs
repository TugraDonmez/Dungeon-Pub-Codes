using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public GameObject shopPanel;
    public Transform itemContainer;
    public GameObject shopItemPrefab;
    private PlayerInventoryManager inventoryManager;
    private PlayerWallet playerWallet;
    private RelationshipManager relationshipManager;

    private void Awake()
    {
        inventoryManager = FindObjectOfType<PlayerInventoryManager>();
        playerWallet = FindObjectOfType<PlayerWallet>();
        relationshipManager = FindObjectOfType<RelationshipManager>();
    }

    public void OpenShop(ShopProfile shop)
    {
        shopPanel.SetActive(true);
        ClearShop();

        int relation = relationshipManager.GetRelation(shop.npcName);

        foreach (var entry in shop.itemsForSale)
        {
            GameObject go = Instantiate(shopItemPrefab, itemContainer);
            // prefab içinde: isim, fiyat, stok ve buton olacak
            go.transform.Find("Name").GetComponent<TMP_Text>().text = entry.item.itemName;
            int price = entry.GetPriceByRelationship(relation);
            go.transform.Find("Price").GetComponent<TMP_Text>().text = price.ToString();
            go.transform.Find("Stock").GetComponent<TMP_Text>().text = $"Stok: {entry.stock}";

            Button buyBtn = go.transform.Find("BuyButton").GetComponent<Button>();
            buyBtn.onClick.AddListener(() =>
            {
                if (entry.stock > 0 && playerWallet.SpendMoney(price))
                {
                    if (inventoryManager.TryAddItem(entry.item, 1))
                    {
                        entry.stock--;
                        OpenShop(shop); // UI'yi yenile
                    }
                }
            });
        }
    }

    private void ClearShop()
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);
    }
}
