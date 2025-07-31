using System.Collections.Generic;

public class CombinationGenerator
{
    private List<List<int>> results;
    private int numFaces;
    private int handSize;
    private int[] faces;

    /// <summary>
    /// �ߺ� ���� ������ �����ϴ� ���� �Լ�
    /// </summary>
    /// <param name="n">���� �� �ִ� �� ������ �� (��: 6���� �ֻ��� ��)</param>
    /// <param name="k">���� ���� (��: 3��¥�� ����)</param>
    /// <returns>��� ������ ��� ����Ʈ</returns>
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
    /// ��������� ������ ã�� �ٽ� �Լ�
    /// </summary>
    /// <param name="startIndex">Ž���� ������ �ֻ��� ���� �ε���</param>
    /// <param name="currentCombination">������� ������� ����</param>
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