using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class HealthManager : MonoBehaviourPunCallbacks
{
    [Header("UI ����")]
    [SerializeField] private Slider myHpSlider;
    [SerializeField] private Slider versusHpSlider;
    [SerializeField] private TextMeshProUGUI myHpText;
    [SerializeField] private TextMeshProUGUI versusHpText;

    // --- �Ʒ� �� ���� �߰��ϼ��� ---
    [SerializeField] private TextMeshProUGUI myNicknameText;
    [SerializeField] private TextMeshProUGUI versusNicknameText;

    [Header("����")]
    [SerializeField] private int maxHp = 1000;

    public static HealthManager Instance;

    private Player myPlayer;
    private Player versusPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start������ �� �÷��̾� ������ �����ϰ�, ������ ��ٸ��ϴ�.
    void Start()
    {
        myHpSlider.maxValue = maxHp;
        versusHpSlider.maxValue = maxHp;

        myPlayer = PhotonNetwork.LocalPlayer;
        InitializeMyHP();
        TrySetupPlayers();
    }

    // ���ο� �÷��̾ �濡 ���� ������ Photon�� �ڵ����� ȣ���ϴ� �Լ�
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupPlayers(); // ���ο� �÷��̾ �������� �ٽ� ���� �õ�
    }

    /// <summary>
    /// �濡 2���� ��� �ִ��� Ȯ���ϰ�, �׷��ٸ� �� �÷��̾ �Ҵ��ϴ� �Լ�
    /// </summary>
    private void TrySetupPlayers()
    {
        // �濡 2���� ���� �� á���� �ƹ��͵� ���� �ʰ� ���
        if (PhotonNetwork.PlayerList.Length < 2)
        {
            Debug.Log("���� �÷��̾ ��ٸ��� ��...");
            return;
        }

        // 2���� ��� á���Ƿ�, ���� �÷��̾ �Ҵ�
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

        Debug.Log("�÷��̾� ���� �Ϸ�! �� �÷��̾�: " + myPlayer.NickName + ", ���: " + versusPlayer.NickName);
        UpdateAllHpBars();
    }

    // ��Ʈ��ũ �÷��̾��� ������ ����� ������ �ڵ����� ȣ��Ǵ� �Լ�
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

            // --- ������ �κ� ---

            // 1. ������Ʈ�� ���ο� HP ���� �غ��մϴ�.
            Hashtable newProperties = new Hashtable();
            newProperties["HP"] = newHp;

            // 2. "���� ������ HP�� ���� �˰� �ִ� currentHp�� ���� ������Ʈ ����" ��� ������ �غ��մϴ�.
            Hashtable expectedProperties = new Hashtable();
            expectedProperties["HP"] = currentHp;

            // 3. ����(expectedProperties)�� �Բ� ���� �����ϰ� ������Ʈ�� �õ��մϴ�.
            targetPlayer.SetCustomProperties(newProperties, expectedProperties);
        }
        else
        {
            Debug.LogError($"{targetPlayer.NickName}���� HP �Ӽ��� ã�� �� ���� �������� ó���� �� �����ϴ�.");
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
        // �� HP ������Ʈ
        if (myPlayer != null && myPlayer.CustomProperties.TryGetValue("HP", out object myHpObj))
        {
            int myCurrentHp = (int)myHpObj;
            myHpSlider.value = myCurrentHp;
            myHpText.text = $"{myCurrentHp} / {maxHp}"; // �г��� �κ� ����
        }

        // ��� HP ������Ʈ
        if (versusPlayer != null && versusPlayer.CustomProperties.TryGetValue("HP", out object versusHpObj))
        {
            int versusCurrentHp = (int)versusHpObj;
            versusHpSlider.value = versusCurrentHp;
            versusHpText.text = $"{versusCurrentHp} / {maxHp}"; // �г��� �κ� ����
        }
    }
}