using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// "조조에게 최종 보고를 하러 간다"에 해당하는 월드 오브젝트에 붙이는 트리거.
    /// 평소에는 finalAccusationRoot(=FinalAccusationController가 붙은 UI 루트)를 비활성 상태로 숨겨두고,
    /// 플레이어가 범위 안에서 상호작용 키(E)를 누르면 그 루트를 활성화합니다.
    /// FinalAccusationController.Start()는 그 오브젝트가 "처음 활성화되는 시점"에 실행되므로,
    /// 이 트리거로 활성화하는 순간 오프닝 대사 → 지목 대상 선택 UI가 자동으로 시작됩니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinalAccusationTrigger : MonoBehaviour
    {
        [Tooltip("FinalAccusationController가 붙어 있는 UI 루트 오브젝트. 평소에는 비활성 상태로 둡니다.")]
        [SerializeField] private GameObject finalAccusationRoot;

        [Tooltip("\"E 눌러서 최종 보고\" 같은 안내 UI. 선택 사항 - 비워둬도 동작합니다.")]
        [SerializeField] private GameObject interactionHint;

        private bool _playerInRange;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (!_playerInRange) return;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
            if (finalAccusationRoot != null && finalAccusationRoot.activeSelf) return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (finalAccusationRoot != null) finalAccusationRoot.SetActive(true);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = true;
            if (interactionHint != null) interactionHint.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
            if (interactionHint != null) interactionHint.SetActive(false);
        }
    }
}
