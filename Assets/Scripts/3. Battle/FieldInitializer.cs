using UnityEngine;
using System.Linq;

public class FieldInitializer : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform tileContainer; // GridLayoutGroup이 있는 부모 오브젝트
    [SerializeField] private Tile bottomLeftTile;   // (0,0) 위치에 있는 타일 오브젝트
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UnitManager unitManager;

    [Header("그리드 설정")]
    [SerializeField] private float cellSize = 90f;
    [SerializeField] private float spacing = 10f;

    void Start()
    {
        InitializeField();
    }

    private void InitializeField()
    {
        // 1. 컨테이너의 모든 자식 타일들을 가져옵니다.
        Tile[] allTiles = tileContainer.GetComponentsInChildren<Tile>();
        float step = cellSize + spacing; // 한 칸의 실제 월드 거리

        // 2. 각 타일의 그리드 좌표를 계산합니다.
        foreach (Tile tile in allTiles)
        {
            Vector2 relativePos = (Vector2)tile.transform.position - (Vector2)bottomLeftTile.transform.position;
            int x = Mathf.RoundToInt(relativePos.x / step);
            int y = Mathf.RoundToInt(relativePos.y / step);
            tile.coordinates = new Vector2Int(x, y);
        }

        // 3. 계산된 좌표를 바탕으로 타일들을 각 매니저에게 분배합니다.
        // 8x8 그리드 기준, 1~6이 내부, 0과 7이 외곽
        GameObject[,] gridTiles = new GameObject[6, 6];
        GameObject[] perimeterSlots = new GameObject[24];

        foreach (Tile tile in allTiles)
        {
            int x = tile.coordinates.x;
            int y = tile.coordinates.y;

            // 내부 6x6 그리드 타일인 경우
            if (x >= 1 && x <= 6 && y >= 1 && y <= 6)
            {
                gridTiles[x - 1, y - 1] = tile.gameObject;
            }
            // 외곽 24칸 타일인 경우 (코너 제외)
            else if (x == 0 || x == 7 || y == 0 || y == 7)
            {
                if ((x == 0 || x == 7) && (y == 0 || y == 7)) continue; // 코너는 무시

                int index = GetPerimeterIndexFromCoords(x, y);
                perimeterSlots[index] = tile.gameObject;
            }
        }

        // 4. 각 매니저에게 정렬된 배열을 전달하여 초기화합니다.
        boardManager.Initialize(gridTiles);
        unitManager.Initialize(perimeterSlots);

        Debug.Log("필드 자동화 설정 완료!");
    }

    // 8x8 그리드의 외곽 좌표(x,y)를 24칸 배열의 인덱스로 변환하는 함수
    private int GetPerimeterIndexFromCoords(int x, int y)
    {
        if (y == 7) return x - 1;       // 상단 (0~5)
        if (y == 0) return 6 + (x - 1); // 하단 (6~11)
        if (x == 0) return 12 + (y - 1); // 좌측 (12~17)
        if (x == 7) return 18 + (y - 1); // 우측 (18~23)
        return -1; // 해당 없음
    }
}