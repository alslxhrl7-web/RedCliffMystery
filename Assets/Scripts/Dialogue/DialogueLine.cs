using System;
using UnityEngine;

namespace RedCliffMystery.Dialogue
{
    /// <summary>
    /// 대사 한 줄. speakerName을 비워두면(null 또는 빈 문자열) 플레이어 본인이 말하는 대사로 취급되어
    /// 화면에는 SaveManager에 저장된 주인공 이름이 화자로 표시됩니다.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        public string speakerName;

        [TextArea(2, 4)]
        public string text;

        public DialogueLine() { }

        public DialogueLine(string speakerName, string text)
        {
            this.speakerName = speakerName;
            this.text = text;
        }
    }
}
