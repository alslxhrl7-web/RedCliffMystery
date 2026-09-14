using System;
using RedCliffMystery.Save;

namespace RedCliffMystery.Ending
{
    /// <summary>
    /// 엔딩 하나가 성립하기 위한 조건 하나.
    /// EndingDefinition은 이 조건들을 여러 개 가질 수 있고, 그 전부가 참이어야
    /// 해당 엔딩이 선택됩니다 (AND 조건).
    ///
    /// SaveManager에 이미 있는 "플래그"와 "수집한 단서 목록"만 보고 판정하기 때문에,
    /// 나중에 만들 대화/심문/최종 지목 시스템은 이 조건들을 직접 알 필요 없이
    /// SaveManager.SetFlag(...) 나 MarkClueCollected(...)만 호출하면 됩니다.
    /// </summary>
    [Serializable]
    public class EndingCondition
    {
        public enum ConditionType
        {
            FlagIsTrue,      // key로 지정한 플래그가 true여야 함
            FlagIsFalse,     // key로 지정한 플래그가 false(또는 미설정)여야 함
            ClueCollected,   // key로 지정한 clueId를 수집했어야 함
            MinClueCount,    // 전체 수집한 단서 개수가 intValue 이상이어야 함
        }

        public ConditionType type = ConditionType.FlagIsTrue;

        [UnityEngine.Tooltip("FlagIsTrue / FlagIsFalse / ClueCollected 일 때 사용하는 키 값")]
        public string key;

        [UnityEngine.Tooltip("MinClueCount 일 때 사용하는 최소 개수")]
        public int intValue;

        public bool IsMet(SaveManager saveManager)
        {
            if (saveManager == null) return false;

            switch (type)
            {
                case ConditionType.FlagIsTrue:
                    return saveManager.GetFlag(key, false) == true;

                case ConditionType.FlagIsFalse:
                    return saveManager.GetFlag(key, false) == false;

                case ConditionType.ClueCollected:
                    return saveManager.IsClueCollected(key);

                case ConditionType.MinClueCount:
                    return saveManager.CollectedClueCount >= intValue;

                default:
                    return false;
            }
        }
    }
}
