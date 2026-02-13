// PlayerController.cs — 玩家运动学控制器
// Z轴：dspTime驱动（零漂移），X轴：输入驱动 + 边界反弹
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Gameplay
{
    public class PlayerController : MonoBehaviour
    {
        [Header("横向物理参数")]
        [SerializeField] private float lateralAccel = 120f;  // 横向加速度（大幅提升）
        [SerializeField] private float maxLateralSpeed = 30f; // 横向最大速度
        [SerializeField] private float damping = 0.92f;       // 无输入时速度衰减

        //内部状态
        private IAudioConductor _conductor;
        private float _velocityX;
        private float _posX;
        private bool _initialized;

        void Start()
        {
            // 延迟获取，确保AudioConductor已注册
            TryInit();
        }

        void Update()
        {
            if (!_initialized) TryInit();
            if (!_initialized || _conductor == null) return;

            UpdateLateralMovement();
            UpdatePosition();
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
            float input = Input.GetAxis("Horizontal"); // A/D 或 左/右箭头
            float dt = Time.deltaTime;

            // 加速
            if (Mathf.Abs(input) > 0.01f)
                _velocityX += input * lateralAccel * dt;
            else
                _velocityX *= damping; // 无输入时衰减

            // 限速
            _velocityX = Mathf.Clamp(_velocityX, -maxLateralSpeed, maxLateralSpeed);

            // 位移
            _posX += _velocityX * dt;

            // 边界反弹（完全弹性）
            float hw = GameConstants.TRACK_HALF_WIDTH;
            if (_posX > hw)
            {
                _posX = hw - (_posX - hw); // 反射回来
                _velocityX = -_velocityX;_posX = Mathf.Clamp(_posX, -hw, hw);
            }
            else if (_posX < -hw)
            {
                _posX = -hw - (_posX + hw);
                _velocityX = -_velocityX;
                _posX = Mathf.Clamp(_posX, -hw, hw);
            }
        }

        /// <summary>组合Z(时间驱动)和X(输入驱动)，设置最终位置</summary>
        private void UpdatePosition()
        {
            // Z轴：严格跟随songTime，不累积，零漂移
            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            transform.position = new Vector3(_posX, transform.position.y, z);
        }

        // --- 公共接口（供后续阶段调用）---

        /// <summary>施加横向冲量（如被音符弹射向中心）</summary>
        public void ApplyLateralImpulse(float impulse)
        {
            _velocityX += impulse;
        }

        /// <summary>当前横向速度（供外部读取）</summary>
        public float VelocityX => _velocityX;
    }
}
