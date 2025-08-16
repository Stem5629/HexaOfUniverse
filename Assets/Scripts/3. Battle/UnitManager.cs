using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

/// <summary>
/// 24개의 외곽 전장을 관리하고, 유닛의 소환, 전투, 파괴를 처리합니다.
/// 모든 전투 로직은 마스터 클라이언트가 계산하고, 결과만 모든 클라이언트에게 동기화됩니다.
/// </summary>
public class UnitManager : MonoBehaviourPunCallbacks
{
    [Header("UI 및 시각 효과 참조")]
    [SerializeField] private Sprite[] unitSpritesByYeok;
    [SerializeField] private Color myColor = Color.red;
    [SerializeField] private Color versusColor = Color.blue;

    [Header("유닛 정보창 UI")]
    public GameObject infoPanel;
    public TextMeshProUGUI unitTreeText;
    public TextMeshProUGUI treeCombinationText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI beginDamageText;
    public TextMeshProUGUI keepDamageText;

    // --- 데이터 ---
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
        // 마스터 클라이언트가 아니면 아무것도 하지 않고, 마스터의 지시를 기다립니다.
        if (!PhotonNetwork.IsMasterClient) return;

        // --- 1. 마스터 클라이언트가 모든 전투를 시뮬레이션하고 최종 결과를 계산합니다. ---
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

        // --- 2. 계산된 결과를 바탕으로 RPC를 전송합니다. ---
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
    /// (마스터 클라이언트 전용) 전투를 시뮬레이션하고, 변경 사항과 플레이어 데미지를 반환합니다.
    /// </summary>
    private int SimulateCombat(UnitData attacker, GameObject slot, Dictionary<GameObject, UnitData> board, List<UnitChangeInfo> changes)
    {
        int slotIndex = System.Array.IndexOf(perimeterSlots, slot);
        int damageToPlayer = 0;

        if (board.TryGetValue(slot, out UnitData defender))
        {
            // 아군 유닛 위에 재배치
            if (defender.Owner == attacker.Owner)
            {
                if (attacker.InitialDamage >= defender.InitialDamage)
                {
                    board[slot] = attacker;
                    changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
                }
            }
            // 적 유닛과 전투
            else
            {
                defender.HP -= attacker.InitialDamage;
                if (defender.HP <= 0)
                {
                    if (defender.HP < 0) // 공격자 승리
                    {
                        damageToPlayer = -defender.HP;
                        board[slot] = attacker;
                        changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
                    }
                    else // 무승부
                    {
                        board.Remove(slot);
                        changes.Add(UnitChangeInfo.Clear(slotIndex));
                    }
                }
                else // 방어자 승리
                {
                    // 방어 유닛의 HP가 변경되었으므로 이 또한 동기화합니다.
                    changes.Add(UnitChangeInfo.Update(slotIndex, defender));
                }
            }
        }
        else // 빈 칸에 배치
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

            // 슬롯 비우기
            if (change.ShouldClear)
            {
                if (unitDataOnBoard.ContainsKey(slot))
                {
                    unitDataOnBoard.Remove(slot);
                }
                Image img = slot.GetComponent<Image>();
                img.sprite = null;
                img.color = Color.clear; // 또는 기본 색상
            }
            // 슬롯 업데이트
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
        else // 튜토리얼 등
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

    // (이 아래의 함수들은 기존 코드와 동일합니다)
    #region Helper Functions
    private void OnUnitSlotClicked(GameObject clickedSlot)
    {
        if (unitDataOnBoard.TryGetValue(clickedSlot, out UnitData unitData))
        {
            infoPanel.SetActive(true);
            unitTreeText.text = unitData.YeokType.ToString();
            treeCombinationText.text = unitData.CombinationString;
            hpText.text = $"HP : {unitData.HP}";
            beginDamageText.text = $"초기데미지 : {unitData.InitialDamage}";
            keepDamageText.text = $"지속데미지 : {unitData.ContinuousDamage}";
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


// --- Photon이 네트워크로 전송할 수 있도록 데이터를 직렬화하는 헬퍼 클래스 ---
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

    // --- 직렬화/역직렬화 로직 ---
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