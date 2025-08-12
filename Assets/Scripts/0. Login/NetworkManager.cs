using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 포톤 서버 접속, 로비 입장, 1v1 매치메이킹의 '상태'를 관리하는 핵심 스크립트입니다.
/// UI를 직접 제어하지 않으며, 씬이 바뀌어도 파괴되지 않습니다.
/// </summary>
public class NetworkManager : MonoBehaviourPunCallbacks
{
    // 현재 매칭 중인지 여부를 외부 UI 스크립트가 읽어갈 수 있도록 public으로 제공
    public bool IsMatching { get; private set; }

    // 싱글톤 패턴
    public static NetworkManager Instance;

    private void Awake()
    {
        // NetworkManager가 중복 생성되는 것을 방지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 마스터 클라이언트가 씬을 로드하면, 다른 클라이언트들도 자동으로 따라가도록 설정
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// LoginManager 등 외부에서 포톤 서버 접속을 시작할 때 호출하는 함수
    /// </summary>
    public void Connect()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("포톤 서버에 접속을 시도합니다...");
            PhotonNetwork.GameVersion = "1.0"; // 게임 버전을 명시하는 것이 좋습니다.
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    #region 포톤 콜백 함수들

    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 접속 완료. 로비에 참여합니다...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 접속 성공. 로비 씬으로 이동합니다.");

        // --- 이 줄을 추가하세요 ---
        // "LobbyScene"으로 씬을 전환하라는 명령입니다.
        PhotonNetwork.LoadLevel("LobbyScene");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("참여 가능한 방이 없어, 새로운 방을 생성합니다.");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"룸에 참가했습니다. 현재 방 인원: {PhotonNetwork.CurrentRoom.PlayerCount}명");
        CheckForGameStart();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName}님이 참가했습니다. 현재 방 인원: {PhotonNetwork.CurrentRoom.PlayerCount}명");
        CheckForGameStart();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방에서 나갔습니다.");
        IsMatching = false; // 매칭 상태 초기화
    }

    #endregion

    #region 매치메이킹 로직

    /// <summary>
    /// LobbyUIManager에서 '대전 찾기/취소' 버튼을 클릭했을 때 호출될 함수
    /// </summary>
    public void OnMatchButtonClicked()
    {
        // --- Pro Tip: 로비에 확실히 접속했을 때만 매칭을 시도하도록 안전장치를 추가합니다. ---
        if (!PhotonNetwork.InLobby)
        {
            Debug.LogWarning("아직 로비에 접속하지 않아 매칭을 시작할 수 없습니다.");
            return;
        }

        IsMatching = !IsMatching; // 상태를 반전 (false -> true, true -> false)

        if (IsMatching)
        {
            Debug.Log("매칭을 시작합니다... (랜덤 룸 참여 시도)");

            // --- 수정된 부분 ---
            // 'JoinRandomOrCreateRoom' 대신 'JoinRandomRoom'을 사용하여, 방 참여만 시도합니다.
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            Debug.Log("매칭을 취소합니다.");
            PhotonNetwork.LeaveRoom();
        }
    }

    private void CheckForGameStart()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("2명이 모두 모였습니다. 방을 잠그고 로딩 씬으로 이동합니다.");

            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            PhotonNetwork.LoadLevel("LoadingScene");
        }
    }

    #endregion
}