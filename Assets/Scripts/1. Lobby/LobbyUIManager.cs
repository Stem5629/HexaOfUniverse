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

    [Header("튜토리얼 버튼")]
    [SerializeField] private Button tutorialButton;

    void Start()
    {
        // NetworkManager의 OnMatchButtonClicked 함수를 직접 호출하도록 리스너 연결
        matchButton.onClick.AddListener(() => NetworkManager.Instance.OnMatchButtonClicked());

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(StartTutorial);
        }
        // 결과창 등에서 로비로 돌아왔을 때, 혹시라도 방에 남아있는 상태라면 방을 떠나도록 처리
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    private void StartTutorial()
    {
        // "TutorialScene"으로 씬을 전환합니다.
        // 튜토리얼은 혼자 하는 것이므로 PhotonNetwork.LoadLevel을 사용할 필요가 없습니다.
        SceneManager.LoadScene("TutorialScene");
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
                tutorialButton.interactable = false; // 매칭 중에는 튜토리얼 못하게 막기
            }
            else
            {
                // 로비 대기 중일 때 UI
                matchButtonText.text = "대전 찾기";
                queueStatusText.gameObject.SetActive(false);
                tutorialButton.interactable = true; // 대기 중에는 튜토리얼 가능
            }
            matchButton.gameObject.SetActive(true);
            tutorialButton.gameObject.SetActive(true); // 로비에 들어오면 튜토리얼 버튼 활성화
        }
        else
        {
            // 로비에 아직 접속하지 않았을 때
            matchButton.gameObject.SetActive(false);
            tutorialButton.gameObject.SetActive(false); // 로비 접속 전에는 튜토리얼 버튼 비활성화
            queueStatusText.gameObject.SetActive(true);
            queueStatusText.text = "서버에 연결하는 중...";
        }
    }
}