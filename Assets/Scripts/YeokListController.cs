// YeokListController.cs (스크롤 전용 버전)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// '역' 정보를 담을 구조체들은 이전과 동일합니다.
public struct YeokInfo
{
    public BaseTreeEnum YeokType;
    public string Name;
    public string Description;
    public int Score;
    public int[] ExampleDice;
}

public struct AddYeokInfo
{
    public string Name;
    public string Description;
    public int Score;
}

public class YeokListController : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private GameObject yeokListItemPrefab;
    [SerializeField] private GameObject addYeokListItemPrefab;
    [SerializeField] private Transform baseContentParent;
    [SerializeField] private Transform addContentParent;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button showBaseYeokButton;
    [SerializeField] private Button showAddYeokButton;
    [SerializeField] private GameObject baseYeokScrollView;
    [SerializeField] private GameObject addYeokScrollView;
    [SerializeField] private Sprite[] diceSprites;

    private BaseTreeScore baseScores = new BaseTreeScore();
    private AddTreeScore addScores = new AddTreeScore();

    private List<YeokInfo> allBaseYeoks = new List<YeokInfo>();
    private List<AddYeokInfo> allAddYeoks = new List<AddYeokInfo>();

    void Start()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        showBaseYeokButton.onClick.AddListener(() => SwitchTab(true));
        showAddYeokButton.onClick.AddListener(() => SwitchTab(false));

        InitializeYeokData();
        PopulateAllLists();
        SwitchTab(true); // 시작 시 기본역 탭을 먼저 보여줌
    }

    private void SwitchTab(bool showBase)
    {
        baseYeokScrollView.SetActive(showBase);
        addYeokScrollView.SetActive(!showBase);

        showBaseYeokButton.image.color = showBase ? Color.yellow : Color.white;
        showAddYeokButton.image.color = !showBase ? Color.yellow : Color.white;
    }

    // 모든 '역' 데이터를 한 번에 생성하는 함수
    private void PopulateAllLists()
    {
        // 1. 기본역 목록 생성
        foreach (Transform child in baseContentParent) { Destroy(child.gameObject); } // 기존 목록 삭제
        foreach (var yeok in allBaseYeoks)
        {
            CreateBaseYeokItem(yeok);
        }

        // 2. 추가역 목록 생성
        foreach (Transform child in addContentParent) { Destroy(child.gameObject); } // 기존 목록 삭제
        foreach (var yeok in allAddYeoks)
        {
            CreateAddYeokItem(yeok);
        }
    }

    // (이 아래의 CreateBaseYeokItem, CreateAddYeokItem, InitializeYeokData, GetBaseYeokInfo 함수는 이전과 동일합니다)
    private void CreateBaseYeokItem(YeokInfo info)
    {
        GameObject itemGO = Instantiate(yeokListItemPrefab, baseContentParent);
        TextMeshProUGUI nameText = itemGO.transform.Find("YeokNameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descText = itemGO.transform.Find("YeokDescText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreText = itemGO.transform.Find("YeokScoreText").GetComponent<TextMeshProUGUI>();
        Transform diceIconParent = itemGO.transform.Find("DiceIconLayout");

        nameText.text = info.Name;
        descText.text = info.Description;
        scoreText.text = $"기본 점수: {info.Score}";

        List<Image> diceImages = new List<Image>(diceIconParent.GetComponentsInChildren<Image>());
        for (int i = 0; i < diceImages.Count; i++)
        {
            if (i < info.ExampleDice.Length)
            {
                diceImages[i].gameObject.SetActive(true);
                diceImages[i].sprite = diceSprites[info.ExampleDice[i]];
            }
            else
            {
                diceImages[i].gameObject.SetActive(false);
            }
        }
    }

    private void CreateAddYeokItem(AddYeokInfo info)
    {
        GameObject itemGO = Instantiate(addYeokListItemPrefab, addContentParent);
        TextMeshProUGUI nameText = itemGO.transform.Find("YeokNameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descText = itemGO.transform.Find("YeokDescText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreText = itemGO.transform.Find("YeokScoreText").GetComponent<TextMeshProUGUI>();

        nameText.text = info.Name;
        descText.text = info.Description;
        scoreText.text = $"추가 점수: +{info.Score}";
    }

    private void InitializeYeokData()
    {
        // 1. 기본역 데이터 초기화
        foreach (BaseTreeEnum yeok in System.Enum.GetValues(typeof(BaseTreeEnum)))
        {
            allBaseYeoks.Add(GetBaseYeokInfo(yeok));
        }

        // 2. 추가역 데이터 초기화
        allAddYeoks.Add(new AddYeokInfo { Name = "올 넘버", Description = "모두 숫자 주사위 (1,2,3)", Score = addScores.allNumber });
        allAddYeoks.Add(new AddYeokInfo { Name = "올 심볼", Description = "모두 상징 주사위 (별,달,해)", Score = addScores.allSymbol });
        allAddYeoks.Add(new AddYeokInfo { Name = "퓨어", Description = "모든 주사위 눈이 동일", Score = addScores.pure });
        allAddYeoks.Add(new AddYeokInfo { Name = "믹스", Description = "모든 주사위 눈이 다름", Score = addScores.mix });
        allAddYeoks.Add(new AddYeokInfo { Name = "인클루드 스타", Description = "별 주사위 포함", Score = addScores.includeStar });
        allAddYeoks.Add(new AddYeokInfo { Name = "인클루드 문", Description = "달 주사위 포함", Score = addScores.includeMoon });
        allAddYeoks.Add(new AddYeokInfo { Name = "인클루드 선", Description = "해 주사위 포함", Score = addScores.includeSun });
        allAddYeoks.Add(new AddYeokInfo { Name = "풀 다이스", Description = "주사위 6개로 완성", Score = addScores.fullDice });
    }

    private YeokInfo GetBaseYeokInfo(BaseTreeEnum yeok)
    {
        YeokInfo info = new YeokInfo { YeokType = yeok };
        switch (yeok)
        {
            case BaseTreeEnum.pair: info.Name = "페어"; info.Description = "같은 눈 2개"; info.Score = baseScores.pair; info.ExampleDice = new int[] { 0, 0 }; break;
            case BaseTreeEnum.twoPair: info.Name = "투 페어"; info.Description = "페어 2개 조합"; info.Score = baseScores.twoPair; info.ExampleDice = new int[] { 0, 0, 1, 1 }; break;
            case BaseTreeEnum.triple: info.Name = "트리플"; info.Description = "같은 눈 3개"; info.Score = baseScores.triple; info.ExampleDice = new int[] { 2, 2, 2 }; break;
            case BaseTreeEnum.straight: info.Name = "스트레이트"; info.Description = "연속된 숫자 또는 상징 3개"; info.Score = baseScores.straight; info.ExampleDice = new int[] { 0, 1, 2 }; break;
            case BaseTreeEnum.fullHouse: info.Name = "풀하우스"; info.Description = "트리플 1개 + 페어 1개"; info.Score = baseScores.fullHouse; info.ExampleDice = new int[] { 2, 2, 2, 0, 0 }; break;
            case BaseTreeEnum.threePair: info.Name = "쓰리 페어"; info.Description = "페어 3개 조합"; info.Score = baseScores.threePair; info.ExampleDice = new int[] { 0, 0, 1, 1, 2, 2 }; break;
            case BaseTreeEnum.fourCard: info.Name = "포카드"; info.Description = "같은 눈 4개"; info.Score = baseScores.fourCard; info.ExampleDice = new int[] { 3, 3, 3, 3 }; break;
            case BaseTreeEnum.fiveCard: info.Name = "파이브카드"; info.Description = "같은 눈 5개"; info.Score = baseScores.fiveCard; info.ExampleDice = new int[] { 4, 4, 4, 4, 4 }; break;
            case BaseTreeEnum.doubleTriple: info.Name = "더블 트리플"; info.Description = "트리플 2개 조합"; info.Score = baseScores.doubleTriple; info.ExampleDice = new int[] { 1, 1, 1, 2, 2, 2 }; break;
            case BaseTreeEnum.grandFullHouse: info.Name = "그랜드 풀하우스"; info.Description = "포카드 1개 + 페어 1개"; info.Score = baseScores.grandFullHouse; info.ExampleDice = new int[] { 0, 0, 0, 0, 1, 1 }; break;
            case BaseTreeEnum.doubleStraight: info.Name = "더블 스트레이트"; info.Description = "스트레이트 2개 조합"; info.Score = baseScores.doubleStraight; info.ExampleDice = new int[] { 0, 0, 1, 1, 2, 2 }; break;
            case BaseTreeEnum.universe: info.Name = "유니버스"; info.Description = "모든 종류의 눈 1개씩 (총 6개)"; info.Score = baseScores.universe; info.ExampleDice = new int[] { 0, 1, 2, 3, 4, 5 }; break;
            case BaseTreeEnum.hexa: info.Name = "헥사"; info.Description = "같은 눈 6개"; info.Score = baseScores.hexa; info.ExampleDice = new int[] { 5, 5, 5, 5, 5, 5 }; break;
            case BaseTreeEnum.genesis: info.Name = "제네시스"; info.Description = "'1' 3개 + '해' 3개"; info.Score = baseScores.genesis; info.ExampleDice = new int[] { 0, 0, 0, 5, 5, 5 }; break;
            default: info.Name = yeok.ToString(); info.Description = "고유 조합"; info.Score = 0; info.ExampleDice = new int[0]; break;
        }
        return info;
    }
}