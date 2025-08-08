using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// LoginScene의 UI와 흐름을 관리합니다.
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject exitAlertPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;

    // TODO: 뒤끝 로그인에 필요한 ID/PW InputField 등을 여기에 연결하세요.

    private void Start()
    {
        // UI 버튼들에 각 기능을 연결
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        exitButton.onClick.AddListener(() => exitAlertPanel.SetActive(true));
        exitYesButton.onClick.AddListener(ExitGame);
        exitNoButton.onClick.AddListener(() => exitAlertPanel.SetActive(false));

        exitAlertPanel.SetActive(false); // 시작 시에는 항상 종료 확인 창을 꺼둠
    }

    /// <summary>
    /// '로그인' 버튼을 눌렀을 때 호출될 함수
    /// </summary>
    private void OnLoginButtonClicked()
    {
        // TODO: 여기에 '뒤끝' 서버 로그인 요청 및 성공/실패 처리 로직을 구현하세요.
        // 아래 코드는 로그인이 '성공'했다고 가정하고 실행됩니다.

        Debug.Log("로그인 성공! LobbyScene으로 이동합니다.");
        SceneManager.LoadScene("LobbyScene");
    }

    private void ExitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }
}