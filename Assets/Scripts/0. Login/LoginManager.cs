using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// LoginScene�� UI�� �帧�� �����մϴ�.
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject exitAlertPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;

    // TODO: �ڳ� �α��ο� �ʿ��� ID/PW InputField ���� ���⿡ �����ϼ���.

    private void Start()
    {
        // UI ��ư�鿡 �� ����� ����
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        exitButton.onClick.AddListener(() => exitAlertPanel.SetActive(true));
        exitYesButton.onClick.AddListener(ExitGame);
        exitNoButton.onClick.AddListener(() => exitAlertPanel.SetActive(false));

        exitAlertPanel.SetActive(false); // ���� �ÿ��� �׻� ���� Ȯ�� â�� ����
    }

    /// <summary>
    /// '�α���' ��ư�� ������ �� ȣ��� �Լ�
    /// </summary>
    private void OnLoginButtonClicked()
    {
        // TODO: ���⿡ '�ڳ�' ���� �α��� ��û �� ����/���� ó�� ������ �����ϼ���.
        // �Ʒ� �ڵ�� �α����� '����'�ߴٰ� �����ϰ� ����˴ϴ�.

        Debug.Log("�α��� ����! LobbyScene���� �̵��մϴ�.");
        SceneManager.LoadScene("LobbyScene");
    }

    private void ExitGame()
    {
        Debug.Log("������ �����մϴ�.");
        Application.Quit();
    }
}