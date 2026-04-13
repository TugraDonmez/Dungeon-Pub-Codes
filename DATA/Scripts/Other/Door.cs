using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string destinationSpawnId; // Sahneye gidecek oyuncunun nereye spawnlanacağını belirt

    public void Interact()
    {
        PlayerPrefs.SetString("spawnId", destinationSpawnId); // Gidilecek yerdeki spawn noktası
        SceneManager.LoadScene(sceneToLoad);
    }
}
