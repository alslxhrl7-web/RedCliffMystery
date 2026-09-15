using System;
using RedCliffMystery.Save;

namespace RedCliffMystery.Dialogue
{
    public enum DialogueConditionType
    {
        None,
        FlagTrue,
        FlagFalse,
        ClueCollected,
        ClueNotCollected,
    }

    /// <summary>
    /// 대화 화제/증거 노출 조건 하나. 기존 EndingCondition과 같은 방식으로
    /// SaveManager의 플래그/단서 수집 여부만 보고 판정합니다.
    /// </summary>
    [Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType type = DialogueConditionType.None;

        [UnityEngine.Tooltip("FlagTrue/FlagFalse일 때는 플래그 키, ClueCollected/ClueNotCollected일 때는 clueId")]
        public string key;

        public bool Evaluate()
        {
            if (type == DialogueConditionType.None) return true;
            if (SaveManager.Instance == null) return false;

            switch (type)
            {
                case DialogueConditionType.FlagTrue:
                    return SaveManager.Instance.GetFlag(key, false);
                case DialogueConditionType.FlagFalse:
                    return !SaveManager.Instance.GetFlag(key, false);
                case DialogueConditionType.ClueCollected:
                    return SaveManager.Instance.IsClueCollected(key);
                case DialogueConditionType.ClueNotCollected:
                    return !SaveManager.Instance.IsClueCollected(key);
                default:
                    return true;
            }
        }
    }
}
