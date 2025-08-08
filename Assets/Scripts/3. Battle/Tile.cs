using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    // 이 타일의 그리드 좌표 (예: (0,0), (3,5) 등)
    public Vector2Int coordinates;

    // GameManager 등에서 참조할 수 있도록 Button 컴포넌트를 미리 찾아둡니다.
    [HideInInspector] public Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
}