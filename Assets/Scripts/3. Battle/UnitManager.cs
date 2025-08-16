using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

/// <summary>
/// 24���� �ܰ� ������ �����ϰ�, ������ ��ȯ, ����, �ı��� ó���մϴ�.
/// ��� ���� ������ ������ Ŭ���̾�Ʈ�� ����ϰ�, ����� ��� Ŭ���̾�Ʈ���� ����ȭ�˴ϴ�.
/// </summary>
public class UnitManager : MonoBehaviourPunCallbacks
{
    [Header("UI �� �ð� ȿ�� ����")]
    [SerializeField] private Sprite[] unitSpritesByYeok;
    [SerializeField] private Color myColor = Color.red;
    [SerializeField] private Color versusColor = Color.blue;

    [Header("���� ����â UI")]
    public GameObject infoPanel;
    public TextMeshProUGUI unitTreeText;
    public TextMeshProUGUI treeCombinationText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI beginDamageText;
    public TextMeshProUGUI keepDamageText;

    // --- ������ ---
    private GameObject[] perimeterSlots;
    private Dictionary<GameObject, UnitData> unitDataOnBoard = new Dictionary<GameObject, UnitData>();

    public void Initialize(GameObject[] slots)
    {
        this.perimeterSlots = slots;
        unitDataOnBoard.Clear();
        if (infoPanel != null) infoPanel.SetActive(false);

        foreach (var slot in perimeterSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button == null) button = slot.AddComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnUnitSlotClicked(slot));
        }
    }

    public void SummonUnitsViaRPC(List<CompletedYeokInfo> yeoks, Player owner)
    {
        // ������ Ŭ���̾�Ʈ�� �ƴϸ� �ƹ��͵� ���� �ʰ�, �������� ���ø� ��ٸ��ϴ�.
        if (!PhotonNetwork.IsMasterClient) return;

        // --- 1. ������ Ŭ���̾�Ʈ�� ��� ������ �ùķ��̼��ϰ� ���� ����� ����մϴ�. ---
        var simulatedBoard = new Dictionary<GameObject, UnitData>(unitDataOnBoard);
        var changesToSync = new List<UnitChangeInfo>();
        int totalPlayerDamage = 0;

        Player targetPlayer = PhotonNetwork.PlayerListOthers.Length > 0 ? PhotonNetwork.PlayerListOthers[0] : null;

        foreach (var yeokInfo in yeoks)
        {
            UnitData attacker = CreateUnitDataFromYeok(yeokInfo, owner);
            int[] slotIndices = GetPerimeterIndices(yeokInfo);

            foreach (int slotIndex in slotIndices)
            {
                GameObject targetSlot = perimeterSlots[slotIndex];
                totalPlayerDamage += SimulateCombat(attacker, targetSlot, simulatedBoard, changesToSync);
            }
        }

        // --- 2. ���� ����� �������� RPC�� �����մϴ�. ---
        if (changesToSync.Count > 0)
        {
            photonView.RPC("SyncUnitChangesRPC", RpcTarget.All, UnitChangeInfo.SerializeList(changesToSync));
        }

        if (targetPlayer != null && totalPlayerDamage > 0)
        {
            HealthManager.Instance.DealDamage(targetPlayer, totalPlayerDamage);
        }
    }

    /// <summary>
    /// (������ Ŭ���̾�Ʈ ����) ������ �ùķ��̼��ϰ�, ���� ���װ� �÷��̾� �������� ��ȯ�մϴ�.
    /// </summary>
    private int SimulateCombat(UnitData attacker, GameObject slot, Dictionary<GameObject, UnitData> board, List<UnitChangeInfo> changes)
    {
        int slotIndex = System.Array.IndexOf(perimeterSlots, slot);
        int damageToPlayer = 0;

        if (board.TryGetValue(slot, out UnitData defender))
        {
            // �Ʊ� ���� ���� ���ġ
            if (defender.Owner == attacker.Owner)
            {
                if (attacker.InitialDamage >= defender.InitialDamage)
                {
                    board[slot] = attacker;
                    changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
                }
            }
            // �� ���ְ� ����
            else
            {
                defender.HP -= attacker.InitialDamage;
                if (defender.HP <= 0)
                {
                    if (defender.HP < 0) // ������ �¸�
                    {
                        damageToPlayer = -defender.HP;
                        board[slot] = attacker;
                        changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
                    }
                    else // ���º�
                    {
                        board.Remove(slot);
                        changes.Add(UnitChangeInfo.Clear(slotIndex));
                    }
                }
                else // ����� �¸�
                {
                    // ��� ������ HP�� ����Ǿ����Ƿ� �� ���� ����ȭ�մϴ�.
                    changes.Add(UnitChangeInfo.Update(slotIndex, defender));
                }
            }
        }
        else // �� ĭ�� ��ġ
        {
            damageToPlayer = attacker.InitialDamage;
            board[slot] = attacker;
            changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
        }

        return damageToPlayer;
    }

    [PunRPC]
    private void SyncUnitChangesRPC(byte[] serializedChanges)
    {
        List<UnitChangeInfo> changes = UnitChangeInfo.DeserializeList(serializedChanges);

        foreach (var change in changes)
        {
            GameObject slot = perimeterSlots[change.SlotIndex];

            // ���� ����
            if (change.ShouldClear)
            {
                if (unitDataOnBoard.ContainsKey(slot))
                {
                    unitDataOnBoard.Remove(slot);
                }
                Image img = slot.GetComponent<Image>();
                img.sprite = null;
                img.color = Color.clear; // �Ǵ� �⺻ ����
            }
            // ���� ������Ʈ
            else
            {
                UnitData newUnitData = change.GetUnitData();
                unitDataOnBoard[slot] = newUnitData;
                UpdateSlotDisplay(slot, newUnitData);
            }
        }
    }

    private void UpdateSlotDisplay(GameObject slot, UnitData unit)
    {
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];

        if (unit.Owner != null)
        {
            slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
        }
        else // Ʃ�丮�� ��
        {
            slotImage.color = unit.IsTutorialEnemy ? versusColor : myColor;
        }
    }

    public void ApplyContinuousDamage()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int totalDamageToP1 = 0;
        int totalDamageToP2 = 0;

        Player player1 = PhotonNetwork.MasterClient;
        Player player2 = PhotonNetwork.PlayerListOthers.Length > 0 ? PhotonNetwork.PlayerListOthers[0] : null;

        if (player2 == null) return;

        foreach (var entry in unitDataOnBoard)
        {
            UnitData unit = entry.Value;
            if (unit.ContinuousDamage <= 0 || unit.Owner == null) continue;

            if (unit.Owner == player1) totalDamageToP2 += unit.ContinuousDamage;
            else if (unit.Owner == player2) totalDamageToP1 += unit.ContinuousDamage;
        }

        if (totalDamageToP1 > 0) HealthManager.Instance.DealDamage(player1, totalDamageToP1);
        if (totalDamageToP2 > 0) HealthManager.Instance.DealDamage(player2, totalDamageToP2);
    }

    // (�� �Ʒ��� �Լ����� ���� �ڵ�� �����մϴ�)
    #region Helper Functions
    private void OnUnitSlotClicked(GameObject clickedSlot)
    {
        if (unitDataOnBoard.TryGetValue(clickedSlot, out UnitData unitData))
        {
            infoPanel.SetActive(true);
            unitTreeText.text = unitData.YeokType.ToString();
            treeCombinationText.text = unitData.CombinationString;
            hpText.text = $"HP : {unitData.HP}";
            beginDamageText.text = $"�ʱⵥ���� : {unitData.InitialDamage}";
            keepDamageText.text = $"���ӵ����� : {unitData.ContinuousDamage}";
        }
        else
        {
            infoPanel.SetActive(false);
        }
    }

    private UnitData CreateUnitDataFromYeok(CompletedYeokInfo yeok, Player owner)
    {
        UnitData unit = new UnitData { Owner = owner, YeokType = yeok.YeokType };
        int baseStat = yeok.BaseScore;
        int continuousStat = yeok.BonusScore;
        unit.HP = baseStat;
        unit.InitialDamage = baseStat;
        unit.ContinuousDamage = continuousStat;
        unit.CombinationString = yeok.CombinationString;
        return unit;
    }

    private int[] GetPerimeterIndices(CompletedYeokInfo yeokInfo)
    {
        int[] indices = new int[2];
        if (yeokInfo.IsHorizontal)
        {
            indices[0] = 12 + yeokInfo.LineIndex;
            indices[1] = 18 + yeokInfo.LineIndex;
        }
        else
        {
            indices[0] = yeokInfo.LineIndex;
            indices[1] = 6 + yeokInfo.LineIndex;
        }
        return indices;
    }

    public void PlaceTutorialEnemyUnit(int slotIndex, BaseTreeEnum yeokType)
    {
        GameObject targetSlot = perimeterSlots[slotIndex];
        if (targetSlot == null) return;

        UnitData enemyUnit = new UnitData
        {
            Owner = null,
            IsTutorialEnemy = true,
            YeokType = yeokType,
            HP = 1,
            InitialDamage = 1,
            ContinuousDamage = 0,
            CombinationString = "TUTORIAL"
        };

        unitDataOnBoard[targetSlot] = enemyUnit;
        UpdateSlotDisplay(targetSlot, enemyUnit);
    }
    #endregion
}


