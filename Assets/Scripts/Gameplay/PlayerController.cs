// PlayerController.cs — 玩家运动学控制器
// Z轴：dspTime驱动（零漂移），X轴：输入驱动 + 边界反弹
// 附带SphereCollider+KinematicRigidbody用于与发声器挡板碰撞
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Gameplay
{
    public class PlayerController : MonoBehaviour
    {
        [Header("横向物理参数")]
        [SerializeField] private float lateralAccel = 150f;
        [SerializeField] private float maxLateralSpeed = 50f;
        [SerializeField] private float damping = 0.88f;

        private IAudioConductor _conductor;
        private float _velocityX;
        private float _posX;
        private bool _initialized;

        void Start()
        {
            EnsureCollisionComponents();
            TryInit();
        }

        void Update()
        {
            if (!_initialized) TryInit();
            if (!_initialized || _conductor == null) return;
            UpdateLateralMovement();
            UpdatePosition();
        }

        /// <summary>确保Player有碰撞所需的组件</summary>
        private void EnsureCollisionComponents()
        {
            // SphereCollider（非Trigger，用于与Trigger挡板产生OnTriggerEnter）
            if (GetComponent<SphereCollider>() == null)
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 0.4f;
                sc.isTrigger = false;
            }
            // Kinematic Rigidbody（位置由代码驱动，但需要它才能触发OnTrigger回调）
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            // 确保Tag为Player
            gameObject.tag = "Player";
        }

        private void TryInit()
        {
            if (ServiceLocator.Has<IAudioConductor>())
            {
                _conductor = ServiceLocator.Get<IAudioConductor>();
                _posX = transform.position.x;
                _initialized = true;
            }
        }

        /// <summary>X轴：输入加速 + 阻尼 + 边界反弹</summary>
        private void UpdateLateralMovement()
        {
            float input = Input.GetAxisRaw("Horizontal");
            float dt = Time.deltaTime;

            if (Mathf.Abs(input) > 0.01f)
                _velocityX += input * lateralAccel * dt;
            else
                _velocityX *= damping;

            _velocityX = Mathf.Clamp(_velocityX, -maxLateralSpeed, maxLateralSpeed);
            _posX += _velocityX * dt;

            // 边界反弹（完全弹性）
            float hw = GameConstants.TRACK_HALF_WIDTH;
            if (_posX > hw)
            {
                _posX = hw - (_posX - hw);
                _velocityX = -_velocityX;
                _posX = Mathf.Clamp(_posX, -hw, hw);
            }
            else if (_posX < -hw)
            {
                _posX = -hw - (_posX + hw);
                _velocityX = -_velocityX;
                _posX = Mathf.Clamp(_posX, -hw, hw);
            }
        }

        /// <summary>组合Z(时间驱动)和X(输入驱动)</summary>
        private void UpdatePosition()
        {
            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            transform.position = new Vector3(_posX, transform.position.y, z);
        }

        /// <summary>施加横向冲量（被挡板弹射时调用）</summary>
        public void ApplyLateralImpulse(float impulse)
        {
            _velocityX += impulse;
        }

        public float VelocityX => _velocityX;
    }
}
