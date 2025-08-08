using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    // �� Ÿ���� �׸��� ��ǥ (��: (0,0), (3,5) ��)
    public Vector2Int coordinates;

    // GameManager ��� ������ �� �ֵ��� Button ������Ʈ�� �̸� ã�ƵӴϴ�.
    [HideInInspector] public Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
}