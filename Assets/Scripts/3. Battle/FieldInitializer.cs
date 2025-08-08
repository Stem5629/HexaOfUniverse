using UnityEngine;
using System.Linq;

public class FieldInitializer : MonoBehaviour
{
    [Header("����")]
    [SerializeField] private Transform tileContainer; // GridLayoutGroup�� �ִ� �θ� ������Ʈ
    [SerializeField] private Tile bottomLeftTile;   // (0,0) ��ġ�� �ִ� Ÿ�� ������Ʈ
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UnitManager unitManager;

    [Header("�׸��� ����")]
    [SerializeField] private float cellSize = 90f;
    [SerializeField] private float spacing = 10f;

    void Start()
    {
        InitializeField();
    }

    private void InitializeField()
    {
        // 1. �����̳��� ��� �ڽ� Ÿ�ϵ��� �����ɴϴ�.
        Tile[] allTiles = tileContainer.GetComponentsInChildren<Tile>();
        float step = cellSize + spacing; // �� ĭ�� ���� ���� �Ÿ�

        // 2. �� Ÿ���� �׸��� ��ǥ�� ����մϴ�.
        foreach (Tile tile in allTiles)
        {
            Vector2 relativePos = (Vector2)tile.transform.position - (Vector2)bottomLeftTile.transform.position;
            int x = Mathf.RoundToInt(relativePos.x / step);
            int y = Mathf.RoundToInt(relativePos.y / step);
            tile.coordinates = new Vector2Int(x, y);
        }

        // 3. ���� ��ǥ�� �������� Ÿ�ϵ��� �� �Ŵ������� �й��մϴ�.
        // 8x8 �׸��� ����, 1~6�� ����, 0�� 7�� �ܰ�
        GameObject[,] gridTiles = new GameObject[6, 6];
        GameObject[] perimeterSlots = new GameObject[24];

        foreach (Tile tile in allTiles)
        {
            int x = tile.coordinates.x;
            int y = tile.coordinates.y;

            // ���� 6x6 �׸��� Ÿ���� ���
            if (x >= 1 && x <= 6 && y >= 1 && y <= 6)
            {
                gridTiles[x - 1, y - 1] = tile.gameObject;
            }
            // �ܰ� 24ĭ Ÿ���� ��� (�ڳ� ����)
            else if (x == 0 || x == 7 || y == 0 || y == 7)
            {
                if ((x == 0 || x == 7) && (y == 0 || y == 7)) continue; // �ڳʴ� ����

                int index = GetPerimeterIndexFromCoords(x, y);
                perimeterSlots[index] = tile.gameObject;
            }
        }

        // 4. �� �Ŵ������� ���ĵ� �迭�� �����Ͽ� �ʱ�ȭ�մϴ�.
        boardManager.Initialize(gridTiles);
        unitManager.Initialize(perimeterSlots);

        Debug.Log("�ʵ� �ڵ�ȭ ���� �Ϸ�!");
    }

    // 8x8 �׸����� �ܰ� ��ǥ(x,y)�� 24ĭ �迭�� �ε����� ��ȯ�ϴ� �Լ�
    private int GetPerimeterIndexFromCoords(int x, int y)
    {
        if (y == 7) return x - 1;       // ��� (0~5)
        if (y == 0) return 6 + (x - 1); // �ϴ� (6~11)
        if (x == 0) return 12 + (y - 1); // ���� (12~17)
        if (x == 7) return 18 + (y - 1); // ���� (18~23)
        return -1; // �ش� ����
    }
}