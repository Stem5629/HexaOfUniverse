using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// 24개의 외곽 전장을 관리하고, 유닛의 소환, 전투, 파괴를 처리합니다.
/// FieldInitializer에 의해 초기화되고, GameManager에 의해 제어됩니다.
/// </summary>
public class UnitManager : MonoBehaviourPunCallbacks
{
    [Header("UI 및 시각 효과 참조")]
    // 각 역(Yeok)에 해당하는 유닛의 2D 스프라이트를 순서대로 할당해야 합니다.
    // (BaseTreeEnum의 순서와 일치해야 함: pair, twoPair, triple ...)
    [SerializeField] private Sprite[] unitSpritesByYeok;
    [SerializeField] private Color myColor = Color.red; // 내 유닛의 색상
    [SerializeField] private Color versusColor = Color.blue; // 상대 유닛의 색상

    [Header("유닛 정보창 UI")]
    public GameObject infoPanel; // 정보창 패널 오브젝트
    public TextMeshProUGUI unitTreeText;
    public TextMeshProUGUI treeCombinationText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI beginDamageText;
    public TextMeshProUGUI keepDamageText;

    // --- 데이터 ---
    private GameObject[] perimeterSlots; // 24개의 외곽 슬롯 (FieldInitializer가 설정)
    private Dictionary<GameObject, UnitData> unitDataOnBoard = new Dictionary<GameObject, UnitData>();

    /*private void Start()
    {
        // 씬에 생성된 GameManager를 찾아 자신을 등록
        GameManager.Instance.SetUnitManager(this);
    }*/

    /// <summary>
    /// FieldInitializer가 호출하여 전장 슬롯을 설정하는 초기화 함수
    /// </summary>
    public void Initialize(GameObject[] slots)
    {
        this.perimeterSlots = slots;
        unitDataOnBoard.Clear();
        infoPanel.SetActive(false); // 시작 시 정보창은 항상 비활성화

        // 각 슬롯에 클릭 이벤트를 추가합니다.
        foreach (var slot in perimeterSlots)
        {
            Button button = slot.GetComponent<Button>();
            if (button == null) button = slot.AddComponent<Button>(); // 버튼이 없으면 새로 추가

            button.onClick.RemoveAllListeners();
            // 클릭 시 OnUnitSlotClicked 함수가 호출되도록 연결합니다.
            button.onClick.AddListener(() => OnUnitSlotClicked(slot));
        }
    }

    /// <summary>
    /// GameManager가 호출. 모든 클라이언트에게 유닛을 소환하라는 RPC 명령을 보냅니다.
    /// </summary>
    public void SummonUnitsViaRPC(List<CompletedYeokInfo> yeoks, Player owner)
    {
        // Photon RPC로 전송하기 위해 데이터를 간단한 배열 형태로 변환합니다.
        int[] yeokTypes = new int[yeoks.Count];
        int[] lineIndices = new int[yeoks.Count];
        bool[] isHorizontals = new bool[yeoks.Count];
        string[] combinationStrings = new string[yeoks.Count];

        for (int i = 0; i < yeoks.Count; i++)
        {
            yeokTypes[i] = (int)yeoks[i].YeokType;
            lineIndices[i] = yeoks[i].LineIndex;
            isHorizontals[i] = yeoks[i].IsHorizontal;
            combinationStrings[i] = yeoks[i].CombinationString;
        }

        // 모든 클라이언트에게 SummonUnitsRPC 함수를 실행하라고 방송합니다.
        photonView.RPC("SummonUnitsRPC", RpcTarget.All, owner, yeokTypes, lineIndices, isHorizontals, combinationStrings);
    }

