using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// BattleScene의 전체 게임 상태와 턴 흐름, 플레이어 입력을 관리하는 총감독 스크립트입니다.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    // --- 게임 상태 ---
    public enum GameState { WaitingForPlayers, Draft, Player1_Turn, Player2_Turn, ResolvingCombat, GameOver }
    [SerializeField] private GameState currentState;

    [Header("핵심 매니저 및 UI 참조")]
    [SerializeField] private BoardManager boardManager;
    //[SerializeField] private UnitManager unitManager;
    private UnitManager unitManager;
    [SerializeField] private GameObject unitManagerPrefab;
    [SerializeField] private DraftManager draftManager;
    [SerializeField] private PlayerHand myHand;
    [SerializeField] private Transform tileContainer; // GridLayoutGroup이 있는 부모 오브젝트

    [Header("UI 참조")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TextMeshProUGUI turnIndicatorText;

    // --- 플레이어 정보 ---
    private Player player1; // MasterClient
    private Player player2; // Other
    private Player currentPlayer;
    private int currentRound = 1;

    // --- 플레이어 입력 상태 ---
    private int? selectedDiceNumber = null; // 현재 손패에서 선택한 주사위 눈

    // 싱글톤
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- 아래 안전장치 추가 ---
        if (tileContainer == null)
        {
            Debug.LogError("치명적 오류: GameManager의 인스펙터에 'Tile Container'가 연결되지 않았습니다!", this.gameObject);
            return; // 초기화를 중단하여 추가 오류 방지
        }

        InitializeField(); // 이제 tileContainer가 null이 아님이 보장됨
    }

    void Start()
    {
        summonButton.gameObject.SetActive(false);
        turnIndicatorText.text = "상대방을 기다리는 중...";
        summonButton.onClick.AddListener(OnSummonButtonClicked);
        TrySetupPlayers();
    }

    #region 초기화 및 상태 관리

    /// <summary>
    /// 게임 시작 시 타일들을 자동으로 찾아 각 매니저에게 역할을 부여합니다.
    /// </summary>
    private void InitializeField()
    {
        Tile[] allTiles = tileContainer.GetComponentsInChildren<Tile>();
        List<Tile> sortedTiles = allTiles
            .OrderByDescending(t => t.transform.position.y)
            .ThenBy(t => t.transform.position.x)
            .ToList();

        GameObject[,] gridTiles = new GameObject[6, 6];
        GameObject[] perimeterSlots = new GameObject[24];

        for (int i = 0; i < sortedTiles.Count; i++)
        {
            Tile tile = sortedTiles[i];
            int x = i % 8;
            int y = 7 - (i / 8);
            tile.coordinates = new Vector2Int(x, y);

            if (x >= 1 && x <= 6 && y >= 1 && y <= 6)
            {
                gridTiles[x - 1, y - 1] = tile.gameObject;
            }
            else if (x == 0 || x == 7 || y == 0 || y == 7)
            {
                if ((x == 0 || x == 7) && (y == 0 || y == 7)) continue;
                int index = GetPerimeterIndexFromCoords(x, y);
                if (index != -1) perimeterSlots[index] = tile.gameObject;
            }
        }

        // --- 아래 검증 로직 추가 ---
        // 각 매니저에게 배열을 전달하기 전에, 배열이 완벽하게 채워졌는지 검사합니다.
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                if (gridTiles[x, y] == null)
                {
                    // 어떤 좌표의 타일이 비어있는지 정확하게 알려줍니다.
                    Debug.LogError($"[FieldInitializer Error] 내부 그리드 타일 (x:{x}, y:{y})이 설정되지 않았습니다! 씬의 타일 개수와 배치를 확인하세요.");
                    return; // 문제가 있으면 초기화를 중단
                }
            }
        }

        boardManager.Initialize(gridTiles);
        unitManager.Initialize(perimeterSlots);
        Debug.Log("필드 자동화 설정 완료!");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupPlayers();
    }

    private void TrySetupPlayers()
    {
        if (PhotonNetwork.PlayerList.Length < 2) return;
        player1 = PhotonNetwork.MasterClient;
        player2 = PhotonNetwork.PlayerListOthers[0];

        // --- 수정된 부분 2: 마스터 클라이언트가 UnitManager를 네트워크에 생성 ---
        if (PhotonNetwork.IsMasterClient)
        {
            // "UnitManager"는 Resources 폴더에 있는 프리팹의 이름과 정확히 일치해야 합니다.
            PhotonNetwork.Instantiate("UnitManager", Vector3.zero, Quaternion.identity);
        }

        ChangeState(GameState.Draft);
    }

    // 씬에 UnitManager가 생성되면 이 함수를 호출하여 참조를 연결
    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case GameState.Draft:
                turnIndicatorText.text = "드래프트 진행 중...";
                summonButton.gameObject.SetActive(false);
                draftManager.StartDraft(currentRound);
                break;
            case GameState.Player1_Turn:
                currentPlayer = player1;
                SetupTurn();
                break;
            case GameState.Player2_Turn:
                currentPlayer = player2;
                SetupTurn();
                break;
            case GameState.ResolvingCombat:
                summonButton.gameObject.SetActive(false);
                turnIndicatorText.text = "전투 정산 중...";

                // 마스터 클라이언트만 데미지 정산을 실행하여 중복 계산 방지
                if (PhotonNetwork.IsMasterClient)
                {
                    unitManager.ApplyContinuousDamage();
                }

                // 1초 후 다음 턴 시작 (전투 결과를 볼 시간)
                Invoke("GoToNextTurn", 1.0f);
                break;
            case GameState.GameOver:
                summonButton.gameObject.SetActive(false);
                turnIndicatorText.text = "게임 종료!";
                break;
        }
    }

    private void SetupTurn()
    {
        turnIndicatorText.text = $"{currentPlayer.NickName}의 턴";
        boardManager.currentTurnPlayerHand = currentPlayer.IsLocal ? myHand : null;
        summonButton.gameObject.SetActive(true);
        summonButton.interactable = currentPlayer.IsLocal;
    }

    private void GoToNextTurn()
    {
        if (currentPlayer == player1) ChangeState(GameState.Player2_Turn);
        else ChangeState(GameState.Player1_Turn);
    }

    #endregion

    #region 플레이어 입력 처리 (UI 호출)

    /// <summary>
    /// PlayerHand의 주사위 버튼을 클릭했을 때 호출될 함수
    /// </summary>
    public void SelectDiceFromHand(int diceNumber)
    {
        if (currentState != GameState.Player1_Turn && currentState != GameState.Player2_Turn) return;
        if (!currentPlayer.IsLocal) return; // 내 턴이 아니면 선택 불가
        if (!myHand.HasDice(diceNumber)) return;

        selectedDiceNumber = diceNumber;
        Debug.Log($"{diceNumber + 1}번 눈 주사위 선택됨.");
        // TODO: 선택된 주사위 버튼에 테두리 등 시각적 효과 표시
    }

    /// <summary>
    /// 6x6 그리드의 타일을 클릭했을 때 호출될 함수
    /// </summary>
    public void OnGridTileClicked(int x, int y)
    {
        if (currentState != GameState.Player1_Turn && currentState != GameState.Player2_Turn) return;
        if (!currentPlayer.IsLocal) return; // 내 턴이 아니면 행동 불가

        if (selectedDiceNumber != null)
        {
            // 손패에서 선택한 주사위가 있으면 -> 배치 시도
            boardManager.PlaceDiceOnGrid(selectedDiceNumber.Value, x, y);
            selectedDiceNumber = null; // 배치 후 선택 상태 해제
            // TODO: 주사위 버튼의 선택 효과 해제
        }
        else
        {
            // 선택한 주사위가 없으면 -> 타일에 놓인 주사위 회수 시도
            boardManager.PickUpDiceFromGrid(x, y);
        }
    }

    /// <summary>
    /// '소환' 버튼이 눌렸을 때 호출될 함수
    /// </summary>
    public void OnSummonButtonClicked()
    {
        List<CompletedYeokInfo> completedYeoks = boardManager.ScanAllLines();

        if (completedYeoks.Count > 0)
        {
            unitManager.SummonUnitsViaRPC(completedYeoks, PhotonNetwork.LocalPlayer);
            boardManager.ClearUsedDice(completedYeoks);
        }
        else
        {
            Debug.Log("완성된 역이 없어 소환할 유닛이 없습니다. 턴을 넘깁니다.");
        }

        ChangeState(GameState.ResolvingCombat);
    }

    #endregion

    /// <summary>
    /// DraftManager가 드래프트를 모두 마친 후 호출할 함수
    /// </summary>
    public void EndDraftPhase(int[] p1Dice, int[] p2Dice)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < p1Dice.Length; i++) myHand.AddDice(i, p1Dice[i]);
        }
        else
        {
            for (int i = 0; i < p2Dice.Length; i++) myHand.AddDice(i, p2Dice[i]);
        }

        Debug.Log("드래프트가 종료되었습니다. P1의 턴부터 시작합니다.");
        ChangeState(GameState.Player1_Turn);
    }

    private int GetPerimeterIndexFromCoords(int x, int y)
    {
        if (y == 7) return x - 1;
        if (y == 0) return 6 + (x - 1);
        if (x == 0) return 12 + (y - 1);
        if (x == 7) return 18 + (y - 1);
        return -1;
    }
}