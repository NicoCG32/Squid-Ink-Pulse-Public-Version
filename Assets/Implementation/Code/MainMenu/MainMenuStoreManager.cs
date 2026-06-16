using UnityEngine;

[DisallowMultipleComponent]
public class MainMenuStoreManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject storePanel;

    private void Start()
    {
        // Ensure the store is hidden when the game starts
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }
    }

    public void Open()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }
    }

    public void Close()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }
    }
}