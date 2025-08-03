using System.Collections.Generic;

public class CombinationGenerator
{
    private List<List<int>> results;
    private int numFaces;
    private int handSize;
    private int[] faces;

    /// <summary>
    /// 중복 조합 생성을 시작하는 메인 함수
    /// </summary>
    /// <param name="n">뽑을 수 있는 총 종류의 수 (예: 6개의 주사위 눈)</param>
    /// <param name="k">뽑을 개수 (예: 3개짜리 조합)</param>
    /// <returns>모든 조합이 담긴 리스트</returns>
    public List<List<int>> Generate(int n, int k)
    {
        results = new List<List<int>>();
        numFaces = n;
        handSize = k;
        faces = new int[n];
        for (int i = 0; i < n; i++)
        {
            faces[i] = i;
        }

        FindCombinationsRecursive(0, new List<int>());
        return results;
    }

    /// <summary>
    /// 재귀적으로 조합을 찾는 핵심 함수
    /// </summary>
    /// <param name="startIndex">탐색을 시작할 주사위 눈의 인덱스</param>
    /// <param name="currentCombination">현재까지 만들어진 조합</param>
    private void FindCombinationsRecursive(int startIndex, List<int> currentCombination)
    {
        if (currentCombination.Count == handSize)
        {
            results.Add(new List<int>(currentCombination));
            return;
        }

        for (int i = startIndex; i < numFaces; i++)
        {
            currentCombination.Add(faces[i]);
            FindCombinationsRecursive(i, currentCombination);
            currentCombination.RemoveAt(currentCombination.Count - 1);
        }
    }
}