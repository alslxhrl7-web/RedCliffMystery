using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// NPC 오브젝트에 붙여서 사용합니다.
    /// 1) 이 오브젝트에 Collider를 하나 추가하고 Is Trigger를 켜세요 (Reset 시 자동으로 켜줍니다).
    /// 2) 플레이어 오브젝트에는 태그 "Player"를 지정하세요.
    /// 3) 범위 안에서 상호작용 키(기본 E)를 누르면 대화가 시작됩니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NPCInteractable : MonoBehaviour
    {
        [SerializeField] private CharacterDialogueData dialogueData;
        [Tooltip("\"E 눌러서 대화\" 같은 안내 UI. 선택 사항 - 비워둬도 동작합니다.")]
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
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive) return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                DialogueManager.Instance.StartDialogue(dialogueData);
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
