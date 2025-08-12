using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class HealthManager : MonoBehaviourPunCallbacks
{
    [Header("UI 참조")]
    [SerializeField] private Slider myHpSlider;
    [SerializeField] private Slider versusHpSlider;
    [SerializeField] private TextMeshProUGUI myHpText;
    [SerializeField] private TextMeshProUGUI versusHpText;

    // --- 아래 두 줄을 추가하세요 ---
    [SerializeField] private TextMeshProUGUI myNicknameText;
    [SerializeField] private TextMeshProUGUI versusNicknameText;

    [Header("설정")]
    [SerializeField] private int maxHp = 1000;

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
        myHpSlider.maxValue = maxHp;
        versusHpSlider.maxValue = maxHp;

        myPlayer = PhotonNetwork.LocalPlayer;
        InitializeMyHP();
        TrySetupPlayers();
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

        if (myNicknameText != null)
        {
            myNicknameText.text = myPlayer.NickName;
        }
        if (versusNicknameText != null)
        {
            versusNicknameText.text = versusPlayer.NickName;
        }

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
        if (targetPlayer == null || damage <= 0) return;

        if (targetPlayer.CustomProperties.TryGetValue("HP", out object hpObj))
        {
            int currentHp = (int)hpObj;
            int newHp = Mathf.Max(0, currentHp - damage);

            // --- 수정된 부분 ---

            // 1. 업데이트할 새로운 HP 값을 준비합니다.
            Hashtable newProperties = new Hashtable();
            newProperties["HP"] = newHp;

            // 2. "현재 서버의 HP가 내가 알고 있는 currentHp일 때만 업데이트 해줘" 라는 조건을 준비합니다.
            Hashtable expectedProperties = new Hashtable();
            expectedProperties["HP"] = currentHp;

            // 3. 조건(expectedProperties)을 함께 보내 안전하게 업데이트를 시도합니다.
            targetPlayer.SetCustomProperties(newProperties, expectedProperties);
        }
        else
        {
            Debug.LogError($"{targetPlayer.NickName}님의 HP 속성을 찾을 수 없어 데미지를 처리할 수 없습니다.");
        }
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
        if (myPlayer != null && myPlayer.CustomProperties.TryGetValue("HP", out object myHpObj))
        {
            int myCurrentHp = (int)myHpObj;
            myHpSlider.value = myCurrentHp;
            myHpText.text = $"{myCurrentHp} / {maxHp}"; // 닉네임 부분 삭제
        }

        // 상대 HP 업데이트
        if (versusPlayer != null && versusPlayer.CustomProperties.TryGetValue("HP", out object versusHpObj))
        {
            int versusCurrentHp = (int)versusHpObj;
            versusHpSlider.value = versusCurrentHp;
            versusHpText.text = $"{versusCurrentHp} / {maxHp}"; // 닉네임 부분 삭제
        }
    }
}