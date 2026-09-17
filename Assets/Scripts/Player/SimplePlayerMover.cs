using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.PlayerControl
{
    /// <summary>
    /// 아직 실제 캐릭터/맵이 없는 상태에서 시스템(대화/최종 지목/엔딩)을 바로 플레이 테스트할 수 있도록
    /// 만든 아주 단순한 임시 이동 스크립트입니다. WASD(또는 방향키)로 앞뒤 이동 + 좌우 회전만 지원합니다.
    ///
    /// 실제 3D 캐릭터/맵 애셋이 준비되면 이 스크립트는 지우고 정식 캐릭터 컨트롤러로 교체하면 됩니다.
    /// CharacterController를 사용하므로 Rigidbody 없이도 NPC/최종 지목 트리거의 트리거 콜라이더에
    /// OnTriggerEnter/Exit 이벤트가 정상적으로 발생합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerMover : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _controller == null) return;

            float turn = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) turn -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) turn += 1f;
            transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

            float move = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move -= 1f;

            Vector3 motion = transform.forward * (move * moveSpeed);

            if (_controller.isGrounded)
            {
                _verticalVelocity = -0.5f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }
            motion.y = _verticalVelocity;

            _controller.Move(motion * Time.deltaTime);
        }
    }
}
