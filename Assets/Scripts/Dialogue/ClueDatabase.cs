using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Dialogue
{
    [Serializable]
    public class ClueInfo
    {
        public string clueId;
        public string displayName;
    }

    /// <summary>
    /// 증거 제시 UI에서 clueId 대신 사람이 읽을 수 있는 이름을 보여주기 위한 참조용 데이터.
    /// 사건기획서 v1의 9절(전체 단서 목록)과 짝이 맞아야 합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ClueDatabase", menuName = "적벽추리/Clue Database", order = 2)]
    public class ClueDatabase : ScriptableObject
    {
        public List<ClueInfo> clues = new List<ClueInfo>();

        private Dictionary<string, string> _lookup;

        public string GetDisplayName(string clueId)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, string>();
                foreach (var c in clues)
                {
                    if (!string.IsNullOrEmpty(c.clueId) && !_lookup.ContainsKey(c.clueId))
                        _lookup.Add(c.clueId, c.displayName);
                }
            }

            return _lookup.TryGetValue(clueId, out var name) ? name : clueId;
        }
    }
}