// --- Photon�� ��Ʈ��ũ�� ������ �� �ֵ��� �����͸� ����ȭ�ϴ� ���� Ŭ���� ---
public class UnitChangeInfo
{
    public int SlotIndex;
    public bool ShouldClear;
    public int OwnerActorNr;
    public int YeokType;
    public int HP;
    public int InitialDamage;
    public int ContinuousDamage;
    public string CombinationString;
    public bool IsTutorialEnemy;

    public static UnitChangeInfo Update(int slotIndex, UnitData unit)
    {
        return new UnitChangeInfo
        {
            SlotIndex = slotIndex,
            ShouldClear = false,
            OwnerActorNr = (unit.Owner != null) ? unit.Owner.ActorNumber : -1,
            YeokType = (int)unit.YeokType,
            HP = unit.HP,
            InitialDamage = unit.InitialDamage,
            ContinuousDamage = unit.ContinuousDamage,
            CombinationString = unit.CombinationString,
            IsTutorialEnemy = unit.IsTutorialEnemy
        };
    }

    public static UnitChangeInfo Clear(int slotIndex)
    {
        return new UnitChangeInfo { SlotIndex = slotIndex, ShouldClear = true };
    }

    public UnitData GetUnitData()
    {
        return new UnitData
        {
            Owner = (OwnerActorNr != -1) ? PhotonNetwork.CurrentRoom.GetPlayer(OwnerActorNr) : null,
            YeokType = (BaseTreeEnum)YeokType,
            HP = HP,
            InitialDamage = InitialDamage,
            ContinuousDamage = ContinuousDamage,
            CombinationString = CombinationString,
            IsTutorialEnemy = IsTutorialEnemy
        };
    }

