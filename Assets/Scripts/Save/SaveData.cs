using System;
using System.Collections.Generic;

namespace RedCliffMystery.Save
{
    /// <summary>
    /// 저장 파일 하나에 들어가는 전체 데이터.
    /// JsonUtility는 Dictionary를 직렬화하지 못하기 때문에,
    /// 키-값 데이터는 모두 List&lt;...Entry&gt; 형태로 저장합니다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 저장 당시 씬 이름 (로드할 때 이 씬을 먼저 불러옵니다)
        public string sceneName;

        // 플레이어 위치/회전
        public float[] playerPosition = new float[3];   // x, y, z
        public float playerBodyYaw;                      // 몸통 좌우 회전(Y축)
        public float playerLookPitch;                     // 카메라 상하 회전(X축, 1인칭 시점용)

        // 수집한 단서/증거 ID 목록 (중복 저장 방지를 위해 저장 시 정리됨)
        public List<string> collectedClueIds = new List<string>();

        // 대화/추리 진행 상황 등 범용 On-Off 플래그 저장소
        // 예: "met_zhuge_liang" = true, "found_letter_in_study" = true
        public List<FlagEntry> gameFlags = new List<FlagEntry>();

        // 저장한 시각 (ISO 8601). 세이브 슬롯 목록 UI에 표시할 때 사용.
        public string savedAtIso;

        // 이 세이브 데이터 포맷의 버전. 나중에 저장 구조가 바뀌었을 때
        // 마이그레이션 여부를 판단하기 위한 값입니다.
        public int saveFormatVersion = 1;
    }

    [Serializable]
    public class FlagEntry
    {
        public string key;
        public bool value;

        public FlagEntry() { }

        public FlagEntry(string key, bool value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
