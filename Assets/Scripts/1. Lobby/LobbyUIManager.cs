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
        // NetworkManager의 OnMatchButtonClicked 함수를 직접 호출하도록 리스너 연결
        matchButton.onClick.AddListener(() => NetworkManager.Instance.OnMatchButtonClicked());
    }

    // 매 프레임 NetworkManager의 상태를 확인하여 UI를 업데이트
    void Update()
    {
        if (NetworkManager.Instance == null) return;

        if (Photon.Pun.PhotonNetwork.InLobby)
        {
            if (NetworkManager.Instance.IsMatching)
            {
                // 매칭 중일 때 UI
                matchButtonText.text = "매칭 취소";
                queueStatusText.gameObject.SetActive(true);
            }
            else
            {
                // 로비 대기 중일 때 UI
                matchButtonText.text = "대전 찾기";
                queueStatusText.gameObject.SetActive(false);
            }
            matchButton.gameObject.SetActive(true);
        }
        else
        {
            // 로비에 아직 접속하지 않았을 때
            matchButton.gameObject.SetActive(false);
            queueStatusText.gameObject.SetActive(true);
            queueStatusText.text = "서버에 연결하는 중...";
        }
    }
}