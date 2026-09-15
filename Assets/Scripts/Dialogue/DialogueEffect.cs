using System;
using RedCliffMystery.Save;

namespace RedCliffMystery.Dialogue
{
    public enum DialogueEffectType
    {
        None,
        SetFlagTrue,
        SetFlagFalse,
        MarkClueCollected,
    }

    /// <summary>
    /// 화제/증거 제시 결과로 적용되는 효과 하나. SaveManager에만 반영되므로
    /// 저장/로드 시 그대로 유지되고, 엔딩 판정(EndingCondition)에서도 바로 참조할 수 있습니다.
    /// </summary>
    [Serializable]
    public class DialogueEffect
    {
        public DialogueEffectType type = DialogueEffectType.None;

        [UnityEngine.Tooltip("SetFlagTrue/SetFlagFalse일 때는 플래그 키, MarkClueCollected일 때는 clueId")]
        public string key;

        public void Apply()
        {
            if (SaveManager.Instance == null || type == DialogueEffectType.None || string.IsNullOrEmpty(key))
                return;

            switch (type)
            {
                case DialogueEffectType.SetFlagTrue:
                    SaveManager.Instance.SetFlag(key, true);
                    break;
                case DialogueEffectType.SetFlagFalse:
                    SaveManager.Instance.SetFlag(key, false);
                    break;
                case DialogueEffectType.MarkClueCollected:
                    SaveManager.Instance.MarkClueCollected(key);
                    break;
            }
        }
    }
}
