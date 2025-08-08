using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class HealthManager : MonoBehaviourPunCallbacks
{
    [Header("UI 참조")]
    [SerializeField] private Slider myHpSlider;
    [SerializeField] private Slider versusHpSlider;

    [Header("설정")]
    [SerializeField] private int maxHp = 100;

    public static HealthManager Instance;

    private Player myPlayer;
    private Player versusPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start에서는 내 플레이어 정보만 설정하고, 상대방은 기다립니다.
    void Start()
    {
        myPlayer = PhotonNetwork.LocalPlayer;
        InitializeMyHP();
        TrySetupPlayers(); // 플레이어 설정 시도
    }

    // 새로운 플레이어가 방에 들어올 때마다 Photon이 자동으로 호출하는 함수
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupPlayers(); // 새로운 플레이어가 들어왔으니 다시 설정 시도
    }

    /// <summary>
    /// 방에 2명이 모두 있는지 확인하고, 그렇다면 각 플레이어를 할당하는 함수
    /// </summary>
    private void TrySetupPlayers()
    {
        // 방에 2명이 아직 안 찼으면 아무것도 하지 않고 대기
        if (PhotonNetwork.PlayerList.Length < 2)
        {
            Debug.Log("상대방 플레이어를 기다리는 중...");
            return;
        }

        // 2명이 모두 찼으므로, 상대방 플레이어를 할당
        myPlayer = PhotonNetwork.LocalPlayer;
        versusPlayer = PhotonNetwork.PlayerListOthers[0];

        Debug.Log("플레이어 설정 완료! 내 플레이어: " + myPlayer.NickName + ", 상대: " + versusPlayer.NickName);
        UpdateAllHpBars();
    }

    // 네트워크 플레이어의 정보가 변경될 때마다 자동으로 호출되는 함수
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("HP"))
        {
            UpdateAllHpBars();
        }
    }

    public void DealDamage(Player targetPlayer, int damage)
    {
        // 상대방이 아직 설정되지 않았으면(게임 시작 전) 데미지 처리 안 함
        if (targetPlayer == null) return;

        int currentHp = (int)targetPlayer.CustomProperties["HP"];
        int newHp = Mathf.Max(0, currentHp - damage);

        Hashtable newProperties = new Hashtable();
        newProperties["HP"] = newHp;
        targetPlayer.SetCustomProperties(newProperties);
    }

    private void InitializeMyHP()
    {
        if (!myPlayer.CustomProperties.ContainsKey("HP"))
        {
            Hashtable initialProperties = new Hashtable();
            initialProperties["HP"] = maxHp;
            myPlayer.SetCustomProperties(initialProperties);
        }
    }

    private void UpdateAllHpBars()
    {
        // 내 HP 업데이트
        if (myPlayer != null && myPlayer.CustomProperties.TryGetValue("HP", out object myHp))
        {
            myHpSlider.value = (int)myHp;
        }

        // 상대 HP 업데이트
        if (versusPlayer != null && versusPlayer.CustomProperties.TryGetValue("HP", out object versusHp))
        {
            versusHpSlider.value = (int)versusHp;
        }
    }
}