    /// <summary>
    /// [RPC] 모든 클라이언트에서 실행되어 실제로 유닛을 소환하는 함수
    /// </summary>
    [PunRPC]
    private void SummonUnitsRPC(Player owner, int[] yeokTypes, int[] lineIndices, bool[] isHorizontals, string[] combinationStrings)
    {
        Debug.Log($"{owner.NickName}님이 {yeokTypes.Length}개의 역을 완성하여 유닛을 소환합니다!");

        for (int i = 0; i < yeokTypes.Length; i++)
        {
            CompletedYeokInfo yeokInfo = new CompletedYeokInfo
            {
                YeokType = (BaseTreeEnum)yeokTypes[i],
                LineIndex = lineIndices[i],
                IsHorizontal = isHorizontals[i]
            };

            // UnitData newUnitData = CreateUnitDataFromYeok(yeokInfo, owner); <-- 기존 코드는 삭제
            yeokInfo.CombinationString = combinationStrings[i];
            int[] slotIndices = GetPerimeterIndices(yeokInfo);

            // 각 슬롯에 배치할 때마다 새로운 유닛 데이터를 생성합니다.
            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[0]]);
            AttemptToPlaceUnit(CreateUnitDataFromYeok(yeokInfo, owner), perimeterSlots[slotIndices[1]]);
        }
    }

    private void AttemptToPlaceUnit(UnitData attackerData, GameObject targetSlot)
    {
        if (unitDataOnBoard.TryGetValue(targetSlot, out UnitData defenderData))
        {
            if (defenderData.Owner != attackerData.Owner)
            {
                ResolveCombat(attackerData, defenderData, targetSlot);
            }
        }
        else
        {
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
            if (overkillDamage > 0 && PhotonNetwork.IsMasterClient) // 오버킬 데미지는 방장만 계산하여 동기화
            {
                HealthManager.Instance.DealDamage(defender.Owner, overkillDamage);
            }
            PlaceUnit(attacker, defenderSlot);
        }
    }

    private void PlaceUnit(UnitData unit, GameObject slot)
    {
        unitDataOnBoard[slot] = unit;

        // --- 슬롯 UI 업데이트 ---
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = unitSpritesByYeok[(int)unit.YeokType];
        slotImage.color = (unit.Owner.IsLocal) ? myColor : versusColor;
    }

    /// <summary>
    /// GameManager가 턴 종료 시 호출. 모든 유닛의 지속 데미지를 처리합니다.
    /// (마스터 클라이언트에서만 호출되어야 합니다.)
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

    // 역 정보로부터 UnitData를 생성하는 팩토리 메서드
    private UnitData CreateUnitDataFromYeok(CompletedYeokInfo yeok, Player owner)
    {
        UnitData unit = new UnitData { Owner = owner, YeokType = yeok.YeokType };

        // --- 밸런싱을 위한 설정값 ---
        // 이 값들만 조절하면 전체 유닛의 능력치가 변경됩니다.
        const float hpMultiplier = 2.5f;     // 점수 대비 체력 비율
        const float initialDamageMultiplier = 1.8f; // 점수 대비 초기 데미지 비율
        const float continuousDamageMultiplier = 0.5f; // 점수 대비 지속 데미지 비율

        // --- 1. '역'의 기본 점수 가져오기 ---
        int baseScore = GetBaseScore(yeok.YeokType);

        // --- 2. 점수를 기반으로 능력치 자동 계산 ---
        unit.HP = Mathf.RoundToInt(baseScore * hpMultiplier);
        unit.InitialDamage = Mathf.RoundToInt(baseScore * initialDamageMultiplier);
        unit.ContinuousDamage = Mathf.RoundToInt(baseScore * continuousDamageMultiplier);

        // --- 예외 처리: 최소 능력치 보장 ---
        // 점수가 낮은 역이라도 최소한의 성능을 갖도록 보장합니다.
        unit.HP = Mathf.Max(10, unit.HP);
        unit.InitialDamage = Mathf.Max(5, unit.InitialDamage);
        unit.ContinuousDamage = Mathf.Max(1, unit.ContinuousDamage);
        unit.CombinationString = yeok.CombinationString;

        return unit;
    }

    // 4. 클릭 시 실행될 새로운 함수 추가
    private void OnUnitSlotClicked(GameObject clickedSlot)
    {
        // 클릭한 슬롯에 유닛 데이터가 있는지 확인합니다.
        if (unitDataOnBoard.TryGetValue(clickedSlot, out UnitData unitData))
        {
            // 유닛이 있다면, 정보창을 항상 활성화하고 내용을 업데이트합니다.
            infoPanel.SetActive(true);

            unitTreeText.text = unitData.YeokType.ToString();
            treeCombinationText.text = unitData.CombinationString;
            hpText.text = $"HP : {unitData.HP}";
            beginDamageText.text = $"초기데미지 : {unitData.InitialDamage}";
            keepDamageText.text = $"지속데미지 : {unitData.ContinuousDamage}";
        }
        else // 빈 슬롯을 클릭한 경우
        {
            // 유닛이 없다면 정보창을 비활성화합니다.
            infoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// BaseTree에서 해당 역의 기본 점수를 가져오는 헬퍼 함수
    /// </summary>
    private int GetBaseScore(BaseTreeEnum yeok)
    {
        BaseTreeScore scores = new BaseTreeScore();
        switch (yeok)
        {
            case BaseTreeEnum.pair: return scores.pair;
            case BaseTreeEnum.twoPair: return scores.twoPair;
            case BaseTreeEnum.triple: return scores.triple;
            case BaseTreeEnum.fullHouse: return scores.fullHouse;
            case BaseTreeEnum.threePair: return scores.threePair;
            case BaseTreeEnum.straight: return scores.straight;
            case BaseTreeEnum.fourCard: return scores.fourCard;
            case BaseTreeEnum.doubleTriple: return scores.doubleTriple;
            case BaseTreeEnum.grandFullHouse: return scores.grandFullHouse;
            case BaseTreeEnum.doubleStraight: return scores.doubleStraight;
            case BaseTreeEnum.fiveCard: return scores.fiveCard;
            case BaseTreeEnum.universe: return scores.universe;
            case BaseTreeEnum.hexa: return scores.hexa;
            case BaseTreeEnum.genesis: return scores.genesis;
            default: return 1; // 알 수 없는 역은 기본 점수 1
        }
    }

    private int[] GetPerimeterIndices(CompletedYeokInfo yeokInfo)
    {
        int[] indices = new int[2];
        if (yeokInfo.IsHorizontal)
        {
            // 왼쪽 타일 인덱스 계산 공식을 수정합니다.
            indices[0] = 12 + yeokInfo.LineIndex; // 17 - yeokInfo.LineIndex -> 12 + yeokInfo.LineIndex 로 변경
            indices[1] = 18 + yeokInfo.LineIndex; // 오른쪽은 정상이므로 그대로 둡니다.
        }
        else
        {
            // 세로줄 계산은 정상이므로 그대로 둡니다.
            indices[0] = yeokInfo.LineIndex;
            indices[1] = 6 + yeokInfo.LineIndex;
        }
        return indices;
    }
}