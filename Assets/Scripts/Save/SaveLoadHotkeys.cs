using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Save
{
    /// <summary>
    /// 실제 저장/불러오기 UI를 만들기 전에, 키보드 단축키로 저장 시스템을
    /// 빠르게 테스트해보기 위한 디버그용 컴포넌트.
    /// 아무 오브젝트에나 붙여서 사용하세요 (SaveManager와 같은 오브젝트여도 됩니다).
    ///
    /// F5 : 1번 슬롯에 저장
    /// F9 : 1번 슬롯에서 불러오기
    /// F6 : 1번 슬롯 삭제 + 진행 상황 초기화 (새 게임 테스트용)
    ///
    /// 이 프로젝트는 com.unity.inputsystem 패키지를 사용하므로 새 Input System API로 작성했습니다.
    /// Project Settings > Player > Active Input Handling이 "Input System Package (New)" 또는
    /// "Both"로 되어 있어야 정상 동작합니다.
    /// </summary>
    public class SaveLoadHotkeys : MonoBehaviour
    {
        [SerializeField] private int testSlot = 1;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || SaveManager.Instance == null) return;

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                SaveManager.Instance.SaveGame(testSlot);
            }
            else if (keyboard.f9Key.wasPressedThisFrame)
            {
                if (SaveManager.Instance.HasSave(testSlot))
                {
                    SaveManager.Instance.LoadGame(testSlot);
                }
                else
                {
                    Debug.LogWarning($"[SaveLoadHotkeys] 슬롯 {testSlot}에 저장된 데이터가 없습니다. 먼저 F5로 저장해보세요.");
                }
            }
            else if (keyboard.f6Key.wasPressedThisFrame)
            {
                SaveManager.Instance.DeleteSave(testSlot);
                SaveManager.Instance.ResetProgress();
                Debug.Log($"[SaveLoadHotkeys] 슬롯 {testSlot} 삭제 및 진행 상황 초기화 완료.");
            }
        }
    }
}
