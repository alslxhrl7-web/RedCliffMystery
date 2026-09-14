namespace RedCliffMystery.Save
{
    /// <summary>
    /// 저장/로드 대상이 되는 모든 오브젝트가 구현하는 인터페이스.
    /// SaveManager는 씬 안에서 이 인터페이스를 구현한 컴포넌트를 모두 찾아서
    /// 저장할 때는 CaptureState를, 불러올 때는 RestoreState를 호출합니다.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// 저장 데이터 안에서 이 오브젝트를 구분하는 고유 ID.
        /// 씬을 새로 로드해도 같은 값이어야 하므로, 보통 인스펙터에서
        /// 직접 지정하는 문자열을 사용합니다 (예: "player", "clue_dagger_01").
        /// </summary>
        string SaveId { get; }

        /// <summary>
        /// 현재 이 오브젝트의 상태를 data에 기록합니다. (저장 시 호출)
        /// </summary>
        void CaptureState(SaveData data);

        /// <summary>
        /// data에 들어있는 값으로 이 오브젝트의 상태를 복원합니다. (로드 시 호출)
        /// </summary>
        void RestoreState(SaveData data);
    }
}
