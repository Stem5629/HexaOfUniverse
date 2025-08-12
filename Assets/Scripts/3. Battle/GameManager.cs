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

    [SerializeField] private GameObject infoPanel; // 정보창 패널 오브젝트
    [SerializeField] private TextMeshProUGUI unitTreeText;
    [SerializeField] private TextMeshProUGUI treeCombinationText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI beginDamageText;
    [SerializeField] private TextMeshProUGUI keepDamageText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("게임 규칙 설정")]
    [SerializeField] private float turnTimeLimit = 60f;

    // --- 참조 변수 ---
    private UnitManager unitManager;
    private Player player1; // MasterClient
    private Player player2; // Other
    private Player currentPlayer;
    private Player winner;
    private int currentRound = 1;
    private int? selectedDiceNumber = null; // 현재 손패에서 선택한 주사위 눈
    private Coroutine turnTimerCoroutine;

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

        player1 = PhotonNetwork.MasterClient;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.IsMasterClient)
            {
                player2 = p;
                break;
            }
        }

        if (unitManager == null && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("마스터 클라이언트가 UnitManager를 생성합니다.");
            GameObject umObject = PhotonNetwork.Instantiate(unitManagerPrefab.name, Vector3.zero, Quaternion.identity);

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
            SetUnitManager(pv.GetComponent<UnitManager>());
        }
    }

    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;

        manager.infoPanel = this.infoPanel;
        manager.unitTreeText = this.unitTreeText;
        manager.treeCombinationText = this.treeCombinationText;
        manager.hpText = this.hpText;
        manager.beginDamageText = this.beginDamageText;
        manager.keepDamageText = this.keepDamageText;

        InitializeField();

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.Draft, currentRound);
        }
    }

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
    private void ChangeStateRPC(int newStateIndex, int newRound = -1)
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        if (newRound != -1)
        {
            this.currentRound = newRound;
        }

        currentState = (GameState)newStateIndex;
        Debug.Log($"상태 변경 -> {currentState}");

        switch (currentState)
        {
            case GameState.Draft:
                roundIndicatorText.text = $"Round {currentRound}";
                turnIndicatorText.text = "드래프트 진행 중...";
                timerText.text = "";
                summonButton.gameObject.SetActive(false);
                draftManager.gameObject.SetActive(true);
                bool isP1Starting = (currentRound % 2 == 1);
                draftManager.InitializeForRound(currentRound, isP1Starting);

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
                if (PhotonNetwork.IsMasterClient)
                {
                    StartCoroutine(GoToNextTurnCoroutine());
                }
                break;
            case GameState.GameOver:
                summonButton.gameObject.SetActive(false);
                draftManager.gameObject.SetActive(false);
                if (winner == null)
                {
                    turnIndicatorText.text = "무승부!";
                }
                else
                {
                    turnIndicatorText.text = $"{winner.NickName}님의 승리!";
                }
                break;
        }
    }

    private void SetupTurn()
    {
        turnIndicatorText.text = $"{currentPlayer.NickName}의 턴";
        boardManager.currentTurnPlayerHand = currentPlayer.IsLocal ? myHand : null;
        summonButton.gameObject.SetActive(true);
        summonButton.interactable = currentPlayer.IsLocal;

        // --- 아래 타이머 시작 코드를 추가합니다 ---
        // 마스터 클라이언트만 타이머를 시작하고, RPC로 모든 클라이언트에게 시간을 동기화합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            StartTurnTimer();
        }
    }

    public Player GetCurrentPlayer()
    {
        return currentPlayer;
    }

    private IEnumerator GoToNextTurnCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        player1.CustomProperties.TryGetValue("HP", out object p1HpObj);
        player2.CustomProperties.TryGetValue("HP", out object p2HpObj);
        int p1Hp = (p1HpObj != null) ? (int)p1HpObj : 100;
        int p2Hp = (p2HpObj != null) ? (int)p2HpObj : 100;

        bool p1Lost = p1Hp <= 0;
        bool p2Lost = p2Hp <= 0;

        if (p1Lost || p2Lost)
        {
            if (p1Lost && p2Lost) // 1. 둘 다 패배한 경우
            {
                // HP가 더 낮은 쪽이 패배합니다.
                if (p1Hp < p2Hp)
                {
                    winner = player2; // P1의 HP가 더 낮으므로 P2가 승리
                }
                else if (p2Hp < p1Hp)
                {
                    winner = player1; // P2의 HP가 더 낮으므로 P1이 승리
                }
                else // p1Hp == p2Hp
                {
                    winner = null; // HP가 정확히 같다면 무승부
                }
            }
            else if (p1Lost) // 2. P1만 패배한 경우
            {
                winner = player2;
            }
            else // 3. P2만 패배한 경우
            {
                winner = player1;
            }

            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.GameOver, -1);
            yield break;
        }

        GameState nextState;
        int nextRound = -1;

        bool isP1Starter = (currentRound % 2 == 1);
        Player roundEndPlayer = isP1Starter ? player2 : player1;

        if (currentPlayer == roundEndPlayer)
        {
            // 라운드가 끝나는 시점에만 지속 데미지를 적용합니다.
            unitManager.ApplyContinuousDamage();

            currentRound++;
            nextRound = currentRound;
            nextState = GameState.Draft;
        }
        else
        {
            nextState = (roundEndPlayer == player1) ? GameState.Player1_Turn : GameState.Player2_Turn;
        }

        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)nextState, nextRound);
    }
    #endregion

    #region 플레이어 입력 및 RPC
    public void SelectDiceFromHand(int diceNumber)
    {
        if (!IsMyTurn()) return;
        if (!myHand.HasDice(diceNumber)) return;
        selectedDiceNumber = diceNumber;
    }

    public void OnGridTileClicked(int x, int y)
    {
        if (!IsMyTurn()) return;

        if (selectedDiceNumber != null)
        {
            photonView.RPC("RequestPlaceDice", RpcTarget.MasterClient, selectedDiceNumber.Value, x, y);
            selectedDiceNumber = null;
        }
        else
        {
            photonView.RPC("RequestPickUpDice", RpcTarget.MasterClient, x, y);
        }
    }

    [PunRPC]
    private void RequestPlaceDice(int diceNumber, int x, int y, PhotonMessageInfo info)
    {
        if (info.Sender != currentPlayer) return;
        photonView.RPC("SyncPlaceDice", RpcTarget.All, diceNumber, x, y, info.Sender);
    }

    [PunRPC]
    private void RequestPickUpDice(int x, int y, PhotonMessageInfo info)
    {
        if (info.Sender != currentPlayer) return;
        photonView.RPC("SyncPickUpDice", RpcTarget.All, x, y);
    }

    [PunRPC]
    private void SyncPlaceDice(int diceNumber, int x, int y, Player placingPlayer)
    {
        boardManager.PlaceDiceOnGrid(diceNumber, x, y, placingPlayer);
    }

    [PunRPC]
    private void SyncPickUpDice(int x, int y)
    {
        boardManager.PickUpDiceFromGrid(x, y);
    }

    public void OnSummonButtonClicked()
    {
        photonView.RPC("FinishTurnRPC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    private void FinishTurnRPC(Player playerWhoFinished)
    {
        if (!PhotonNetwork.IsMasterClient || playerWhoFinished != currentPlayer) return;

        List<CompletedYeokInfo> completedYeoks = boardManager.ScanAllLines();

        if (completedYeoks.Count > 0)
        {
            unitManager.SummonUnitsViaRPC(completedYeoks, playerWhoFinished);
        }

        boardManager.ClearAllDiceViaRPC();

        photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)GameState.ResolvingCombat, -1);
    }

    public void EndDraftPhase(int[] p1Dice, int[] p2Dice)
    {
        int[] myDiceResult = p1Dice;
        for (int i = 0; i < myDiceResult.Length; i++)
        {
            if (myDiceResult[i] > 0) myHand.AddDice(i, myDiceResult[i]);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            bool isP1Starting = (currentRound % 2 == 1);
            GameState firstTurnState = isP1Starting ? GameState.Player1_Turn : GameState.Player2_Turn;
            photonView.RPC("ChangeStateRPC", RpcTarget.All, (int)firstTurnState, -1);
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

    private void StartTurnTimer()
    {
        // 이전에 실행되던 타이머가 있다면 중지시킵니다.
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }
        // 새로운 타이머 코루틴을 시작합니다.
        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    private IEnumerator TurnTimerCoroutine()
    {
        float remainingTime = turnTimeLimit;
        while (remainingTime > 0)
        {
            // 모든 클라이언트에게 남은 시간을 동기화하여 UI를 업데이트하도록 명령합니다.
            photonView.RPC("UpdateTimerRPC", RpcTarget.All, remainingTime);

            yield return new WaitForSeconds(1.0f);
            remainingTime--;
        }

        // 시간이 0이 되면 턴을 강제로 종료합니다.
        Debug.Log($"{currentPlayer.NickName}님의 턴 시간이 초과되었습니다.");
        photonView.RPC("UpdateTimerRPC", RpcTarget.All, 0f); // UI를 0으로 맞춤

        // 현재 턴인 플레이어가 강제로 '소환' 버튼을 누른 것처럼 처리합니다.
        photonView.RPC("FinishTurnRPC", RpcTarget.MasterClient, currentPlayer);
    }

    [PunRPC]
    private void UpdateTimerRPC(float time)
    {
        // 초를 분:초 형식의 문자열로 변환합니다. (예: 60 -> 01:00)
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    #endregion
}