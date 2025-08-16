using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("생성할 프리팹들")]
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

    // Update 함수에 ESC 키 감지 로직을 다시 추가합니다.
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
            // 다른 패널(매뉴얼, 역일람)이 열려있다면 그것부터 닫습니다.
            if (manualPanelInstance != null && manualPanelInstance.activeSelf)
            {
                manualPanelInstance.SetActive(false);
            }
            else if (yeokListPanelInstance != null && yeokListPanelInstance.activeSelf)
            {
                yeokListPanelInstance.SetActive(false);
            }
            else // 다른 패널이 모두 닫혀있을 때만 메인 설정 창을 닫습니다.
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