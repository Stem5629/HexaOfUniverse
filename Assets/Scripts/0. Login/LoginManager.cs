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
        // 1. �ڳ� SDK �ʱ�ȭ�� ���� ��û�մϴ�.
        statusText.text = "������ ���� ��...";
        Backend.InitializeAsync(bro =>
        {
            if (bro.IsSuccess())
            {
                Debug.Log("�ڳ� SDK �ʱ�ȭ ����");
                // �ʱ�ȭ ���� ��, ����� �Է��� ���� �غ� �Ϸ�Ǿ����� �˸��ϴ�.
                statusText.text = "�α����� �ʿ��մϴ�.";
            }
            else
            {
                statusText.text = "���� �ʱ�ȭ ���� (��Ʈ��ũ Ȯ��)";
                Debug.LogError($"�ڳ� SDK �ʱ�ȭ ����: {bro.GetMessage()}");
            }
        });

        // 2. ��ư Ŭ�� �̺�Ʈ�� �״�� ������ �Ӵϴ�.
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        goToSignUpButton.onClick.AddListener(ShowSignUpPanel);
        signUpButton.onClick.AddListener(OnSignUpButtonClicked);
        backToLoginButton.onClick.AddListener(ShowLoginPanel);
        nickname_checkButton.onClick.AddListener(OnCheckNicknameButtonClicked);
        nickname_confirmButton.onClick.AddListener(OnConfirmNicknameButtonClicked);

        nickname_nicknameInput.onValueChanged.AddListener(delegate { isNicknameVerified = false; });

        // 3. ���� �� �α��� �г��� Ȱ��ȭ�մϴ�.
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
        statusText.text = "���� ����! ����� �г����� �����ּ���.";
    }
    #endregion

    #region ȸ������ ����
    public void OnSignUpButtonClicked()
    {
        if (signUp_pwInput.text != signUp_pwConfirmInput.text)
        {
            statusText.text = "��й�ȣ�� ��ġ���� �ʽ��ϴ�.";
            return;
        }

        string id = signUp_idInput.text;
        string pw = signUp_pwInput.text;

        statusText.text = "ȸ������ �õ� ��...";
        Backend.BMember.CustomSignUp(id, pw, bro => {
            if (bro.IsSuccess())
            {
                statusText.text = "ȸ������ ����! �ش� ������ �α��� ���ּ���.";
                ShowLoginPanel();
            }
            else
            {
                statusText.text = $"ȸ������ ����: {bro.GetMessage()}";
            }
        });
    }
    #endregion

    #region �α��� ����
    public void OnLoginButtonClicked()
    {
        string id = login_idInput.text;
        string pw = login_pwInput.text;

        statusText.text = "�α��� �õ� ��...";
        Backend.BMember.CustomLogin(id, pw, bro => {
            if (bro.IsSuccess())
            {
                OnLoginSuccess("�α��� ����!");
            }
            else
            {
                statusText.text = $"�α��� ����: {bro.GetMessage()}";
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

                Debug.Log($"[�α���] �ڳ� �������� ���� �г��� ��: '{nickname}'");

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
                statusText.text = "���� ���� ��ȸ�� �����߽��ϴ�.";
            }
        });
    }
    #endregion

    #region �г��� ���� ����
    public void OnCheckNicknameButtonClicked()
    {
        string nickname = nickname_nicknameInput.text;
        if (string.IsNullOrEmpty(nickname))
        {
            statusText.text = "�г����� �Է��ϼ���.";
            return;
        }

        statusText.text = "�г��� �ߺ� Ȯ�� ��...";
        Backend.BMember.CheckNicknameDuplication(nickname, bro =>
        {
            if (bro.IsSuccess())
            {
                statusText.text = "��� ������ �г����Դϴ�.";
                isNicknameVerified = true;
            }
            else
            {
                statusText.text = $"����� �� ���� �г����Դϴ�: {bro.GetMessage()}";
                isNicknameVerified = false;
            }
        });
    }

    public void OnConfirmNicknameButtonClicked()
    {
        if (!isNicknameVerified)
        {
            statusText.text = "�г��� �ߺ� Ȯ���� ���� ���ּ���.";
            return;
        }

        string nickname = nickname_nicknameInput.text;
        statusText.text = "�г��� ���� ��...";

        Backend.BMember.UpdateNickname(nickname, bro =>
        {
            if (bro.IsSuccess())
            {
                statusText.text = "�г��� ���� �Ϸ�! �κ�� �̵��մϴ�.";
                Photon.Pun.PhotonNetwork.NickName = nickname;
                NetworkManager.Instance.Connect();
            }
            else
            {
                statusText.text = $"�г��� ���� ����: {bro.GetMessage()}";
            }
        });
    }
    #endregion
}