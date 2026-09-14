using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Ending
{
    /// <summary>
    /// 아직 최종 심문/추리 판정 시스템이 없어도 엔딩 판정과 씬 전환을
    /// 미리 테스트해볼 수 있도록 만든 디버그용 컴포넌트.
    ///
    /// F10 : 지금 SaveManager 상태를 기준으로 엔딩을 판정하고 엔딩 씬으로 이동
    /// F11 : 테스트용으로 "final_accusation_correct" 플래그를 true로 설정
    ///       (실제 추리 판정 시스템이 생기면 이 스크립트는 지워도 됩니다)
    /// </summary>
    public class EndingTestTrigger : MonoBehaviour
    {
        [SerializeField] private string testAccusationFlagKey = "final_accusation_correct";

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                if (EndingManager.Instance != null)
                {
                    EndingManager.Instance.TriggerEnding();
                }
                else
                {
                    Debug.LogWarning("[EndingTestTrigger] 씬에 EndingManager가 없습니다.");
                }
            }
            else if (keyboard.f11Key.wasPressedThisFrame)
            {
                if (Save.SaveManager.Instance != null)
                {
                    Save.SaveManager.Instance.SetFlag(testAccusationFlagKey, true);
                    Debug.Log($"[EndingTestTrigger] 테스트용 플래그 설정: {testAccusationFlagKey} = true");
                }
            }
        }
    }
}
