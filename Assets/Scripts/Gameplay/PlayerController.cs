// PlayerController.cs —玩家运动学控制器（直接速度映射模型）
// Z轴：dspTime驱动，X轴：输入直接映射速度 + 响应曲线
// 核心改进：零延迟响应，松手即停，幂次曲线精确控制
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("直接速度映射参数")]
        [SerializeField] private float maxLateralSpeed = 35f;
        [Tooltip("响应曲线幂次：>1小输入精确，大输入快速")]
        [SerializeField] private float responseCurve = 2.0f;
        [Tooltip("摇杆死区，过滤漂移")]
        [SerializeField] private float deadZone = 0.05f;
        [SerializeField] private float playerRadius = 0.4f;

        [Header("外部冲量")]
        [Tooltip("外部冲量（挡板反弹等）的衰减速率")]
        [SerializeField] private float impulseDecay = 8.0f;

        private IAudioConductor _conductor;
        private Rigidbody _rb;
        private MapGenerator _mapGen;
        private float _posX;
        private float _fixedY;
        private float _impulseVelocity; // 外部冲量独立通道
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

            float dt = Time.deltaTime;
            // 输入直接映射为速度（零延迟）
            float inputVelocity = ComputeInputVelocity();
            // 外部冲量独立衰减
            _impulseVelocity = Mathf.MoveTowards(_impulseVelocity, 0f, impulseDecay * dt);
            // 合成最终速度
            float totalVelocity = inputVelocity + _impulseVelocity;
            _posX += totalVelocity * dt;
            // 边界硬夹紧（反弹由挡板系统处理）
            float hw = GameConstants.TRACK_HALF_WIDTH;
            _posX = Mathf.Clamp(_posX, -hw, hw);

            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            transform.position = new Vector3(_posX, _fixedY, z);CheckManualCollisions();
        }

        /// <summary>
        /// 直接速度映射：输入值经死区过滤+幂次曲线后直接映射为速度
        /// 小输入=精确微调，大输入=快速移动，松手=立即停止
        /// </summary>
        private float ComputeInputVelocity()
        {
            float raw = Input.GetAxisRaw("Horizontal");
            // 死区过滤
            if (Mathf.Abs(raw) < deadZone) return 0f;
            // 重映射：死区外的范围归一化到[0,1]
            float sign = Mathf.Sign(raw);
            float magnitude = (Mathf.Abs(raw) - deadZone) / (1f - deadZone);
            // 幂次响应曲线：magnitude^responseCurve 保持[0,1]范围
            float curved = Mathf.Pow(magnitude, responseCurve);
            return sign * curved * maxLateralSpeed;
        }

        private void EnsureCollisionComponents()
        {
            var sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = playerRadius;
            sc.isTrigger = false;
            _rb = GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.None;
            gameObject.tag = "Player";
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            _posX = transform.position.x;
            _fixedY = transform.position.y;
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            _mapGen = Object.FindObjectOfType<MapGenerator>();
            _initialized = true;
        }

        /// <summary>手动AABB碰撞检测，遍历活跃挡板</summary>
        private void CheckManualCollisions()
        {
            if (_mapGen == null) return;
            var markers = _mapGen.ActiveMarkers;
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].ManualCollisionCheck(transform, playerRadius))
                    markers[i].DoBounce(this);
            }
        }

        /// <summary>外部冲量接口（挡板反弹等），独立于输入通道</summary>
        public void ApplyLateralImpulse(float impulse) { _impulseVelocity += impulse; }
        public float VelocityX => ComputeInputVelocity() + _impulseVelocity;
    }
}
