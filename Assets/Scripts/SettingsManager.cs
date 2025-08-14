// SettingsManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField]
    private GameObject settingsPanelPrefab; // 1단계에서 만든 설정 창 프리팹
    private GameObject settingsPanelInstance; // 생성된 설정 창 인스턴스

    // --- 각 버튼에 연결할 기능들 ---
    [SerializeField] private GameObject manualPanelPrefab; // 설명서 패널 프리팹
    private GameObject manualPanelInstance;

    [SerializeField] private GameObject yeokListPanelPrefab; // 역 일람 패널 프리팹 (새로 만들어야 함)
    private GameObject yeokListPanelInstance;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 이 오브젝트는 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // ESC 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    public void ToggleSettingsPanel()
    {
        // 설정 창이 아직 없으면 프리팹으로부터 생성
        if (settingsPanelInstance == null)
        {
            // Canvas를 찾아 그 자식으로 생성해야 UI가 제대로 보임
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                settingsPanelInstance = Instantiate(settingsPanelPrefab, canvas.transform);
            }
        }
        else // 이미 있다면 끄고 켬
        {
            settingsPanelInstance.SetActive(!settingsPanelInstance.activeSelf);
        }
    }

    // --- 버튼 기능 구현 ---
    public void ShowManual()
    {
        // 설명서 기능 (ManualManager와 유사하게 구현)
        if (manualPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            manualPanelInstance = Instantiate(manualPanelPrefab, canvas.transform);
        }
        manualPanelInstance.SetActive(true);
    }

    public void ShowYeokList()
    {
        // 역 일람 패널이 아직 없으면 프리팹으로부터 생성
        if (yeokListPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                yeokListPanelInstance = Instantiate(yeokListPanelPrefab, canvas.transform);
            }
        }

        // 생성된 패널을 활성화하여 보여줌
        yeokListPanelInstance.SetActive(true);
    }

    public void QuitGame()
    {
        // 에디터에서는 동작하지 않지만, 빌드된 게임에서는 종료됨
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}