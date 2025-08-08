using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private Button matchButton;
    [SerializeField] private TextMeshProUGUI matchButtonText;
    [SerializeField] private TextMeshProUGUI queueStatusText;

    void Start()
    {
        // NetworkManager�� OnMatchButtonClicked �Լ��� ���� ȣ���ϵ��� ������ ����
        matchButton.onClick.AddListener(() => NetworkManager.Instance.OnMatchButtonClicked());
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
            }
            else
            {
                // �κ� ��� ���� �� UI
                matchButtonText.text = "���� ã��";
                queueStatusText.gameObject.SetActive(false);
            }
            matchButton.gameObject.SetActive(true);
        }
        else
        {
            // �κ� ���� �������� �ʾ��� ��
            matchButton.gameObject.SetActive(false);
            queueStatusText.gameObject.SetActive(true);
            queueStatusText.text = "������ �����ϴ� ��...";
        }
    }
}