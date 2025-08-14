// YeokListController.cs (��ũ�� ���� ����)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// '��' ������ ���� ����ü���� ������ �����մϴ�.
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
    [Header("UI ��� ����")]
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
        SwitchTab(true); // ���� �� �⺻�� ���� ���� ������
    }

    private void SwitchTab(bool showBase)
    {
        baseYeokScrollView.SetActive(showBase);
        addYeokScrollView.SetActive(!showBase);

        showBaseYeokButton.image.color = showBase ? Color.yellow : Color.white;
        showAddYeokButton.image.color = !showBase ? Color.yellow : Color.white;
    }

    // ��� '��' �����͸� �� ���� �����ϴ� �Լ�
    private void PopulateAllLists()
    {
        // 1. �⺻�� ��� ����
        foreach (Transform child in baseContentParent) { Destroy(child.gameObject); } // ���� ��� ����
        foreach (var yeok in allBaseYeoks)
        {
            CreateBaseYeokItem(yeok);
        }

        // 2. �߰��� ��� ����
        foreach (Transform child in addContentParent) { Destroy(child.gameObject); } // ���� ��� ����
        foreach (var yeok in allAddYeoks)
        {
            CreateAddYeokItem(yeok);
        }
    }

    // (�� �Ʒ��� CreateBaseYeokItem, CreateAddYeokItem, InitializeYeokData, GetBaseYeokInfo �Լ��� ������ �����մϴ�)
    private void CreateBaseYeokItem(YeokInfo info)
    {
        GameObject itemGO = Instantiate(yeokListItemPrefab, baseContentParent);
        TextMeshProUGUI nameText = itemGO.transform.Find("YeokNameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descText = itemGO.transform.Find("YeokDescText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreText = itemGO.transform.Find("YeokScoreText").GetComponent<TextMeshProUGUI>();
        Transform diceIconParent = itemGO.transform.Find("DiceIconLayout");

        nameText.text = info.Name;
        descText.text = info.Description;
        scoreText.text = $"�⺻ ����: {info.Score}";

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
        scoreText.text = $"�߰� ����: +{info.Score}";
    }

    private void InitializeYeokData()
    {
        // 1. �⺻�� ������ �ʱ�ȭ
        foreach (BaseTreeEnum yeok in System.Enum.GetValues(typeof(BaseTreeEnum)))
        {
            allBaseYeoks.Add(GetBaseYeokInfo(yeok));
        }

        // 2. �߰��� ������ �ʱ�ȭ
        allAddYeoks.Add(new AddYeokInfo { Name = "�� �ѹ�", Description = "��� ���� �ֻ��� (1,2,3)", Score = addScores.allNumber });
        allAddYeoks.Add(new AddYeokInfo { Name = "�� �ɺ�", Description = "��� ��¡ �ֻ��� (��,��,��)", Score = addScores.allSymbol });
        allAddYeoks.Add(new AddYeokInfo { Name = "ǻ��", Description = "��� �ֻ��� ���� ����", Score = addScores.pure });
        allAddYeoks.Add(new AddYeokInfo { Name = "�ͽ�", Description = "��� �ֻ��� ���� �ٸ�", Score = addScores.mix });
        allAddYeoks.Add(new AddYeokInfo { Name = "��Ŭ��� ��Ÿ", Description = "�� �ֻ��� ����", Score = addScores.includeStar });
        allAddYeoks.Add(new AddYeokInfo { Name = "��Ŭ��� ��", Description = "�� �ֻ��� ����", Score = addScores.includeMoon });
        allAddYeoks.Add(new AddYeokInfo { Name = "��Ŭ��� ��", Description = "�� �ֻ��� ����", Score = addScores.includeSun });
        allAddYeoks.Add(new AddYeokInfo { Name = "Ǯ ���̽�", Description = "�ֻ��� 6���� �ϼ�", Score = addScores.fullDice });
    }

    private YeokInfo GetBaseYeokInfo(BaseTreeEnum yeok)
    {
        YeokInfo info = new YeokInfo { YeokType = yeok };
        switch (yeok)
        {
            case BaseTreeEnum.pair: info.Name = "���"; info.Description = "���� �� 2��"; info.Score = baseScores.pair; info.ExampleDice = new int[] { 0, 0 }; break;
            case BaseTreeEnum.twoPair: info.Name = "�� ���"; info.Description = "��� 2�� ����"; info.Score = baseScores.twoPair; info.ExampleDice = new int[] { 0, 0, 1, 1 }; break;
            case BaseTreeEnum.triple: info.Name = "Ʈ����"; info.Description = "���� �� 3��"; info.Score = baseScores.triple; info.ExampleDice = new int[] { 2, 2, 2 }; break;
            case BaseTreeEnum.straight: info.Name = "��Ʈ����Ʈ"; info.Description = "���ӵ� ���� �Ǵ� ��¡ 3��"; info.Score = baseScores.straight; info.ExampleDice = new int[] { 0, 1, 2 }; break;
            case BaseTreeEnum.fullHouse: info.Name = "Ǯ�Ͽ콺"; info.Description = "Ʈ���� 1�� + ��� 1��"; info.Score = baseScores.fullHouse; info.ExampleDice = new int[] { 2, 2, 2, 0, 0 }; break;
            case BaseTreeEnum.threePair: info.Name = "���� ���"; info.Description = "��� 3�� ����"; info.Score = baseScores.threePair; info.ExampleDice = new int[] { 0, 0, 1, 1, 2, 2 }; break;
            case BaseTreeEnum.fourCard: info.Name = "��ī��"; info.Description = "���� �� 4��"; info.Score = baseScores.fourCard; info.ExampleDice = new int[] { 3, 3, 3, 3 }; break;
            case BaseTreeEnum.fiveCard: info.Name = "���̺�ī��"; info.Description = "���� �� 5��"; info.Score = baseScores.fiveCard; info.ExampleDice = new int[] { 4, 4, 4, 4, 4 }; break;
            case BaseTreeEnum.doubleTriple: info.Name = "���� Ʈ����"; info.Description = "Ʈ���� 2�� ����"; info.Score = baseScores.doubleTriple; info.ExampleDice = new int[] { 1, 1, 1, 2, 2, 2 }; break;
            case BaseTreeEnum.grandFullHouse: info.Name = "�׷��� Ǯ�Ͽ콺"; info.Description = "��ī�� 1�� + ��� 1��"; info.Score = baseScores.grandFullHouse; info.ExampleDice = new int[] { 0, 0, 0, 0, 1, 1 }; break;
            case BaseTreeEnum.doubleStraight: info.Name = "���� ��Ʈ����Ʈ"; info.Description = "��Ʈ����Ʈ 2�� ����"; info.Score = baseScores.doubleStraight; info.ExampleDice = new int[] { 0, 0, 1, 1, 2, 2 }; break;
            case BaseTreeEnum.universe: info.Name = "���Ϲ���"; info.Description = "��� ������ �� 1���� (�� 6��)"; info.Score = baseScores.universe; info.ExampleDice = new int[] { 0, 1, 2, 3, 4, 5 }; break;
            case BaseTreeEnum.hexa: info.Name = "���"; info.Description = "���� �� 6��"; info.Score = baseScores.hexa; info.ExampleDice = new int[] { 5, 5, 5, 5, 5, 5 }; break;
            case BaseTreeEnum.genesis: info.Name = "���׽ý�"; info.Description = "'1' 3�� + '��' 3��"; info.Score = baseScores.genesis; info.ExampleDice = new int[] { 0, 0, 0, 5, 5, 5 }; break;
            default: info.Name = yeok.ToString(); info.Description = "���� ����"; info.Score = 0; info.ExampleDice = new int[0]; break;
        }
        return info;
    }
}