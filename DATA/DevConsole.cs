
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class DevConsole : MonoBehaviour
{
    [Header("Console UI References")]
    public GameObject consolePanel;
    public TMP_InputField commandInput;
    public TMP_Text outputText;
    public ScrollRect scrollRect;
    public Button submitButton;

    [Header("Console Settings")]
    public KeyCode toggleKey = KeyCode.BackQuote; // ~ tuşu
    public int maxOutputLines = 100;
    public Color normalTextColor = Color.white;
    public Color errorTextColor = Color.red;
    public Color successTextColor = Color.green;

    [Header("System References")]
    public PlayerInventoryManager inventoryManager;
    public ItemDatabase itemDatabase;

    private bool isConsoleOpen = false;
    private List<string> outputLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;

    private Dictionary<string, System.Action<string[]>> commands;

    private void Start()
    {
        InitializeConsole();
        RegisterCommands();
        consolePanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }

        if (isConsoleOpen)
        {
            HandleConsoleInput();
        }
    }

    private void InitializeConsole()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(ExecuteCommand);
        }

        // Input field 
        if (commandInput != null)
        {
            commandInput.onEndEdit.AddListener(OnInputEndEdit);
        }
    }

    private void RegisterCommands()
    {
        commands = new Dictionary<string, System.Action<string[]>>();

        commands.Add("item", ItemCommand);
        commands.Add("give", ItemCommand);

        commands.Add("clear", ClearInventoryCommand);
        commands.Add("save", SaveInventoryCommand);
        commands.Add("load", LoadInventoryCommand);

        commands.Add("help", HelpCommand);
        commands.Add("list", ListItemsCommand);
        commands.Add("cls", ClearConsoleCommand);
        commands.Add("clear_console", ClearConsoleCommand);
    }

    private void ToggleConsole()
    {
        isConsoleOpen = !isConsoleOpen;
        consolePanel.SetActive(isConsoleOpen);

        if (isConsoleOpen)
        {
            commandInput.ActivateInputField();
            commandInput.Select();

            if (inventoryManager != null)
            {
                var player = inventoryManager.gameObject;
                SetPlayerControlsEnabled(player, false);
            }
        }
        else
        {
            if (inventoryManager != null)
            {
                var player = inventoryManager.gameObject;
                SetPlayerControlsEnabled(player, true);
            }
        }
    }

    private void SetPlayerControlsEnabled(GameObject player, bool enabled)
    {
        var movement = player.GetComponent<PlayerMovement>();
        var interaction = player.GetComponent<PlayerInteraction>();
        var combat = player.GetComponent<PlayerCombat>();

        if (movement != null) movement.enabled = enabled;
        if (interaction != null) interaction.enabled = enabled;
        if (combat != null) combat.enabled = enabled;
    }

    private void HandleConsoleInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExecuteCommand();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            NavigateHistory(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NavigateHistory(1);
        }
    }

    private void OnInputEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExecuteCommand();
        }
    }

    private void ExecuteCommand()
    {
        string commandText = commandInput.text.Trim();
        if (string.IsNullOrEmpty(commandText)) return;


        commandHistory.Add(commandText);
        if (commandHistory.Count > 50)
        {
            commandHistory.RemoveAt(0);
        }
        historyIndex = -1;


        AddOutput($"> {commandText}", normalTextColor);


        string[] parts = commandText.Split(' ');
        string command = parts[0].ToLower();

        if (commands.ContainsKey(command))
        {
            try
            {
                commands[command](parts);
            }
            catch (System.Exception e)
            {
                AddOutput($"Komut çalıştırılırken hata: {e.Message}", errorTextColor);
            }
        }
        else
        {
            AddOutput($"Bilinmeyen komut: {command}. Yardım için 'help' yazın.", errorTextColor);
        }


        commandInput.text = "";
        commandInput.ActivateInputField();
    }

    private void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0) return;

        historyIndex += direction;
        historyIndex = Mathf.Clamp(historyIndex, -1, commandHistory.Count - 1);

        if (historyIndex >= 0)
        {
            commandInput.text = commandHistory[commandHistory.Count - 1 - historyIndex];
            commandInput.caretPosition = commandInput.text.Length;
        }
        else
        {
            commandInput.text = "";
        }
    }

    private void AddOutput(string text, Color color)
    {
        outputLines.Add($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");


        if (outputLines.Count > maxOutputLines)
        {
            outputLines.RemoveAt(0);
        }

        // Output text'i güncelle
        outputText.text = string.Join("\n", outputLines);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // KOMUT FONKSİYONLARI
    private void ItemCommand(string[] args)
    {
        if (args.Length < 2)
        {
            AddOutput("Kullanım: item <item_id> [miktar]", errorTextColor);
            AddOutput("Örnek: item apple 5", normalTextColor);
            return;
        }

        string itemId = args[1].ToLower();
        int amount = 1;

        if (args.Length >= 3)
        {
            if (!int.TryParse(args[2], out amount) || amount <= 0)
            {
                AddOutput("Geçersiz miktar! Pozitif bir sayı girin.", errorTextColor);
                return;
            }
        }

        // Item'i bul
        Item item = itemDatabase.GetItemByID(itemId);
        if (item == null)
        {
            AddOutput($"Item bulunamadı: {itemId}", errorTextColor);
            AddOutput("Mevcut itemler için 'list' komutunu kullanın.", normalTextColor);
            return;
        }

        // Envantere ekle
        bool success = inventoryManager.TryAddItem(item, amount);
        if (success)
        {
            AddOutput($"✓ {item.itemName} x{amount} envantere eklendi!", successTextColor);
        }
        else
        {
            AddOutput($"✗ {item.itemName} eklenemedi. Envanter dolu olabilir.", errorTextColor);
        }
    }

    private void ClearInventoryCommand(string[] args)
    {
        if (inventoryManager?.inventory != null)
        {
            foreach (var slot in inventoryManager.inventory.slots)
            {
                slot.Clear();
            }
            inventoryManager.inventoryUI?.DrawInventory();
            AddOutput("✓ Envanter temizlendi!", successTextColor);
        }
        else
        {
            AddOutput("✗ Envanter sistemi bulunamadı!", errorTextColor);
        }
    }

    private void SaveInventoryCommand(string[] args)
    {
        if (inventoryManager?.inventory != null)
        {
            inventoryManager.inventory.Save();
            AddOutput("✓ Envanter kaydedildi!", successTextColor);
        }
        else
        {
            AddOutput("✗ Envanter sistemi bulunamadı!", errorTextColor);
        }
    }

    private void LoadInventoryCommand(string[] args)
    {
        if (inventoryManager?.inventory != null && itemDatabase != null)
        {
            inventoryManager.inventory.Load(itemDatabase);
            inventoryManager.inventoryUI?.DrawInventory();
            AddOutput("✓ Envanter yüklendi!", successTextColor);
        }
        else
        {
            AddOutput("✗ Envanter sistemi veya item database bulunamadı!", errorTextColor);
        }
    }

    private void ListItemsCommand(string[] args)
    {
        if (itemDatabase?.items == null || itemDatabase.items.Count == 0)
        {
            AddOutput("Item database bulunamadı veya boş!", errorTextColor);
            return;
        }

        AddOutput("=== MEVCUT İTEMLER ===", normalTextColor);
        foreach (var item in itemDatabase.items)
        {
            AddOutput($"- {item.id} ({item.itemName})", normalTextColor);
        }
        AddOutput($"Toplam: {itemDatabase.items.Count} item", normalTextColor);
    }

    private void ClearConsoleCommand(string[] args)
    {
        outputLines.Clear();
        outputText.text = "";
        AddOutput("Konsol temizlendi.", normalTextColor);
    }

    private void HelpCommand(string[] args)
    {
        AddOutput("=== GELİŞTİRİCİ KONSOLU KOMUTLARI ===", normalTextColor);
        AddOutput("item <id> [miktar] - Envantere item ekler", normalTextColor);
        AddOutput("give <id> [miktar] - item komutu ile aynı", normalTextColor);
        AddOutput("clear - Envanteri temizler", normalTextColor);
        AddOutput("save - Envanteri kaydeder", normalTextColor);
        AddOutput("load - Envanteri yükler", normalTextColor);
        AddOutput("list - Mevcut itemleri listeler", normalTextColor);
        AddOutput("cls/clear_console - Konsolu temizler", normalTextColor);
        AddOutput("help - Bu yardım menüsünü gösterir", normalTextColor);
        AddOutput("", normalTextColor);
        AddOutput("Komut geçmişi: ↑/↓ ok tuşları", normalTextColor);
        AddOutput("Konsolu kapatmak: ~ tuşu", normalTextColor);
    }
}