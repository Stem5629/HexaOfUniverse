using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Linq 사용을 위해 추가


public class ProbabilityLoader : MonoBehaviour
{
    // 불러온 확률 데이터를 저장할 중첩 딕셔너리
    // [Key: 주사위 개수, Value: [Key: 역 이름, Value: 확률]]
    public Dictionary<int, Dictionary<BaseTreeEnum, float>> loadedProbabilities;

    // 다른 스크립트에서 쉽게 접근할 수 있도록 싱글톤 인스턴스를 만듭니다.
    public static ProbabilityLoader Instance { get; private set; }

    void Awake()
    {
        // 싱글톤 패턴 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 게임이 시작될 때 CSV 파일에서 데이터를 불러옵니다.
        LoadProbabilityData();
    }

    /// <summary>
    /// Assets/ProbabilityResults.csv 파일에서 확률 데이터를 불러옵니다.
    /// </summary>
    public void LoadProbabilityData()
    {
        // 데이터 구조 초기화
        loadedProbabilities = new Dictionary<int, Dictionary<BaseTreeEnum, float>>();
        string filePath = Path.Combine(Application.dataPath, "ProbabilityResults.csv");

        if (!File.Exists(filePath))
        {
            Debug.LogError("파일을 찾을 수 없습니다: " + filePath);
            return;
        }

        try
        {
            // StreamReader를 사용하여 파일을 줄 단위로 읽습니다.
            using (var reader = new StreamReader(filePath))
            {
                // 1. 헤더 읽기 (주사위 개수 정보를 얻기 위해)
                string headerLine = reader.ReadLine();
                if (headerLine == null) return;

                // 헤더를 쉼표로 분리 (ex: "역 이름", "7개 풀 (%)", "10개 풀 (%)", ...)
                string[] headers = headerLine.Split(',');

                // "7개 풀 (%)"에서 숫자 7만 추출하여 주사위 개수 리스트를 만듭니다.
                List<int> diceCountsInFile = headers
                    .Skip(1) // 첫 번째 "역 이름" 열은 건너뜁니다.
                    .Select(header => int.Parse(header.Split('개')[0].Trim()))
                    .ToList();

                // 각 주사위 개수에 대한 내부 딕셔셔리를 초기화합니다.
                foreach (int count in diceCountsInFile)
                {
                    loadedProbabilities[count] = new Dictionary<BaseTreeEnum, float>();
                }

                // 2. 데이터 행 읽기
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    // 첫 번째 값(역 이름)을 BaseTreeEnum 타입으로 변환합니다.
                    BaseTreeEnum yeok = (BaseTreeEnum)System.Enum.Parse(typeof(BaseTreeEnum), values[0]);

                    // 각 주사위 개수에 해당하는 확률 값을 저장합니다.
                    for (int i = 0; i < diceCountsInFile.Count; i++)
                    {
                        int currentDiceCount = diceCountsInFile[i]; // ex: 7, 10, 13, ...
                        float probability = float.Parse(values[i + 1]); // ex: 99.9800, 100.0000, ...

                        // 최종 데이터 구조에 저장
                        loadedProbabilities[currentDiceCount][yeok] = probability;
                    }
                }
            }

            Debug.Log("CSV 확률 데이터를 성공적으로 불러왔습니다!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSV 파일 처리 중 오류 발생: " + e.Message);
        }
    }

    /// <summary>
    /// 저장된 데이터에서 특정 확률 값을 가져오는 예제 함수
    /// </summary>
    /// <param name="diceCount">가져오고 싶은 주사위 개수</param>
    /// <param name="yeok">가져오고 싶은 역의 종류</param>
    /// <returns>저장된 확률 값. 데이터가 없으면 -1을 반환합니다.</returns>
    public float GetProbability(int diceCount, BaseTreeEnum yeok)
    {
        if (loadedProbabilities != null && loadedProbabilities.ContainsKey(diceCount))
        {
            if (loadedProbabilities[diceCount].ContainsKey(yeok))
            {
                return loadedProbabilities[diceCount][yeok];
            }
        }

        // 요청한 데이터가 없는 경우
        Debug.LogWarning($"확률 데이터를 찾을 수 없습니다: 주사위 {diceCount}개, 역 {yeok}");
        return -1f; // 에러를 의미하는 값
    }
}