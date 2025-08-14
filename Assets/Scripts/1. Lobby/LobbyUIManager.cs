using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private Button matchButton;
    [SerializeField] private TextMeshProUGUI matchButtonText;
    [SerializeField] private TextMeshProUGUI queueStatusText;

    [Header("Ʃ�丮�� ��ư")]
    [SerializeField] private Button tutorialButton;

    void Start()
    {
        // NetworkManager�� OnMatchButtonClicked �Լ��� ���� ȣ���ϵ��� ������ ����
        matchButton.onClick.AddListener(() => NetworkManager.Instance.OnMatchButtonClicked());

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(StartTutorial);
        }
        // ���â ��� �κ�� ���ƿ��� ��, Ȥ�ö� �濡 �����ִ� ���¶�� ���� �������� ó��
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    private void StartTutorial()
    {
        // "TutorialScene"���� ���� ��ȯ�մϴ�.
        // Ʃ�丮���� ȥ�� �ϴ� ���̹Ƿ� PhotonNetwork.LoadLevel�� ����� �ʿ䰡 �����ϴ�.
        SceneManager.LoadScene("TutorialScene");
    }

    // �� ������ NetworkManager�� ���¸� Ȯ���Ͽ� UI�� ������Ʈ
    void Update()
    {
        if (NetworkManager.Instance == null) return;

        if (Photon.Pun.PhotonNetwork.InLobby)
        {
            if (NetworkManager.Instance.IsMatching)
            {
                // ��Ī ���� �� UI
                matchButtonText.text = "��Ī ���";
                queueStatusText.gameObject.SetActive(true);
                tutorialButton.interactable = false; // ��Ī �߿��� Ʃ�丮�� ���ϰ� ����
            }
            else
            {
                // �κ� ��� ���� �� UI
                matchButtonText.text = "���� ã��";
                queueStatusText.gameObject.SetActive(false);
                tutorialButton.interactable = true; // ��� �߿��� Ʃ�丮�� ����
            }
            matchButton.gameObject.SetActive(true);
            tutorialButton.gameObject.SetActive(true); // �κ� ������ Ʃ�丮�� ��ư Ȱ��ȭ
        }
        else
        {
            // �κ� ���� �������� �ʾ��� ��
            matchButton.gameObject.SetActive(false);
            tutorialButton.gameObject.SetActive(false); // �κ� ���� ������ Ʃ�丮�� ��ư ��Ȱ��ȭ
            queueStatusText.gameObject.SetActive(true);
            queueStatusText.text = "������ �����ϴ� ��...";
        }
    }
}