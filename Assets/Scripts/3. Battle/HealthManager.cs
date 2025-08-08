using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class HealthManager : MonoBehaviourPunCallbacks
{
    [Header("UI ����")]
    [SerializeField] private Slider myHpSlider;
    [SerializeField] private Slider versusHpSlider;

    [Header("����")]
    [SerializeField] private int maxHp = 100;

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
        myPlayer = PhotonNetwork.LocalPlayer;
        InitializeMyHP();
        TrySetupPlayers(); // �÷��̾� ���� �õ�
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
        // ������ ���� �������� �ʾ�����(���� ���� ��) ������ ó�� �� ��
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
        // �� HP ������Ʈ
        if (myPlayer != null && myPlayer.CustomProperties.TryGetValue("HP", out object myHp))
        {
            myHpSlider.value = (int)myHp;
        }

        // ��� HP ������Ʈ
        if (versusPlayer != null && versusPlayer.CustomProperties.TryGetValue("HP", out object versusHp))
        {
            versusHpSlider.value = (int)versusHp;
        }
    }
}