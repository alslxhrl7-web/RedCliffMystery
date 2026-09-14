using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Save;

namespace RedCliffMystery.Ending
{
    /// <summary>
    /// 어떤 엔딩을 보여줄지 판정하고, 엔딩 씬으로 전환해주는 매니저.
    ///
    /// 사용 방법
    /// 1) 최종 지목(심문/추리 판정) 시스템이 결과가 나오는 시점에
    ///    SaveManager.Instance.SetFlag(...)로 관련 플래그를 설정합니다.
    ///    (예: SetFlag("final_accusation_correct", true))
    /// 2) 그 직후 EndingManager.Instance.TriggerEnding()을 호출하면
    ///    조건에 맞는 엔딩을 찾아 엔딩 씬으로 전환합니다.
    ///
    /// SaveManager와 마찬가지로 씬이 바뀌어도 살아남는 싱글턴입니다.
    /// (엔딩 씬으로 넘어간 뒤에도 "어떤 엔딩이 선택됐는지"를 들고 있어야 하기 때문)
    /// </summary>
    public class EndingManager : MonoBehaviour
    {
        public static EndingManager Instance { get; private set; }

        [Header("엔딩 목록 (우선순위 순서)")]
        [Tooltip("위에서부터 순서대로 조건을 검사해서 맨 처음으로 조건을 만족하는 엔딩이 선택됩니다.\n" +
                 "그래서 조건이 까다로운 '트루 엔딩'을 위쪽에, 조건이 없는(항상 성립하는) '배드 엔딩'을 맨 아래에 둡니다.")]
        [SerializeField] private List<EndingDefinition> endingsInPriorityOrder = new List<EndingDefinition>();

        [Header("씬 설정")]
        [SerializeField] private string endingSceneName = "Ending";

        [Tooltip("엔딩 화면에서 '엔딩 목록 기록' 파일 이름")]
        [SerializeField] private string unlockedEndingsFileName = "endings_unlocked.json";

        public EndingDefinition CurrentEnding { get; private set; }

        /// <summary>엔딩이 결정된 직후(씬 전환 전)에 호출됩니다.</summary>
        public event Action<EndingDefinition> OnEndingDecided;

        [Serializable]
        private class UnlockedEndingsData
        {
            public List<string> seenEndingIds = new List<string>();
        }

        private UnlockedEndingsData _unlockedData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadUnlockedEndings();
        }

        // ------------------------------------------------------------------
        // 판정
        // ------------------------------------------------------------------

        /// <summary>
        /// 현재 SaveManager 상태를 기준으로 조건에 맞는 엔딩을 찾습니다.
        /// 목록을 위에서부터 순서대로 검사해 처음 조건을 만족하는 엔딩을 반환합니다.
        /// </summary>
        public EndingDefinition Evaluate()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[EndingManager] SaveManager가 씬에 없습니다. SaveManager를 먼저 배치하세요.");
                return null;
            }

            foreach (var ending in endingsInPriorityOrder)
            {
                if (ending == null) continue;

                if (ending.AreConditionsMet(SaveManager.Instance))
                {
                    return ending;
                }
            }

            Debug.LogWarning("[EndingManager] 조건에 맞는 엔딩을 찾지 못했습니다. " +
                              "목록 맨 아래에 조건 없는 기본(배드) 엔딩을 하나 추가해두는 것을 권장합니다.");
            return null;
        }

        /// <summary>
        /// 엔딩을 판정하고, 그 결과로 엔딩 씬을 불러옵니다.
        /// </summary>
        public void TriggerEnding()
        {
            EndingDefinition ending = Evaluate();
            if (ending == null) return;

            CurrentEnding = ending;
            RecordEndingSeen(ending.endingId);

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetFlag("game_completed", true);
                SaveManager.Instance.SetFlag($"ending_seen_{ending.endingId}", true);
            }

            OnEndingDecided?.Invoke(ending);

            SceneManager.LoadScene(endingSceneName);
        }

        // ------------------------------------------------------------------
        // 엔딩 열람 기록 (세이브 슬롯과 별개로, 지금까지 본 엔딩들을 전역으로 기록)
        // 새 게임을 시작해도 이 기록은 남아있어서, 나중에 "엔딩 갤러리" 같은
        // 화면을 만들 때 활용할 수 있습니다.
        // ------------------------------------------------------------------

        private string GetUnlockedEndingsPath()
        {
            return Path.Combine(Application.persistentDataPath, unlockedEndingsFileName);
        }

        private void LoadUnlockedEndings()
        {
            string path = GetUnlockedEndingsPath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _unlockedData = JsonUtility.FromJson<UnlockedEndingsData>(json) ?? new UnlockedEndingsData();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EndingManager] 엔딩 기록 파일 읽기 실패: {e.Message}");
                    _unlockedData = new UnlockedEndingsData();
                }
            }
            else
            {
                _unlockedData = new UnlockedEndingsData();
            }
        }

        private void RecordEndingSeen(string endingId)
        {
            if (string.IsNullOrEmpty(endingId)) return;
            if (!_unlockedData.seenEndingIds.Contains(endingId))
            {
                _unlockedData.seenEndingIds.Add(endingId);
                try
                {
                    string json = JsonUtility.ToJson(_unlockedData, true);
                    File.WriteAllText(GetUnlockedEndingsPath(), json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EndingManager] 엔딩 기록 저장 실패: {e.Message}");
                }
            }
        }

        public bool HasSeenEnding(string endingId) => _unlockedData.seenEndingIds.Contains(endingId);

        public IReadOnlyList<string> GetAllSeenEndingIds() => _unlockedData.seenEndingIds;

        public int TotalEndingCount => endingsInPriorityOrder.Count;
    }
}
