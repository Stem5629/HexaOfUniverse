// SettingsManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField]
    private GameObject settingsPanelPrefab; // 1�ܰ迡�� ���� ���� â ������
    private GameObject settingsPanelInstance; // ������ ���� â �ν��Ͻ�

    // --- �� ��ư�� ������ ��ɵ� ---
    [SerializeField] private GameObject manualPanelPrefab; // ���� �г� ������
    private GameObject manualPanelInstance;

    [SerializeField] private GameObject yeokListPanelPrefab; // �� �϶� �г� ������ (���� ������ ��)
    private GameObject yeokListPanelInstance;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �� ������Ʈ�� ���� �ٲ� �ı����� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // ESC Ű�� ������ ��
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    public void ToggleSettingsPanel()
    {
        // ���� â�� ���� ������ ���������κ��� ����
        if (settingsPanelInstance == null)
        {
            // Canvas�� ã�� �� �ڽ����� �����ؾ� UI�� ����� ����
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                settingsPanelInstance = Instantiate(settingsPanelPrefab, canvas.transform);
            }
        }
        else // �̹� �ִٸ� ���� ��
        {
            settingsPanelInstance.SetActive(!settingsPanelInstance.activeSelf);
        }
    }

    // --- ��ư ��� ���� ---
    public void ShowManual()
    {
        // ���� ��� (ManualManager�� �����ϰ� ����)
        if (manualPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            manualPanelInstance = Instantiate(manualPanelPrefab, canvas.transform);
        }
        manualPanelInstance.SetActive(true);
    }

    public void ShowYeokList()
    {
        // �� �϶� �г��� ���� ������ ���������κ��� ����
        if (yeokListPanelInstance == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                yeokListPanelInstance = Instantiate(yeokListPanelPrefab, canvas.transform);
            }
        }

        // ������ �г��� Ȱ��ȭ�Ͽ� ������
        yeokListPanelInstance.SetActive(true);
    }

    public void QuitGame()
    {
        // �����Ϳ����� �������� ������, ����� ���ӿ����� �����
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}