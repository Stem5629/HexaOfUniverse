using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// BattleScene의 전체 게임 상태와 턴 흐름, 플레이어 입력을 관리하는 총감독 스크립트입니다.
/// 모든 턴 전환과 상태 변경은 마스터 클라이언트가 주도하고, RPC를 통해 모든 클라이언트에게 전파합니다.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    // --- 게임 상태 ---
    public enum GameState { WaitingForPlayers, Draft, Player1_Turn, Player2_Turn, ResolvingCombat, GameOver }
    [SerializeField] private GameState currentState;

    [Header("핵심 매니저 및 프리팹")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GameObject unitManagerPrefab; // Resources 폴더에 있어야 합니다.
    [SerializeField] private DraftManager draftManager;
    [SerializeField] private PlayerHand myHand;
    [SerializeField] private Transform tileContainer; // GridLayoutGroup이 있는 부모 오브젝트

    [Header("UI 참조")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TextMeshProUGUI turnIndicatorText;
    [SerializeField] private TextMeshProUGUI roundIndicatorText;

    // --- 참조 변수 ---
    private UnitManager unitManager;
    private Player player1; // MasterClient
    private Player player2; // Other
    private Player currentPlayer;
    private int currentRound = 1;
    private int? selectedDiceNumber = null; // 현재 손패에서 선택한 주사위 눈

    // --- 싱글톤 ---
    public static GameManager Instance;

    #region 초기화 및 설정
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        summonButton.gameObject.SetActive(false);
        turnIndicatorText.text = "상대방을 기다리는 중...";
        roundIndicatorText.text = $"Round {currentRound}";
        summonButton.onClick.AddListener(OnSummonButtonClicked);
        TrySetupPlayers();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupPlayers();
    }

    private void TrySetupPlayers()
    {
        if (PhotonNetwork.PlayerList.Length < 2) return;

        // Player1은 항상 마스터 클라이언트입니다. 이 정보는 모든 컴퓨터에서 동일합니다.
        player1 = PhotonNetwork.MasterClient;

        // 전체 플레이어 목록을 확인하여, 마스터 클라이언트가 아닌 사람을 Player2로 지정합니다.
        // 이 로직 또한 모든 컴퓨터에서 동일한 결과를 보장합니다.
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.IsMasterClient)
            {
                player2 = p;
                break; // Player2를 찾았으니 반복을 종료합니다.
            }
        }

        // UnitManager가 아직 생성되지 않았고, 내가 마스터 클라이언트라면 생성합니다.
        // UnitManager는 게임 당 하나만 존재해야 합니다.
        if (unitManager == null && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("마스터 클라이언트가 UnitManager를 생성합니다.");
            // 1. Resources 폴더에 있는 UnitManager 프리팹을 네트워크 상에 생성합니다.
            GameObject umObject = PhotonNetwork.Instantiate(unitManagerPrefab.name, Vector3.zero, Quaternion.identity);

            // 2. 모든 클라이언트에게 방금 생성한 UnitManager의 정보를 알려줍니다.
            if (umObject != null)
            {
                photonView.RPC("SetUnitManagerRPC", RpcTarget.All, umObject.GetComponent<PhotonView>().ViewID);
            }
        }
    }

    [PunRPC]
    private void SetUnitManagerRPC(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            // --- 바로 이 함수 호출을 위해 아래에 함수를 추가해야 합니다 ---
            SetUnitManager(pv.GetComponent<UnitManager>());
        }
    }

    /// <summary>
    /// 생성된 UnitManager의 참조를 GameManager에 등록하고 초기화를 진행합니다.
    /// (UnitManager.cs에서 이 함수를 호출합니다)
    /// </summary>
    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;
        InitializeField(); // 필드 초기화는 UnitManager가 설정된 후에 실행

        // 모든 클라이언트의 설정이 끝났음을 확인 후, 방장이 드래프트 시작을 명령
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.Draft);
        }
    }

    /// <summary>
    /// 게임 시작 시 타일들을 자동으로 찾아 각 매니저에게 역할을 부여합니다.
    /// </summary>
    private void InitializeField()
    {
        if (tileContainer == null)
        {
            Debug.LogError("치명적 오류: GameManager의 인스펙터에 'Tile Container'가 연결되지 않았습니다!", this.gameObject);
            return;
        }

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

        boardManager.Initialize(gridTiles);
        unitManager.Initialize(perimeterSlots);
        Debug.Log("필드 자동화 설정 완료!");
    }
    #endregion

    #region 상태 관리
    [PunRPC]
    private void ChangeStateRPC(int newStateIndex)
    {
        currentState = (GameState)newStateIndex;
        Debug.Log($"상태 변경 -> {currentState}");

        switch (currentState)
        {
            case GameState.Draft:
                turnIndicatorText.text = "드래프트 진행 중...";
                summonButton.gameObject.SetActive(false);
                draftManager.gameObject.SetActive(true);

                // --- 수정된 부분 ---
                // 1. 모든 클라이언트가 라운드 규칙을 먼저 초기화합니다.
                draftManager.InitializeForRound(currentRound);
                // 2. 마스터 클라이언트만 실제 드래프트를 시작합니다.
                if (PhotonNetwork.IsMasterClient) draftManager.StartDraft();
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
                // 방장만 다음 턴으로 넘어가는 코루틴을 실행
                if (PhotonNetwork.IsMasterClient)
                {
                    StartCoroutine(GoToNextTurnCoroutine());
                }
                break;
            case GameState.GameOver:
                summonButton.gameObject.SetActive(false);
                turnIndicatorText.text = "게임 종료!";
                // TODO: 결과 씬 로드 또는 재시작 버튼 활성화
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
    
    /// <summary>
     /// 다른 스크립트에서 현재 턴의 플레이어 정보를 가져갈 수 있도록 해주는 함수
     /// </summary>
     /// <returns>현재 턴인 Player 객체</returns>
    public Player GetCurrentPlayer()
    {
        return currentPlayer;
    }

    private IEnumerator GoToNextTurnCoroutine()
    {
        // 지속 데미지 정산 (마스터 클라이언트에서만 실행)
        unitManager.ApplyContinuousDamage();

        // 전투 결과를 볼 시간
        yield return new WaitForSeconds(1.5f);

        // --- 수정된 부분 ---

        GameState nextState;

        // 현재 턴이 Player1의 턴이었는가?
        if (currentPlayer == player1)
        {
            // 그렇다면 다음 턴은 Player2의 턴입니다.
            nextState = GameState.Player2_Turn;
        }
        else // 현재 턴이 Player2의 턴이었다면
        {
            // 한 라운드가 종료된 것입니다.
            currentRound++; // 라운드 숫자를 1 증가시킵니다.
            roundIndicatorText.text = $"Round {currentRound}";
            nextState = GameState.Draft;
        }

        // --- 여기까지 수정 ---

        // 모든 클라이언트에게 다음 상태 시작을 알림
        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)nextState);
    }
    #endregion

    #region 플레이어 입력 및 RPC
    public void SelectDiceFromHand(int diceNumber)
    {
        if (!IsMyTurn()) return;
        if (!myHand.HasDice(diceNumber)) return;
        selectedDiceNumber = diceNumber;
        // TODO: 선택된 주사위 버튼에 시각적 효과 표시
    }

    public void OnGridTileClicked(int x, int y)
    {
        if (!IsMyTurn()) return;

        // 1. 행동 요청 (Client -> Master)
        if (selectedDiceNumber != null)
        {
            // 내가 선택한 주사위와 놓을 위치를 마스터 클라이언트에게 "요청"합니다.
            photonView.RPC("RequestPlaceDice", RpcTarget.MasterClient, selectedDiceNumber.Value, x, y);
            selectedDiceNumber = null;
        }
        else
        {
            // 주사위 회수도 마스터 클라이언트에게 "요청"합니다.
            photonView.RPC("RequestPickUpDice", RpcTarget.MasterClient, x, y);
        }
    }

    [PunRPC]
    private void RequestPlaceDice(int diceNumber, int x, int y, PhotonMessageInfo info)
    {
        // 요청한 플레이어가 현재 턴이 맞는지 마스터 클라이언트가 검증합니다.
        if (info.Sender != currentPlayer) return;

        // 여기에 추가적인 유효성 검사(예: 비용 확인 등)를 할 수 있습니다.

        // 2. 상태 동기화 (Master -> All)
        // 모든 클라이언트에게 실제 주사위 배치를 "동기화"하라고 명령합니다.
        photonView.RPC("SyncPlaceDice", RpcTarget.All, diceNumber, x, y, info.Sender);
    }

    [PunRPC]
    private void RequestPickUpDice(int x, int y, PhotonMessageInfo info)
    {
        // 요청한 플레이어가 현재 턴이 맞는지 마스터 클라이언트가 검증합니다.
        if (info.Sender != currentPlayer) return;

        // 모든 클라이언트에게 실제 주사위 회수를 "동기화"하라고 명령합니다.
        photonView.RPC("SyncPickUpDice", RpcTarget.All, x, y);
    }

    [PunRPC]
    private void SyncPlaceDice(int diceNumber, int x, int y, Player placingPlayer)
    {
        // BoardManager의 주사위 배치 로직을 호출합니다.
        // 이 함수는 모든 클라이언트에서 실행되어, 모두의 보드 상태를 동일하게 만듭니다.
        boardManager.PlaceDiceOnGrid(diceNumber, x, y, placingPlayer);
    }

    [PunRPC]
    private void SyncPickUpDice(int x, int y)
    {
        // BoardManager의 주사위 회수 로직을 호출합니다.
        boardManager.PickUpDiceFromGrid(x, y);
    }

    public void OnSummonButtonClicked()
    {
        // 버튼을 누른 플레이어는 "내 턴을 마치겠다"고 방장에게 알립니다.
        photonView.RPC("FinishTurnRPC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void FinishTurnRPC(Player playerWhoFinished)
    {
        if (!PhotonNetwork.IsMasterClient || playerWhoFinished != currentPlayer) return;

        List<CompletedYeokInfo> completedYeoks = boardManager.ScanAllLines();

        // 역이 있다면 유닛을 소환하는 로직은 그대로 둡니다.
        if (completedYeoks.Count > 0)
        {
            unitManager.SummonUnitsViaRPC(completedYeoks, playerWhoFinished);
            // boardManager.ClearUsedDiceViaRPC(completedYeoks); <-- 이 줄은 삭제합니다.
        }

        // --- 수정된 부분 ---
        // 역의 존재 여부와 상관없이, 항상 보드 전체를 초기화하는 새 함수를 호출합니다.
        boardManager.ClearAllDiceViaRPC();

        // 모든 클라이언트의 상태를 '전투 정산'으로 변경하라고 명령
        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.ResolvingCombat);
    }

    public void EndDraftPhase(int[] p1Dice, int[] p2Dice)
    {
        // 드래프트 결과는 각 클라이언트의 DraftManager가 로컬로 계산하므로,
        // GameManager는 자신의 손패만 채우면 됩니다.

        // 각 클라이언트의 DraftManager는 자신의 주사위 결과를 첫 번째 인자로 넘겨주므로,
        // p1Dice가 항상 "내 주사위"가 됩니다.
        int[] myDiceResult = p1Dice; // <-- 이렇게 수정하세요!

        for (int i = 0; i < myDiceResult.Length; i++)
        {
            if (myDiceResult[i] > 0) myHand.AddDice(i, myDiceResult[i]);
        }

        // 방장만 첫 턴 시작을 알립니다.
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.Player1_Turn);
        }
    }
    #endregion

    #region 헬퍼 함수
    private bool IsMyTurn()
    {
        if (currentState != GameState.Player1_Turn && currentState != GameState.Player2_Turn) return false;
        return currentPlayer.IsLocal;
    }

    private int GetPerimeterIndexFromCoords(int x, int y)
    {
        if (y == 7) return x - 1;
        if (y == 0) return 6 + (x - 1);
        if (x == 0) return 12 + (y - 1);
        if (x == 7) return 18 + (y - 1);
        return -1;
    }
    #endregion
}