// ResultSceneManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // ���� ����� ���� �߰�

public class ResultSceneManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI myNicknameText;
    [SerializeField] private TextMeshProUGUI opponentNicknameText;
    [SerializeField] private Button lobbyButton;

    void Start()
    {
        // ResultData�� ����� ��� ����� UI�� ǥ��
        myNicknameText.text = ResultData.MyNickname;
        opponentNicknameText.text = ResultData.OpponentNickname;

        switch (ResultData.Outcome)
        {
            case ResultData.GameOutcome.Victory:
                resultText.text = "�¸�";
                resultText.color = Color.blue;
                break;
            case ResultData.GameOutcome.Defeat:
                resultText.text = "�й�";
                resultText.color = Color.red;
                break;
            case ResultData.GameOutcome.Draw:
                resultText.text = "���º�";
                resultText.color = Color.gray;
                break;
        }

        lobbyButton.onClick.AddListener(GoToLobby);
    }

    private void GoToLobby()
    {
        // ���� ������ ���� NetworkManager�� ó���ϵ���, ���⼭�� �κ� �� �ε常 ��û
        PhotonNetwork.LoadLevel("LobbyScene");
    }
}