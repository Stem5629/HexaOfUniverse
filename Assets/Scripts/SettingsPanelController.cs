// SettingsPanelController.cs
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private Button manualButton;
    [SerializeField] private Button yeokListButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button closeButton;

    void Start()
    {
        // �� ��ư�� SettingsManager�� ����� ȣ���ϵ��� ����
        manualButton.onClick.AddListener(() => SettingsManager.Instance.ShowManual());
        yeokListButton.onClick.AddListener(() => SettingsManager.Instance.ShowYeokList());
        quitButton.onClick.AddListener(() => SettingsManager.Instance.QuitGame());

        // �ݱ� ��ư�� �ڽ�(SettingsPanel)�� ��Ȱ��ȭ
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}