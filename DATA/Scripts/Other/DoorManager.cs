using UnityEngine;
using System.Collections.Generic;

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private Dictionary<string, DoorController> doors = new Dictionary<string, DoorController>();
    private List<DoorController> allDoors = new List<DoorController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        DoorController.OnDoorStateChanged += OnDoorStateChanged;
    }

    private void OnDisable()
    {
        DoorController.OnDoorStateChanged -= OnDoorStateChanged;
    }

    private void Start()
    {
        RegisterAllDoors();
    }

    private void RegisterAllDoors()
    {
        DoorController[] foundDoors = FindObjectsOfType<DoorController>();
        foreach (var door in foundDoors)
        {
            RegisterDoor(door);
        }

        if (enableDebugLogs)
            Debug.Log($"[DoorManager] Registered {allDoors.Count} doors");
    }

    public void RegisterDoor(DoorController door)
    {
        if (door == null) return;

        string doorId = door.gameObject.name;

        if (!doors.ContainsKey(doorId))
        {
            doors[doorId] = door;
            allDoors.Add(door);
        }
    }

    public void UnregisterDoor(DoorController door)
    {
        if (door == null) return;

        string doorId = door.gameObject.name;
        doors.Remove(doorId);
        allDoors.Remove(door);
    }

    public DoorController GetDoor(string doorId)
    {
        doors.TryGetValue(doorId, out DoorController door);
        return door;
    }

    public void OpenAllDoors()
    {
        foreach (var door in allDoors)
        {
            door.OpenDoor();
        }
    }

    public void CloseAllDoors()
    {
        foreach (var door in allDoors)
        {
            door.CloseDoor();
        }
    }

    private void OnDoorStateChanged(DoorController door)
    {
        if (enableDebugLogs)
            Debug.Log($"[DoorManager] Door {door.gameObject.name} changed state to {(door.IsOpen ? "Open" : "Closed")}");
    }
}