    // --- ����ȭ/������ȭ ���� ---
    public static byte[] SerializeList(List<UnitChangeInfo> list)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(list.Count);
                foreach (var item in list)
                {
                    writer.Write(item.SlotIndex);
                    writer.Write(item.ShouldClear);
                    if (!item.ShouldClear)
                    {
                        writer.Write(item.OwnerActorNr);
                        writer.Write(item.YeokType);
                        writer.Write(item.HP);
                        writer.Write(item.InitialDamage);
                        writer.Write(item.ContinuousDamage);
                        writer.Write(item.CombinationString);
                        writer.Write(item.IsTutorialEnemy);
                    }
                }
            }
            return stream.ToArray();
        }
    }

    public static List<UnitChangeInfo> DeserializeList(byte[] data)
    {
        var list = new List<UnitChangeInfo>();
        using (var stream = new System.IO.MemoryStream(data))
        {
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var item = new UnitChangeInfo();
                    item.SlotIndex = reader.ReadInt32();
                    item.ShouldClear = reader.ReadBoolean();
                    if (!item.ShouldClear)
                    {
                        item.OwnerActorNr = reader.ReadInt32();
                        item.YeokType = reader.ReadInt32();
                        item.HP = reader.ReadInt32();
                        item.InitialDamage = reader.ReadInt32();
                        item.ContinuousDamage = reader.ReadInt32();
                        item.CombinationString = reader.ReadString();
                        item.IsTutorialEnemy = reader.ReadBoolean();
                    }
                    list.Add(item);
                }
            }
        }
        return list;
    }
}