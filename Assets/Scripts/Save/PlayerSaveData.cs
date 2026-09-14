using UnityEngine;

namespace RedCliffMystery.Save
{
    /// <summary>
    /// 플레이어(1인칭 캐릭터)의 위치/회전을 저장·복원하는 컴포넌트.
    /// 플레이어 루트 오브젝트(캐릭터 본체)에 붙여서 사용합니다.
    /// </summary>
    public class PlayerSaveData : MonoBehaviour, ISaveable
    {
        [Header("저장 ID")]
        [Tooltip("세이브 데이터 안에서 플레이어를 구분하는 고유 ID. 플레이어는 보통 1명이므로 기본값 그대로 사용해도 됩니다.")]
        [SerializeField] private string saveId = "player";

        [Header("1인칭 카메라(선택)")]
        [Tooltip("상하 회전(고개 숙이기/들기)을 담당하는 트랜스폼. 보통 플레이어 자식으로 있는 카메라나 카메라 피벗입니다. 비워두면 상하 회전은 저장하지 않습니다.")]
        [SerializeField] private Transform lookPitchTransform;

        public string SaveId => saveId;

        public void CaptureState(SaveData data)
        {
            Vector3 pos = transform.position;
            data.playerPosition[0] = pos.x;
            data.playerPosition[1] = pos.y;
            data.playerPosition[2] = pos.z;

            data.playerBodyYaw = transform.eulerAngles.y;

            if (lookPitchTransform != null)
            {
                data.playerLookPitch = lookPitchTransform.localEulerAngles.x;
            }
        }

        public void RestoreState(SaveData data)
        {
            Vector3 pos = new Vector3(
                data.playerPosition[0],
                data.playerPosition[1],
                data.playerPosition[2]);

            // CharacterController가 붙어있으면 이동시키기 전에 잠깐 꺼야
            // "collision" 경고 없이 순간이동이 정상적으로 적용됩니다.
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                transform.position = pos;
                cc.enabled = true;
            }
            else
            {
                transform.position = pos;
            }

            Vector3 euler = transform.eulerAngles;
            euler.y = data.playerBodyYaw;
            transform.eulerAngles = euler;

            if (lookPitchTransform != null)
            {
                Vector3 lookEuler = lookPitchTransform.localEulerAngles;
                lookEuler.x = data.playerLookPitch;
                lookPitchTransform.localEulerAngles = lookEuler;
            }
        }
    }
}
