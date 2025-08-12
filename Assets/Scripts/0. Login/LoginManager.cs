using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BackEnd;

public class LoginManager : MonoBehaviour
{
    [Header("Login Panel")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField login_idInput;
    [SerializeField] private TMP_InputField login_pwInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToSignUpButton;

    [Header("Sign Up Panel")]
    [SerializeField] private GameObject signUpPanel;
    [SerializeField] private TMP_InputField signUp_idInput;
    [SerializeField] private TMP_InputField signUp_pwInput;
    [SerializeField] private TMP_InputField signUp_pwConfirmInput;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button backToLoginButton;

    [Header("Nickname Panel")]
    [SerializeField] private GameObject nicknamePanel;
    [SerializeField] private TMP_InputField nickname_nicknameInput;
    [SerializeField] private Button nickname_checkButton;
    [SerializeField] private Button nickname_confirmButton;

    [Header("Common UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    private bool isNicknameVerified = false;

    #region Unity Lifecycle & Panel Control
    void Start()
    {
        // 1. 뒤끝 SDK 초기화를 먼저 요청합니다.
        statusText.text = "서버에 연결 중...";
        Backend.InitializeAsync(bro =>
        {
            if (bro.IsSuccess())
            {
                Debug.Log("뒤끝 SDK 초기화 성공");
                // 초기화 성공 시, 사용자 입력을 받을 준비가 완료되었음을 알립니다.
                statusText.text = "로그인이 필요합니다.";
            }
            else
            {
                statusText.text = "서버 초기화 실패 (네트워크 확인)";
                Debug.LogError($"뒤끝 SDK 초기화 실패: {bro.GetMessage()}");
            }
        });

        // 2. 버튼 클릭 이벤트는 그대로 연결해 둡니다.
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        goToSignUpButton.onClick.AddListener(ShowSignUpPanel);
        signUpButton.onClick.AddListener(OnSignUpButtonClicked);
        backToLoginButton.onClick.AddListener(ShowLoginPanel);
        nickname_checkButton.onClick.AddListener(OnCheckNicknameButtonClicked);
        nickname_confirmButton.onClick.AddListener(OnConfirmNicknameButtonClicked);

        nickname_nicknameInput.onValueChanged.AddListener(delegate { isNicknameVerified = false; });

        // 3. 시작 시 로그인 패널을 활성화합니다.
        ShowLoginPanel();
    }

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
        nicknamePanel.SetActive(false);
        statusText.text = "";
    }

    private void ShowSignUpPanel()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
        nicknamePanel.SetActive(false);
        statusText.text = "";
    }

    private void ShowNicknamePanel()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(false);
        nicknamePanel.SetActive(true);
        statusText.text = "최초 접속! 사용할 닉네임을 정해주세요.";
    }
    #endregion

    #region 회원가입 로직
    public void OnSignUpButtonClicked()
    {
        if (signUp_pwInput.text != signUp_pwConfirmInput.text)
        {
            statusText.text = "비밀번호가 일치하지 않습니다.";
            return;
        }

        string id = signUp_idInput.text;
        string pw = signUp_pwInput.text;

        statusText.text = "회원가입 시도 중...";
        Backend.BMember.CustomSignUp(id, pw, bro => {
            if (bro.IsSuccess())
            {
                statusText.text = "회원가입 성공! 해당 정보로 로그인 해주세요.";
                ShowLoginPanel();
            }
            else
            {
                statusText.text = $"회원가입 실패: {bro.GetMessage()}";
            }
        });
    }
    #endregion

    #region 로그인 로직
    public void OnLoginButtonClicked()
    {
        string id = login_idInput.text;
        string pw = login_pwInput.text;

        statusText.text = "로그인 시도 중...";
        Backend.BMember.CustomLogin(id, pw, bro => {
            if (bro.IsSuccess())
            {
                OnLoginSuccess("로그인 성공!");
            }
            else
            {
                statusText.text = $"로그인 실패: {bro.GetMessage()}";
            }
        });
    }

    private void OnLoginSuccess(string message)
    {
        statusText.text = message;

        Backend.BMember.GetUserInfo(userInfoBro =>
        {
            if (userInfoBro.IsSuccess())
            {
                string nickname = userInfoBro.GetReturnValuetoJSON()["row"]["nickname"]?.ToString();

                Debug.Log($"[로그인] 뒤끝 서버에서 받은 닉네임 값: '{nickname}'");

                if (string.IsNullOrWhiteSpace(nickname))
                {
                    ShowNicknamePanel();
                }
                else
                {
                    Photon.Pun.PhotonNetwork.NickName = nickname;
                    NetworkManager.Instance.Connect();
                }
            }
            else
            {
                statusText.text = "유저 정보 조회에 실패했습니다.";
            }
        });
    }
    #endregion

    #region 닉네임 설정 로직
    public void OnCheckNicknameButtonClicked()
    {
        string nickname = nickname_nicknameInput.text;
        if (string.IsNullOrEmpty(nickname))
        {
            statusText.text = "닉네임을 입력하세요.";
            return;
        }

        statusText.text = "닉네임 중복 확인 중...";
        Backend.BMember.CheckNicknameDuplication(nickname, bro =>
        {
            if (bro.IsSuccess())
            {
                statusText.text = "사용 가능한 닉네임입니다.";
                isNicknameVerified = true;
            }
            else
            {
                statusText.text = $"사용할 수 없는 닉네임입니다: {bro.GetMessage()}";
                isNicknameVerified = false;
            }
        });
    }

    public void OnConfirmNicknameButtonClicked()
    {
        if (!isNicknameVerified)
        {
            statusText.text = "닉네임 중복 확인을 먼저 해주세요.";
            return;
        }

        string nickname = nickname_nicknameInput.text;
        statusText.text = "닉네임 설정 중...";

        Backend.BMember.UpdateNickname(nickname, bro =>
        {
            if (bro.IsSuccess())
            {
                statusText.text = "닉네임 설정 완료! 로비로 이동합니다.";
                Photon.Pun.PhotonNetwork.NickName = nickname;
                NetworkManager.Instance.Connect();
            }
            else
            {
                statusText.text = $"닉네임 설정 실패: {bro.GetMessage()}";
            }
        });
    }
    #endregion
}