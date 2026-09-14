using UnityEngine;

namespace RedCliffMystery.Save
{
    /// <summary>
    /// 저장/로드 시스템을 바로 테스트해볼 수 있도록 만든 예시용 "단서/증거" 오브젝트.
    /// 나중에 실제 단서 수집 시스템을 만들 때 이 스크립트를 기반으로 확장하면 됩니다.
    ///
    /// 사용 방법
    /// 1) 단서로 쓸 오브젝트(예: 편지, 단검 등)에 Collider(Is Trigger 체크)를 추가합니다.
    /// 2) 이 스크립트를 붙이고, Clue Id에 이 세상에 하나뿐인 고유 문자열을 입력합니다.
    ///    (예: "clue_dagger_study", "clue_letter_01")
    /// 3) 플레이어 오브젝트의 태그가 "Player"로 되어 있어야 합니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SaveableClue : MonoBehaviour, ISaveable
    {
        [Header("단서 정보")]
        [Tooltip("이 단서를 구분하는 고유 ID. 씬 안에서 절대 겹치면 안 됩니다.")]
        [SerializeField] private string clueId;

        [Tooltip("플레이어가 닿았을 때 오브젝트를 사라지게 할지 여부")]
        [SerializeField] private bool hideOnCollect = true;

        private bool _isCollected;

        public string SaveId => clueId;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;
            if (!other.CompareTag("Player")) return;

            Collect();
        }

        private void Collect()
        {
            _isCollected = true;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkClueCollected(clueId);
            }
            else
            {
                Debug.LogWarning("[SaveableClue] 씬에 SaveManager가 없어 단서 수집 상태를 기록하지 못했습니다.");
            }

            Debug.Log($"[SaveableClue] 단서 획득: {clueId}");

            if (hideOnCollect)
            {
                gameObject.SetActive(false);
            }
        }

        public void CaptureState(SaveData data)
        {
            if (_isCollected && !data.collectedClueIds.Contains(clueId))
            {
                data.collectedClueIds.Add(clueId);
            }
        }

        public void RestoreState(SaveData data)
        {
            if (data.collectedClueIds.Contains(clueId))
            {
                _isCollected = true;
                if (hideOnCollect)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
