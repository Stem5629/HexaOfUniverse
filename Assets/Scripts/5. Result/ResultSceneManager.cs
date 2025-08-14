// ResultSceneManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // 포톤 사용을 위해 추가

public class ResultSceneManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI myNicknameText;
    [SerializeField] private TextMeshProUGUI opponentNicknameText;
    [SerializeField] private Button lobbyButton;

    void Start()
    {
        // ResultData에 저장된 경기 결과를 UI에 표시
        myNicknameText.text = ResultData.MyNickname;
        opponentNicknameText.text = ResultData.OpponentNickname;

        switch (ResultData.Outcome)
        {
            case ResultData.GameOutcome.Victory:
                resultText.text = "승리";
                resultText.color = Color.blue;
                break;
            case ResultData.GameOutcome.Defeat:
                resultText.text = "패배";
                resultText.color = Color.red;
                break;
            case ResultData.GameOutcome.Draw:
                resultText.text = "무승부";
                resultText.color = Color.gray;
                break;
        }

        lobbyButton.onClick.AddListener(GoToLobby);
    }

    private void GoToLobby()
    {
        // 방을 떠나는 것은 NetworkManager가 처리하도록, 여기서는 로비 씬 로드만 요청
        PhotonNetwork.LoadLevel("LobbyScene");
    }
}