using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

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

    // --- BUG FIX 1: 데미지 적용 대상 오류 수정 ---
    public void SummonUnitsViaRPC(List<CompletedYeokInfo> yeoks, Player owner)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var simulatedBoard = new Dictionary<GameObject, UnitData>(unitDataOnBoard);
        var changesToSync = new List<UnitChangeInfo>();
        int totalPlayerDamage = 0;

        // --- 수정된 부분: 공격받을 플레이어를 정확히 찾습니다 ---
        Player targetPlayer = null;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p != owner)
            {
                targetPlayer = p;
                break;
            }
        }

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

        if (changesToSync.Count > 0)
        {
            photonView.RPC("SyncUnitChangesRPC", RpcTarget.All, UnitChangeInfo.SerializeList(changesToSync));
        }

        if (targetPlayer != null && totalPlayerDamage > 0)
        {
            HealthManager.Instance.DealDamage(targetPlayer, totalPlayerDamage);
        }
    }

    // --- BUG FIX 2: 튜토리얼 전용 소환 함수 추가 ---
    public void SummonUnitsForTutorial(List<CompletedYeokInfo> yeoks)
    {
        foreach (var yeokInfo in yeoks)
        {
            UnitData attacker = CreateUnitDataFromYeok(yeokInfo, null);
            attacker.IsTutorialEnemy = false;

            int[] slotIndices = GetPerimeterIndices(yeokInfo);
            foreach (int slotIndex in slotIndices)
            {
                GameObject targetSlot = perimeterSlots[slotIndex];
                TutorialCombat(attacker, targetSlot);
            }
        }
    }

    private void TutorialCombat(UnitData attacker, GameObject slot)
    {
        if (unitDataOnBoard.TryGetValue(slot, out UnitData defender))
        {
            if (defender.IsTutorialEnemy)
            {
                defender.HP -= attacker.InitialDamage;
                if (defender.HP <= 0)
                {
                    unitDataOnBoard.Remove(slot);
                    if (defender.HP < 0)
                    {
                        PlaceUnit(attacker, slot);
                    }
                    else
                    {
                        Image img = slot.GetComponent<Image>();
                        img.sprite = null;
                        img.color = Color.clear;
                    }
                }
            }
            else // 아군 유닛 위에 재배치
            {
                if (attacker.InitialDamage >= defender.InitialDamage)
                {
                    PlaceUnit(attacker, slot);
                }
            }
        }
        else // 빈 칸
        {
            PlaceUnit(attacker, slot);
        }
    }

    // (이 아래의 다른 함수들은 제공해주신 코드와 대부분 동일합니다)
    private int SimulateCombat(UnitData attacker, GameObject slot, Dictionary<GameObject, UnitData> board, List<UnitChangeInfo> changes)
    {
        int slotIndex = System.Array.IndexOf(perimeterSlots, slot);
        int damageToPlayer = 0;

        if (board.TryGetValue(slot, out UnitData defender))
        {
            if (defender.Owner == attacker.Owner) // 아군 유닛
            {
                if (attacker.InitialDamage >= defender.InitialDamage)
                {
                    board[slot] = attacker;
                    changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
                }
            }
            else // 적 유닛
            {
                defender.HP -= attacker.InitialDamage;
                if (defender.HP <= 0)
                {
                    if (defender.HP < 0)
                    {
                        damageToPlayer = -defender.HP;
                        board[slot] = attacker;
                        changes.Add(UnitChangeInfo.Update(slotIndex, attacker));
                    }
                    else
                    {
                        board.Remove(slot);
                        changes.Add(UnitChangeInfo.Clear(slotIndex));
                    }
                }
                else
                {
                    changes.Add(UnitChangeInfo.Update(slotIndex, defender));
                }
            }
        }
        else // 빈 칸
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
            if (change.ShouldClear)
            {
                if (unitDataOnBoard.ContainsKey(slot)) unitDataOnBoard.Remove(slot);
                Image img = slot.GetComponent<Image>();
                img.sprite = null;
                img.color = Color.clear;
            }
            else
            {
                UnitData newUnitData = change.GetUnitData();
                unitDataOnBoard[slot] = newUnitData;
                UpdateSlotDisplay(slot, newUnitData);
            }
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;
        UpdateSlotDisplay(slot, unit);
    }

    private void UpdateSlotDisplay(GameObject slot, UnitData unit)
    {
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];
        if (unit.Owner != null)
        {
            slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
        }
        else
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
        PlaceUnit(enemyUnit, targetSlot);
    }
    #endregion
}

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