using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.Save
{
    /// <summary>
    /// 게임 전체의 저장/로드를 담당하는 싱글턴 매니저.
    ///
    /// 사용 방법
    /// 1) 처음 시작하는 씬(예: 타이틀/메인 씬)에 빈 GameObject를 하나 만들고
    ///    이름을 "SaveManager"로 지은 뒤 이 스크립트를 붙입니다.
    /// 2) 씬에서 저장 대상이 되는 오브젝트(플레이어, 단서 등)에는
    ///    ISaveable을 구현한 컴포넌트(PlayerSaveData, SaveableClue 등)를 붙입니다.
    /// 3) 코드 어디서든 SaveManager.Instance.SaveGame(slot);
    ///    SaveManager.Instance.LoadGame(slot); 을 호출하면 됩니다.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("설정")]
        [Tooltip("세이브 파일이 저장될 하위 폴더 이름")]
        [SerializeField] private string saveFolderName = "Saves";

        [Tooltip("저장 파일 이름 접두사. 실제 파일명은 {prefix}{slot}.json")]
        [SerializeField] private string saveFilePrefix = "save_slot_";

        // 현재 메모리에 올라와 있는 세이브 데이터 (아직 파일로 안 쓴 상태 포함)
        private SaveData _currentData;

        // 씬 로드가 끝난 뒤 상태 복원을 이어서 하기 위해 잠깐 들고 있는 데이터
        private SaveData _pendingLoadData;

        public event Action<int> OnSaveCompleted;   // 저장 완료 시 (슬롯 번호 전달)
        public event Action<int> OnLoadStarted;     // 로드 시작 시 (슬롯 번호 전달)
        public event Action<int> OnLoadCompleted;   // 로드(씬 전환 + 상태 복원)까지 끝난 시점

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 씬을 다시 로드하는 과정에서 SaveManager가 중복 생성되는 것을 방지
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _currentData = new SaveData();
        }

        // ------------------------------------------------------------------
        // 파일 경로 관련
        // ------------------------------------------------------------------

        private string GetSaveDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, saveFolderName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        private string GetSaveFilePath(int slot)
        {
            return Path.Combine(GetSaveDirectory(), $"{saveFilePrefix}{slot}.json");
        }

        public bool HasSave(int slot)
        {
            return File.Exists(GetSaveFilePath(slot));
        }

        public void DeleteSave(int slot)
        {
            string path = GetSaveFilePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 세이브 슬롯 UI에 표시할 간단한 정보(저장 시각)만 미리 읽어옵니다.
        /// 파일이 없으면 null을 반환합니다.
        /// </summary>
        public string PeekSavedAt(int slot)
        {
            string path = GetSaveFilePath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data?.savedAtIso;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 세이브 슬롯 {slot} 미리보기 실패: {e.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------------
        // 저장
        // ------------------------------------------------------------------

        /// <summary>
        /// 현재 씬의 모든 ISaveable 오브젝트 상태를 모아 지정한 슬롯에 저장합니다.
        /// </summary>
        public void SaveGame(int slot)
        {
            SaveData data = new SaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                savedAtIso = DateTime.UtcNow.ToString("o")
            };

            // 기존에 들고 있던 플래그/단서 목록/플레이어 이름을 이어받고, 그 위에 최신 상태를 덮어씁니다.
            data.gameFlags = new List<FlagEntry>(_currentData.gameFlags);
            data.collectedClueIds = new List<string>(_currentData.collectedClueIds);
            data.playerName = _currentData.playerName;

            CollectStateFromScene(data);

            _currentData = data;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(GetSaveFilePath(slot), json);
                Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료: {GetSaveFilePath(slot)}");
                OnSaveCompleted?.Invoke(slot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 실패(슬롯 {slot}): {e}");
            }
        }

        private void CollectStateFromScene(SaveData data)
        {
            // 활성/비활성 오브젝트를 모두 포함해 ISaveable을 구현한 모든 컴포넌트를 찾습니다.
            // (Unity 6부터 FindObjectsSortMode를 받는 오버로드는 사용 중단되어, 정렬 없는 오버로드를 사용합니다)
            var saveables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            foreach (var mb in saveables)
            {
                if (mb is ISaveable saveable)
                {
                    saveable.CaptureState(data);
                }
            }
        }

        // ------------------------------------------------------------------
        // 로드
        // ------------------------------------------------------------------

        /// <summary>
        /// 지정한 슬롯의 저장 파일을 읽어 씬을 전환하고 상태를 복원합니다.
        /// 저장 당시 씬과 현재 씬이 같으면 씬 전환 없이 바로 상태만 복원합니다.
        /// </summary>
        public void LoadGame(int slot)
        {
            string path = GetSaveFilePath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] 슬롯 {slot}에 저장된 파일이 없습니다.");
                return;
            }

            SaveData data;
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slot} 로드 실패(파일 손상 가능): {e}");
                return;
            }

            if (data == null)
            {
                Debug.LogError($"[SaveManager] 슬롯 {slot}의 데이터를 해석할 수 없습니다.");
                return;
            }

            OnLoadStarted?.Invoke(slot);
            _currentData = data;

            string activeScene = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(data.sceneName) && data.sceneName != activeScene)
            {
                // 저장된 씬이 지금 씬과 다르면 그 씬을 먼저 불러온 뒤, 로드가 끝나면
                // OnSceneLoaded에서 상태를 복원합니다.
                _pendingLoadData = data;
                SceneManager.sceneLoaded += OnSceneLoadedForRestore;
                SceneManager.LoadScene(data.sceneName);
            }
            else
            {
                ApplyStateToScene(data);
                OnLoadCompleted?.Invoke(slot);
            }
        }

        private void OnSceneLoadedForRestore(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForRestore;

            if (_pendingLoadData != null)
            {
                ApplyStateToScene(_pendingLoadData);
                _pendingLoadData = null;
                OnLoadCompleted?.Invoke(-1);
            }
        }

        private void ApplyStateToScene(SaveData data)
        {
            var saveables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            foreach (var mb in saveables)
            {
                if (mb is ISaveable saveable)
                {
                    saveable.RestoreState(data);
                }
            }
        }

        // ------------------------------------------------------------------
        // 게임 플래그 (대화 진행, 사건 단서 확인 여부 등 범용 On/Off 값)
        // ------------------------------------------------------------------

        public void SetFlag(string key, bool value)
        {
            var list = _currentData.gameFlags;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].key == key)
                {
                    list[i].value = value;
                    return;
                }
            }
            list.Add(new FlagEntry(key, value));
        }

        public bool GetFlag(string key, bool defaultValue = false)
        {
            var list = _currentData.gameFlags;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].key == key)
                {
                    return list[i].value;
                }
            }
            return defaultValue;
        }

        // ------------------------------------------------------------------
        // 플레이어 이름 (새 게임 시작 시 이름 입력 화면에서 설정)
        // ------------------------------------------------------------------

        /// <summary>새 게임을 시작할 때 지정할 기본(입력을 비워둔 경우) 이름.</summary>
        public const string DefaultPlayerName = "이름 없는 참모";

        public void SetPlayerName(string name)
        {
            _currentData.playerName = string.IsNullOrWhiteSpace(name) ? DefaultPlayerName : name.Trim();
        }

        public string GetPlayerName()
        {
            return string.IsNullOrEmpty(_currentData.playerName) ? DefaultPlayerName : _currentData.playerName;
        }

        // ------------------------------------------------------------------
        // 단서/증거 수집 여부 (SaveableClue 등에서 사용)
        // ------------------------------------------------------------------

        public void MarkClueCollected(string clueId)
        {
            if (!_currentData.collectedClueIds.Contains(clueId))
            {
                _currentData.collectedClueIds.Add(clueId);
            }
        }

        public bool IsClueCollected(string clueId)
        {
            return _currentData.collectedClueIds.Contains(clueId);
        }

        /// <summary>
        /// 지금까지 수집한 단서 개수. 엔딩 판정(EndingCondition.MinClueCount) 등에서 사용합니다.
        /// </summary>
        public int CollectedClueCount => _currentData.collectedClueIds.Count;

        /// <summary>
        /// 새 게임 시작 시 메모리 상의 진행 데이터를 초기화합니다.
        /// (디스크에 저장된 파일에는 영향을 주지 않습니다. 지우려면 DeleteSave 사용)
        /// </summary>
        public void ResetProgress()
        {
            _currentData = new SaveData();
        }
    }
}
