using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    // --------------------------------------------------
    // # �̱��� �ν��Ͻ�
    // --------------------------------------------------
    public static TutorialManager Instance { get; private set; }

    // --------------------------------------------------
    // # Ʃ�丮�� �ܰ� ����
    // --------------------------------------------------
    public enum TutorialStep
    {
        Stage1_Setup,
        Stage1_SelectDice,
        Stage1_PlaceDice,
        Stage1_Complete,

        Stage2_Setup,
        Stage2_WaitForFirstDice,
        Stage2_HighlightPairLocations,
        Stage2_WaitForSecondDice,
        Stage2_Summon,
        Stage2_Complete,

        Stage3_Setup,
        Stage3_PlaceForTriple,
        Stage3_Summon,
        Stage3_Complete,

        Stage4_Setup,
        Stage4_Drafting,
        Stage4_Complete,

        Stage5_Setup,
        Stage5_DirectDamage_Place,
        Stage5_DirectDamage_Summon,
        Stage5_BonusScore_Place,
        Stage5_BonusScore_Summon,
        Stage5_Complete
    }
    public TutorialStep currentStep;

    // --------------------------------------------------
    // # UI �� �Ŵ��� ����
    // --------------------------------------------------
    [Header("�ٽ� �Ŵ��� ����")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UnitManager unitManager;

    [Header("���� UI ����")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button summonButton;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button previousStepButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Transform tileContainer;
    [SerializeField] private Sprite[] diceSprites;

    [Header("���̳��� ���̶���Ʈ")]
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Canvas mainCanvas;
    private List<GameObject> highlightInstances = new List<GameObject>();

    [Header("�ܰ躰 UI ����")]
    [SerializeField] private GameObject tutorialDraftPanel;
    [SerializeField] private List<Button> tutorialDiceButtons;
    [SerializeField] private List<Image> tutorialDiceImages;
    [SerializeField] private Slider tutorialMyHpBar;
    [SerializeField] private Slider tutorialEnemyHpBar;
    [SerializeField] private TextMeshProUGUI tutorialEnemyHpText; // ��� HP �ؽ�Ʈ ���� �߰�
    [SerializeField] private GameObject unitInfoPanel;

    private int selectedDiceNumber;
    private int tutorialPlayerMaxHp = 100;
    private int tutorialPlayerCurrentHp;
    private int tutorialEnemyMaxHp = 100;
    private int tutorialEnemyCurrentHp;
    private Vector2Int firstDicePosition;
    private List<int> currentDraftPool;
    private int picksRemaining;
    private List<int> draftedDiceThisStage = new List<int>();
    private Stack<TutorialStep> stepHistory = new Stack<TutorialStep>();

    // --------------------------------------------------
    // # Unity ����������Ŭ
    // --------------------------------------------------
    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        InitializeManagers();
        InitializeTutorialUI();
        boardManager.currentTurnPlayerHand = this.playerHand;

        if (summonButton != null) summonButton.onClick.AddListener(OnSummonButtonClicked);
        if (nextStageButton != null) nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
        if (previousStepButton != null) previousStepButton.onClick.AddListener(OnPreviousStepButtonClicked);
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipTutorialClicked);

        GoToStep(TutorialStep.Stage1_Setup, recordHistory: false);
    }

    // --------------------------------------------------
    // # Ʃ�丮�� �帧 ����
    // --------------------------------------------------
    private void GoToStep(TutorialStep nextStep, bool recordHistory = true)
    {
        if (recordHistory && currentStep != nextStep)
        {
            stepHistory.Push(currentStep);
        }
        currentStep = nextStep;
        RunCurrentStep();
    }

    private void RunCurrentStep()
    {
        HideAllHighlights();
        tutorialDraftPanel.SetActive(false);
        if (unitInfoPanel != null) unitInfoPanel.SetActive(false);

        if (tutorialEnemyHpBar != null)
        {
            bool isStage5Combat = currentStep >= TutorialStep.Stage5_DirectDamage_Place &&
                                  currentStep <= TutorialStep.Stage5_BonusScore_Summon;
            if (!isStage5Combat)
            {
                tutorialEnemyHpBar.gameObject.SetActive(false);
            }
        }

        if (summonButton != null) summonButton.interactable = false;
        if (nextStageButton != null) nextStageButton.gameObject.SetActive(false);
        if (previousStepButton != null) previousStepButton.gameObject.SetActive(stepHistory.Count > 0);

        switch (currentStep)
        {
            case TutorialStep.Stage1_Setup:
                instructionText.text = "�⺻ ���۹�: �ֻ����� ������ ���忡 ��������.";
                ClearBoardAndHand();
                playerHand.AddDice(0, 2);
                GoToStep(TutorialStep.Stage1_SelectDice, recordHistory: false);
                break;
            case TutorialStep.Stage1_SelectDice:
                instructionText.text = "�տ� �ִ� �ֻ����� �����ϼ���.";
                HighlightHandDice(0);
                break;
            case TutorialStep.Stage1_PlaceDice:
                instructionText.text = "���忡 �ֻ����� ��������.";
                break;
            case TutorialStep.Stage1_Complete:
                instructionText.text = "�⺻ ���۹��� �������ϴ�! '����'�� ���� ����ϼ���.";
                nextStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "���� �ܰ��";
                nextStageButton.gameObject.SetActive(true);
                break;

            case TutorialStep.Stage2_Setup:
                instructionText.text = "'��' ����⸦ ���ϴ�. '���'�� ����� ���ô�.";
                ClearBoardAndHand();
                playerHand.AddDice(1, 2);
                GoToStep(TutorialStep.Stage2_WaitForFirstDice, recordHistory: false);
                break;
            case TutorialStep.Stage2_WaitForFirstDice:
                instructionText.text = "ù ��° �ֻ����� ������ �ƹ� �� ĭ���� ���ƺ�����.";
                HighlightHandDice(1);
                break;
            case TutorialStep.Stage2_HighlightPairLocations:
                instructionText.text = "�����ϴ�! ���� ������ ĭ �� �� ���� �� ��° �ֻ����� ��������.";
                HighlightAllPairLocations(firstDicePosition);
                HighlightHandDice(1);
                break;
            case TutorialStep.Stage2_WaitForSecondDice:
                break;
            case TutorialStep.Stage2_Summon:
                instructionText.text = "'���' �ϼ�! '��ȯ' ��ư�� ���� ������ ���弼��.";
                summonButton.interactable = true;
                break;
            case TutorialStep.Stage2_Complete:
                instructionText.text = "���� ��ȯ ����! '����' ��ư�� ��������.";
                nextStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "���� �ܰ��";
                nextStageButton.gameObject.SetActive(true);
                break;

            case TutorialStep.Stage3_Setup:
                instructionText.text = "�� ������ 'Ʈ����' ������ ������ ���ô�.";
                ClearBoardAndHand();
                unitManager.PlaceTutorialEnemyUnit(0, BaseTreeEnum.pair);
                playerHand.AddDice(2, 3);
                GoToStep(TutorialStep.Stage3_PlaceForTriple, recordHistory: false);
                break;
            case TutorialStep.Stage3_PlaceForTriple:
                instructionText.text = "'3' �ֻ��� 3���� �� �ٿ� ���� 'Ʈ����'�� ���弼��.";
                HighlightHandDice(2);
                break;
            case TutorialStep.Stage3_Summon:
                instructionText.text = "'Ʈ����' �ϼ�! '��ȯ' ��ư���� �����ϼ���.";
                summonButton.interactable = true;
                break;
            case TutorialStep.Stage3_Complete:
                instructionText.text = "���� �¸�! �⺻ ��Ģ�� ��� ������ϴ�.";
                nextStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "���� �ܰ��";
                nextStageButton.gameObject.SetActive(true);
                break;

            case TutorialStep.Stage4_Setup:
                ClearBoardAndHand();
                draftedDiceThisStage.Clear();
                currentDraftPool = new List<int> { 0, 1, 2, 3, 4 };
                for (int i = 0; i < tutorialDiceButtons.Count; i++)
                {
                    tutorialDiceButtons[i].onClick.RemoveAllListeners();
                    if (i < currentDraftPool.Count)
                    {
                        tutorialDiceButtons[i].gameObject.SetActive(true);
                        tutorialDiceButtons[i].interactable = true;
                        tutorialDiceImages[i].sprite = diceSprites[currentDraftPool[i]];
                        Button button = tutorialDiceButtons[i];
                        tutorialDiceButtons[i].onClick.AddListener(() => OnTutorialDiceDrafted(button));
                    }
                    else { tutorialDiceButtons[i].gameObject.SetActive(false); }
                }
                picksRemaining = 3;
                GoToStep(TutorialStep.Stage4_Drafting, recordHistory: false);
                break;
            case TutorialStep.Stage4_Drafting:
                tutorialDraftPanel.SetActive(true);
                instructionText.text = $"'1, 2, 3'�� ������ '��Ʈ����Ʈ'�� �븮����. (���� ����: {picksRemaining}��)";
                break;
            case TutorialStep.Stage4_Complete:
                instructionText.text = "�巡��Ʈ �Ϸ�! '����'�� ���� ������ �غ��ϼ���.";
                nextStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "���� �ܰ��";
                nextStageButton.gameObject.SetActive(true);
                break;

            case TutorialStep.Stage5_Setup:
                instructionText.text = "���� �¸��ϴ� ����� ���ϴ�. ���� '���'�� ��������.";
                ClearBoardAndHand();
                if (tutorialEnemyHpBar != null)
                {
                    tutorialEnemyHpBar.gameObject.SetActive(true);
                    tutorialEnemyCurrentHp = tutorialEnemyMaxHp;
                    tutorialEnemyHpBar.value = tutorialEnemyCurrentHp;
                    if (tutorialEnemyHpText != null)
                    {
                        tutorialEnemyHpText.text = $"{tutorialEnemyCurrentHp} / {tutorialEnemyMaxHp}";
                    }
                }
                playerHand.AddDice(0, 2);
                GoToStep(TutorialStep.Stage5_DirectDamage_Place, recordHistory: false);
                break;
            case TutorialStep.Stage5_DirectDamage_Place:
                instructionText.text = "�ֻ��� 2���� '���'�� �����, �� ���� ��ȯ�Ͽ� ��븦 �����ϼ���.";
                HighlightHandDice(0);
                break;
            case TutorialStep.Stage5_DirectDamage_Summon:
                instructionText.text = "'���' �ϼ�! '��ȯ' ��ư�� ���� ��� HP�� ���ظ� �ּ���.";
                summonButton.interactable = true;
                break;
            case TutorialStep.Stage5_BonusScore_Place:
                instructionText.text = "�����ϴ�! �̹��� �ֻ��� 4���� '��ī��'�� ����� '�߰� ����'�� ������.";
                ClearBoardAndHand();
                playerHand.AddDice(0, 4);
                HighlightHandDice(0);
                break;
            case TutorialStep.Stage5_BonusScore_Summon:
                instructionText.text = "'��ī��' �ϼ�! '��ȯ' ��ư�� ���� ������ ������ Ȯ���ϼ���.";
                summonButton.interactable = true;
                break;
            case TutorialStep.Stage5_Complete:
                instructionText.text = "��� �Ʒ��� ���ƽ��ϴ�! ���� �������� �¸��ϼ���!";
                nextStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ʃ�丮�� �Ϸ�";
                nextStageButton.gameObject.SetActive(true);
                break;
        }
    }

    // --------------------------------------------------
    // # �÷��̾� �Է� ó��
    // --------------------------------------------------
    public void OnDiceSelectedFromHand(int diceNumber)
    {
        selectedDiceNumber = diceNumber;

        if (currentStep == TutorialStep.Stage1_SelectDice) { GoToStep(TutorialStep.Stage1_PlaceDice); }
        else if (currentStep == TutorialStep.Stage2_WaitForFirstDice) { instructionText.text = $"'{diceNumber + 1}' ����. ���忡 ��������."; }
        else if (currentStep == TutorialStep.Stage2_HighlightPairLocations) { instructionText.text = $"'{diceNumber + 1}' ����. ������ ���� ��������."; }
        else if (currentStep == TutorialStep.Stage3_PlaceForTriple) { instructionText.text = $"'{diceNumber + 1}' ����. ���忡 ��������."; }
        else if (currentStep == TutorialStep.Stage5_DirectDamage_Place) { instructionText.text = $"'{diceNumber + 1}' ����. ���忡 ��������."; }
        else if (currentStep == TutorialStep.Stage5_BonusScore_Place) { instructionText.text = $"'{diceNumber + 1}' ����. ���忡 ��������."; }
    }

    public void OnGridTileClicked(int x, int y)
    {
        if (currentStep == TutorialStep.Stage1_PlaceDice)
        {
            boardManager.PlaceDiceOnGrid(selectedDiceNumber, x, y, null);
            if (playerHand.GetTotalDiceCount() > 0) GoToStep(TutorialStep.Stage1_SelectDice);
            else GoToStep(TutorialStep.Stage1_Complete);
        }
        else if (currentStep == TutorialStep.Stage2_WaitForFirstDice)
        {
            boardManager.PlaceDiceOnGrid(selectedDiceNumber, x, y, null);
            firstDicePosition = new Vector2Int(x, y);
            GoToStep(TutorialStep.Stage2_HighlightPairLocations);
        }
        else if (currentStep == TutorialStep.Stage2_HighlightPairLocations || currentStep == TutorialStep.Stage2_WaitForSecondDice)
        {
            boardManager.PlaceDiceOnGrid(selectedDiceNumber, x, y, null);
            List<CompletedYeokInfo> yeoks = boardManager.ScanAllLines();
            if (yeoks.Count > 0 && yeoks.Any(yeok => yeok.YeokType == BaseTreeEnum.pair))
            {
                HideAllHighlights();
                GoToStep(TutorialStep.Stage2_Summon);
            }
            else
            {
                instructionText.text = "����! ������ ��ġ�� ���ƾ� �մϴ�. �ٽ� �õ��ϼ���.";
                StartCoroutine(ResetIncorrectMove(x, y, selectedDiceNumber));
            }
        }
        else if (currentStep == TutorialStep.Stage3_PlaceForTriple)
        {
            boardManager.PlaceDiceOnGrid(selectedDiceNumber, x, y, null);
            if (playerHand.GetTotalDiceCount() == 0)
            {
                List<CompletedYeokInfo> yeoks = boardManager.ScanAllLines();
                if (yeoks.Count > 0 && yeoks.Any(yeok => yeok.YeokType == BaseTreeEnum.triple)) { GoToStep(TutorialStep.Stage3_Summon); }
                else
                {
                    instructionText.text = "����! 'Ʈ����'�� �ƴմϴ�. �ٽ� ��ġ�� ������.";
                    StartCoroutine(ResetStage(TutorialStep.Stage3_Setup));
                }
            }
        }
        else if (currentStep == TutorialStep.Stage5_DirectDamage_Place)
        {
            boardManager.PlaceDiceOnGrid(selectedDiceNumber, x, y, null);
            if (playerHand.GetTotalDiceCount() == 0)
            {
                List<CompletedYeokInfo> yeoks = boardManager.ScanAllLines();
                if (yeoks.Count > 0 && yeoks.Any(yeok => yeok.YeokType == BaseTreeEnum.pair)) { GoToStep(TutorialStep.Stage5_DirectDamage_Summon); }
            }
        }
        else if (currentStep == TutorialStep.Stage5_BonusScore_Place)
        {
            boardManager.PlaceDiceOnGrid(selectedDiceNumber, x, y, null);
            if (playerHand.GetTotalDiceCount() == 0)
            {
                List<CompletedYeokInfo> yeoks = boardManager.ScanAllLines();
                if (yeoks.Count > 0 && yeoks.Any(yeok => yeok.YeokType == BaseTreeEnum.fourCard)) { GoToStep(TutorialStep.Stage5_BonusScore_Summon); }
            }
        }
    }

    public void OnTutorialDiceDrafted(Button clickedButton)
    {
        if (currentStep != TutorialStep.Stage4_Drafting) return;
        int buttonIndex = tutorialDiceButtons.IndexOf(clickedButton);
        if (buttonIndex == -1 || buttonIndex >= currentDraftPool.Count) return;
        int draftedDice = currentDraftPool[buttonIndex];
        playerHand.AddDice(draftedDice, 1);
        draftedDiceThisStage.Add(draftedDice);
        clickedButton.interactable = false;
        picksRemaining--;
        if (picksRemaining <= 0)
        {
            bool hasAllRequiredDice = draftedDiceThisStage.Contains(0) && draftedDiceThisStage.Contains(1) && draftedDiceThisStage.Contains(2);
            if (hasAllRequiredDice) { GoToStep(TutorialStep.Stage4_Complete); }
            else
            {
                instructionText.text = "'��Ʈ����Ʈ'�� ����� ���� �ֻ����� �ƴϿ���. �ٽ� ������ ������.";
                StartCoroutine(ResetStage(TutorialStep.Stage4_Setup));
                return;
            }
        }
        RunCurrentStep();
    }

    private void OnSummonButtonClicked()
    {
        List<CompletedYeokInfo> yeoks = boardManager.ScanAllLines();
        if (currentStep == TutorialStep.Stage2_Summon || currentStep == TutorialStep.Stage3_Summon)
        {
            if (yeoks.Count > 0) { unitManager.SummonUnitsViaRPC(yeoks, null); }
            boardManager.ClearAllDiceViaRPC();
            if (currentStep == TutorialStep.Stage2_Summon) GoToStep(TutorialStep.Stage2_Complete);
            else if (currentStep == TutorialStep.Stage3_Summon) GoToStep(TutorialStep.Stage3_Complete);
        }
        else if (currentStep == TutorialStep.Stage5_DirectDamage_Summon)
        {
            int totalDamage = yeoks.Sum(y => y.BaseScore);
            unitManager.SummonUnitsViaRPC(yeoks, null);
            if (tutorialEnemyHpBar != null)
            {
                tutorialEnemyCurrentHp -= totalDamage;
                tutorialEnemyHpBar.value = tutorialEnemyCurrentHp;
                if (tutorialEnemyHpText != null)
                {
                    tutorialEnemyHpText.text = $"{tutorialEnemyCurrentHp} / {tutorialEnemyMaxHp}";
                }
            }
            boardManager.ClearAllDiceViaRPC();
            instructionText.text = "��뿡�� ���ظ� �������ϴ�! '����'�� ���� ����ϼ���.";
            nextStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "���";
            nextStageButton.gameObject.SetActive(true);
        }
        else if (currentStep == TutorialStep.Stage5_BonusScore_Summon)
        {
            unitManager.SummonUnitsViaRPC(boardManager.ScanAllLines(), null);
            if (unitInfoPanel != null) unitInfoPanel.SetActive(true);
            boardManager.ClearAllDiceViaRPC();
            GoToStep(TutorialStep.Stage5_Complete);
        }
    }

    private void OnNextStageButtonClicked()
    {
        if (currentStep == TutorialStep.Stage1_Complete) GoToStep(TutorialStep.Stage2_Setup);
        else if (currentStep == TutorialStep.Stage2_Complete) GoToStep(TutorialStep.Stage3_Setup);
        else if (currentStep == TutorialStep.Stage3_Complete) GoToStep(TutorialStep.Stage4_Setup);
        else if (currentStep == TutorialStep.Stage4_Complete) GoToStep(TutorialStep.Stage5_Setup);
        else if (currentStep == TutorialStep.Stage5_DirectDamage_Summon) GoToStep(TutorialStep.Stage5_BonusScore_Place);
        else if (currentStep == TutorialStep.Stage5_Complete)
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }

    private void OnPreviousStepButtonClicked()
    {
        if (stepHistory.Count > 0)
        {
            GoToStep(stepHistory.Pop(), recordHistory: false);
        }
    }

    private void OnSkipTutorialClicked()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    // --------------------------------------------------
    // # ���� �Լ�
    // --------------------------------------------------
    private void InitializeTutorialUI()
    {
        if (tutorialMyHpBar != null)
        {
            tutorialPlayerCurrentHp = tutorialPlayerMaxHp;
            tutorialMyHpBar.maxValue = tutorialPlayerMaxHp;
            tutorialMyHpBar.value = tutorialPlayerCurrentHp;
            tutorialMyHpBar.gameObject.SetActive(true);
        }
        if (tutorialEnemyHpBar != null)
        {
            tutorialEnemyCurrentHp = tutorialEnemyMaxHp;
            tutorialEnemyHpBar.maxValue = tutorialEnemyMaxHp;
            tutorialEnemyHpBar.value = tutorialEnemyCurrentHp;
            tutorialEnemyHpBar.gameObject.SetActive(false);
            if (tutorialEnemyHpText != null)
            {
                tutorialEnemyHpText.text = $"{tutorialEnemyCurrentHp} / {tutorialEnemyMaxHp}";
            }
        }
    }

    private void InitializeManagers()
    {
        if (tileContainer == null) { Debug.LogError("TileContainer�� TutorialManager�� ������� �ʾҽ��ϴ�!"); return; }
        Tile[] allTiles = tileContainer.GetComponentsInChildren<Tile>();
        List<Tile> sortedTiles = allTiles.OrderByDescending(t => t.transform.position.y).ThenBy(t => t.transform.position.x).ToList();
        GameObject[,] gridTiles = new GameObject[6, 6];
        GameObject[] perimeterSlots = new GameObject[24];
        for (int i = 0; i < sortedTiles.Count; i++)
        {
            Tile tile = sortedTiles[i];
            int x = i % 8; int y = 7 - (i / 8);
            tile.coordinates = new Vector2Int(x, y);
            if (x >= 1 && x <= 6 && y >= 1 && y <= 6) { gridTiles[x - 1, y - 1] = tile.gameObject; }
            else if (x == 0 || x == 7 || y == 0 || y == 7)
            {
                if ((x == 0 || x == 7) && (y == 0 || y == 7)) continue;
                int index = -1;
                if (y == 7) index = x - 1;
                else if (y == 0) index = 6 + (x - 1);
                else if (x == 0) index = 12 + (y - 1);
                else if (x == 7) index = 18 + (y - 1);
                if (index != -1) perimeterSlots[index] = tile.gameObject;
            }
        }
        if (boardManager != null) boardManager.Initialize(gridTiles);
        if (unitManager != null) unitManager.Initialize(perimeterSlots);
    }

    private void ClearBoardAndHand()
    {
        boardManager.ClearAllDiceViaRPC();
        playerHand.ClearAllDice();
        HideAllHighlights();
    }

    private System.Collections.IEnumerator ResetIncorrectMove(int x, int y, int diceNumberToReturn)
    {
        yield return new WaitForSeconds(1.5f);
        boardManager.PickUpDiceFromGrid(x, y);
        playerHand.AddDice(diceNumberToReturn, 1);
        GoToStep(currentStep, recordHistory: false);
    }

    private System.Collections.IEnumerator ResetStage(TutorialStep stageToRestart)
    {
        yield return new WaitForSeconds(2.0f);
        GoToStep(stageToRestart, recordHistory: false);
    }

    private void HighlightAllPairLocations(Vector2Int origin)
    {
        HideAllHighlights();
        // ���� �ٿ� ���� �� �ֵ��� ����, ���� ��ü�� �˻��մϴ�.
        // ������ ���̶���Ʈ
        for (int x = 0; x < 6; x++)
        {
            if (x == origin.x) continue; // �ڱ� �ڽ��� ����
            if (boardManager.IsTileEmpty(x, origin.y))
            {
                HighlightTile(x, origin.y);
            }
        }
        // ������ ���̶���Ʈ
        for (int y = 0; y < 6; y++)
        {
            if (y == origin.y) continue; // �ڱ� �ڽ��� ����
            if (boardManager.IsTileEmpty(origin.x, y))
            {
                HighlightTile(origin.x, y);
            }
        }
    }

    private void HighlightTile(int x, int y)
    {
        GameObject tileObject = boardManager.GetTileObjectAt(x, y);
        if (tileObject != null && mainCanvas != null)
        {
            GameObject newHighlight = Instantiate(highlightPrefab, mainCanvas.transform);
            RectTransform highlightRect = newHighlight.GetComponent<RectTransform>();
            RectTransform targetRect = tileObject.GetComponent<RectTransform>();
            highlightRect.position = targetRect.position;
            highlightRect.sizeDelta = targetRect.sizeDelta;
            newHighlight.SetActive(true);
            highlightInstances.Add(newHighlight);
        }
    }

    private void HighlightHandDice(int diceIndex)
    {
        if (!playerHand.HasDice(diceIndex))
        {
            HideAllHighlights();
            return;
        }
        RectTransform targetRect = playerHand.GetDiceButtonRect(diceIndex);
        if (targetRect != null && mainCanvas != null)
        {
            GameObject newHighlight = Instantiate(highlightPrefab, mainCanvas.transform);
            RectTransform highlightRect = newHighlight.GetComponent<RectTransform>();
            highlightRect.position = targetRect.position;
            highlightRect.sizeDelta = targetRect.sizeDelta;
            newHighlight.SetActive(true);
            highlightInstances.Add(newHighlight);
        }
    }

    private void HideAllHighlights()
    {
        foreach (var instance in highlightInstances)
        {
            Destroy(instance);
        }
        highlightInstances.Clear();
    }
}