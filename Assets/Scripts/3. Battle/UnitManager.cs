using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// 24개의 외곽 전장을 관리하고, 유닛의 소환, 전투, 파괴를 처리합니다.
/// GameManager에 의해 초기화되고 제어됩니다.
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

    /// <summary>
    /// GameManager가 호출하여 전장 슬롯을 설정하고 클릭 가능하게 만드는 초기화 함수
    /// </summary>
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

    /// <summary>
    /// GameManager가 호출. 모든 클라이언트에게 유닛을 소환하라는 RPC 명령을 보냅니다.
    /// </summary>
    public void SummonUnitsViaRPC(List<CompletedYeokInfo> yeoks, Player owner)
    {
        int[] yeokTypes = new int[yeoks.Count];
        int[] lineIndices = new int[yeoks.Count];
        bool[] isHorizontals = new bool[yeoks.Count];
        string[] combinationStrings = new string[yeoks.Count];
        int[] baseScores = new int[yeoks.Count];
        int[] bonusScores = new int[yeoks.Count];

        for (int i = 0; i < yeoks.Count; i++)
        {
            yeokTypes[i] = (int)yeoks[i].YeokType;
            lineIndices[i] = yeoks[i].LineIndex;
            isHorizontals[i] = yeoks[i].IsHorizontal;
            combinationStrings[i] = yeoks[i].CombinationString;
            baseScores[i] = yeoks[i].BaseScore;
            bonusScores[i] = yeoks[i].BonusScore;
        }

        photonView.RPC("SummonUnitsRPC", RpcTarget.All, owner, yeokTypes, lineIndices, isHorizontals, combinationStrings, baseScores, bonusScores);
    }

    /// <summary>
    /// [RPC] 모든 클라이언트에서 실행되어 실제로 유닛을 소환하는 함수
    /// </summary>
    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals, string[] combinationStrings, int[] baseScores, int[] bonusScores)
    {
        for (int i = 0; i < yeokTypes.Length; i++)
        {
            CompletedYeokInfo yeokInfo = new CompletedYeokInfo
            {
                YeokType = (BaseTreeEnum)yeokTypes[i],
                LineIndex = lineIndices[i],
                IsHorizontal = isHorizontals[i],
                CombinationString = combinationStrings[i],
                BaseScore = baseScores[i],
                BonusScore = bonusScores[i]
            };

            int[] slotIndices = GetPerimeterIndices(yeokInfo);

            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[0]]);
            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[1]]);
        }
    }

    /// <summary>
    /// 지정된 슬롯에 유닛 배치를 시도합니다. (빈 슬롯이면 데미지, 적 유닛이 있으면 전투)
    /// </summary>
    private void AttemptToPlaceUnit(UnitData attackerData, GameObject targetSlot)
    {
        if (unitDataOnBoard.TryGetValue(targetSlot, out UnitData defenderData))
        {
            if (defenderData.Owner != attackerData.Owner)
            {
                ResolveCombat(attackerData, defenderData, targetSlot);
            }
        }
        else // 슬롯이 비어있는 경우
        {
            // 마스터 클라이언트만 데미지 계산 및 전송을 담당합니다.
            if (PhotonNetwork.IsMasterClient)
            {
                // 공격 유닛의 주인을 기준으로 상대방(타겟)을 찾습니다.
                Player targetPlayer = null;
                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    if (p != attackerData.Owner)
                    {
                        targetPlayer = p;
                        break;
                    }
                }

                // 타겟이 존재하면, 공격 유닛의 '초기 데미지'를 입힙니다.
                if (targetPlayer != null)
                {
                    HealthManager.Instance.DealDamage(targetPlayer, attackerData.InitialDamage);
                }
            }

            // 모든 클라이언트에서 유닛을 배치합니다.
            PlaceUnit(attackerData, targetSlot);
        }
    }

    private void ResolveCombat(UnitData attacker, UnitData defender, GameObject defenderSlot)
    {
        int damageDealt = attacker.InitialDamage;
        defender.HP -= damageDealt;

        if (defender.HP <= 0)
        {
            unitDataOnBoard.Remove(defenderSlot);
            int overkillDamage = -defender.HP;
            if (overkillDamage > 0 && PhotonNetwork.IsMasterClient)
            {
                HealthManager.Instance.DealDamage(defender.Owner, overkillDamage);
            }
            PlaceUnit(attacker, defenderSlot);
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;

        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];
        slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
    }

    /// <summary>
    /// GameManager가 라운드 종료 시 호출. 모든 유닛의 지속 데미지를 처리합니다.
    /// </summary>
    public void ApplyContinuousDamage()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var entry in unitDataOnBoard)
        {
            UnitData unit = entry.Value;
            Player targetPlayer = (unit.Owner == PhotonNetwork.MasterClient) ? PhotonNetwork.PlayerListOthers[0] : PhotonNetwork.MasterClient;

            if (unit.ContinuousDamage > 0)
            {
                HealthManager.Instance.DealDamage(targetPlayer, unit.ContinuousDamage);
            }
        }
    }

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
}