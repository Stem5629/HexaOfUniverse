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
        // 각 버튼이 SettingsManager의 기능을 호출하도록 연결
        manualButton.onClick.AddListener(() => SettingsManager.Instance.ShowManual());
        yeokListButton.onClick.AddListener(() => SettingsManager.Instance.ShowYeokList());
        quitButton.onClick.AddListener(() => SettingsManager.Instance.QuitGame());

        // 닫기 버튼은 자신(SettingsPanel)을 비활성화
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}