using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// ���� ���� ����, �κ� ����, 1v1 ��ġ����ŷ�� '����'�� �����ϴ� �ٽ� ��ũ��Ʈ�Դϴ�.
/// UI�� ���� �������� ������, ���� �ٲ� �ı����� �ʽ��ϴ�.
/// </summary>
public class NetworkManager : MonoBehaviourPunCallbacks
{
    // ���� ��Ī ������ ���θ� �ܺ� UI ��ũ��Ʈ�� �о �� �ֵ��� public���� ����
    public bool IsMatching { get; private set; }

    // �̱��� ����
    public static NetworkManager Instance;

    private void Awake()
    {
        // NetworkManager�� �ߺ� �����Ǵ� ���� ����
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

        // ������ Ŭ���̾�Ʈ�� ���� �ε��ϸ�, �ٸ� Ŭ���̾�Ʈ�鵵 �ڵ����� ���󰡵��� ����
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// LoginManager �� �ܺο��� ���� ���� ������ ������ �� ȣ���ϴ� �Լ�
    /// </summary>
    public void Connect()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("���� ������ ������ �õ��մϴ�...");
            PhotonNetwork.GameVersion = "1.0"; // ���� ������ ����ϴ� ���� �����ϴ�.
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    #region ���� �ݹ� �Լ���

    public override void OnConnectedToMaster()
    {
        Debug.Log("������ ���� ���� �Ϸ�. �κ� �����մϴ�...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("�κ� ���� ����. �κ� ������ �̵��մϴ�.");

        // --- �� ���� �߰��ϼ��� ---
        // "LobbyScene"���� ���� ��ȯ�϶�� ����Դϴ�.
        PhotonNetwork.LoadLevel("LobbyScene");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("���� ������ ���� ����, ���ο� ���� �����մϴ�.");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"�뿡 �����߽��ϴ�. ���� �� �ο�: {PhotonNetwork.CurrentRoom.PlayerCount}��");
        CheckForGameStart();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName}���� �����߽��ϴ�. ���� �� �ο�: {PhotonNetwork.CurrentRoom.PlayerCount}��");
        CheckForGameStart();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("�濡�� �������ϴ�.");
        IsMatching = false; // ��Ī ���� �ʱ�ȭ
    }

    #endregion

    #region ��ġ����ŷ ����

    /// <summary>
    /// LobbyUIManager���� '���� ã��/���' ��ư�� Ŭ������ �� ȣ��� �Լ�
    /// </summary>
    public void OnMatchButtonClicked()
    {
        // --- Pro Tip: �κ� Ȯ���� �������� ���� ��Ī�� �õ��ϵ��� ������ġ�� �߰��մϴ�. ---
        if (!PhotonNetwork.InLobby)
        {
            Debug.LogWarning("���� �κ� �������� �ʾ� ��Ī�� ������ �� �����ϴ�.");
            return;
        }

        IsMatching = !IsMatching; // ���¸� ���� (false -> true, true -> false)

        if (IsMatching)
        {
            Debug.Log("��Ī�� �����մϴ�... (���� �� ���� �õ�)");

            // --- ������ �κ� ---
            // 'JoinRandomOrCreateRoom' ��� 'JoinRandomRoom'�� ����Ͽ�, �� ������ �õ��մϴ�.
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            Debug.Log("��Ī�� ����մϴ�.");
            PhotonNetwork.LeaveRoom();
        }
    }

    private void CheckForGameStart()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("2���� ��� �𿴽��ϴ�. ���� ��װ� �ε� ������ �̵��մϴ�.");

            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            PhotonNetwork.LoadLevel("LoadingScene");
        }
    }

    #endregion
}