using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("������ �����յ�")]
    [SerializeField] private GameObject persistentUiCanvasPrefab;
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private GameObject manualPanelPrefab;
    [SerializeField] private GameObject yeokListPanelPrefab;

    private GameObject settingsPanelInstance;
    private GameObject manualPanelInstance;
    private GameObject yeokListPanelInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (persistentUiCanvasPrefab != null)
            {
                GameObject canvasInstance = Instantiate(persistentUiCanvasPrefab);
                DontDestroyOnLoad(canvasInstance);

                Button settingsButton = canvasInstance.GetComponentInChildren<Button>();
                if (settingsButton != null)
                {
                    settingsButton.onClick.AddListener(ToggleSettingsPanel);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update �Լ��� ESC Ű ���� ������ �ٽ� �߰��մϴ�.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                settingsPanelInstance = Instantiate(settingsPanelPrefab, canvas.transform);
            }
        }
        else
        {
            // �ٸ� �г�(�Ŵ���, ���϶�)�� �����ִٸ� �װͺ��� �ݽ��ϴ�.
            if (manualPanelInstance != null && manualPanelInstance.activeSelf)
            {
                manualPanelInstance.SetActive(false);
            }
            else if (yeokListPanelInstance != null && yeokListPanelInstance.activeSelf)
            {
                yeokListPanelInstance.SetActive(false);
            }
            else // �ٸ� �г��� ��� �������� ���� ���� ���� â�� �ݽ��ϴ�.
            {
                settingsPanelInstance.SetActive(!settingsPanelInstance.activeSelf);
            }
        }
    }

    public void ShowManual()
    {
        if (manualPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            manualPanelInstance = Instantiate(manualPanelPrefab, canvas.transform);
        }
        manualPanelInstance.SetActive(true);
    }

    public void ShowYeokList()
    {
        if (yeokListPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                yeokListPanelInstance = Instantiate(yeokListPanelPrefab, canvas.transform);
            }
        }
        yeokListPanelInstance.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}