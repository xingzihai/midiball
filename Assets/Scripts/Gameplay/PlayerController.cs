// PlayerController.cs — 玩家运动学控制器（平滑直接映射模型）
// Z轴：dspTime驱动，X轴：输入映射目标速度 + 平滑过渡
// 核心：方向即时响应，速度平滑过渡，松手快速刹停
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("横向控制参数")]
        [SerializeField] private float maxLateralSpeed = 35f;
        [Tooltip("加速平滑率：越大越快到达目标速度")]
        [SerializeField] private float accelSmooth = 120f;
        [Tooltip("刹车平滑率：松手后减速到0的速率，应大于accelSmooth")]
        [SerializeField] private float brakeSmooth = 200f;
        [Tooltip("摇杆死区")]
        [SerializeField] private float deadZone = 0.05f;
        [Tooltip("响应曲线幂次（仅对模拟摇杆有效）")]
        [SerializeField] private float responseCurve = 1.5f;
        [SerializeField] private float playerRadius = 0.4f;

        [Header("外部冲量")]
        [SerializeField] private float impulseDecay = 8.0f;

        private IAudioConductor _conductor;
        private Rigidbody _rb;
        private MapGenerator _mapGen;
        private float _posX;
        private float _fixedY;
        private float _currentVelocityX; // 当前实际横向速度（平滑后）
        private float _impulseVelocity;// 外部冲量独立通道
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
            // 计算目标速度（输入直接映射）
            float targetVelocity = ComputeTargetVelocity();
            // 平滑过渡：加速用accelSmooth，刹车用brakeSmooth（松手快停）
            float smoothRate = Mathf.Abs(targetVelocity) > 0.01f ? accelSmooth : brakeSmooth;
            _currentVelocityX = Mathf.MoveTowards(_currentVelocityX, targetVelocity, smoothRate * dt);
            // 外部冲量独立衰减
            _impulseVelocity = Mathf.MoveTowards(_impulseVelocity, 0f, impulseDecay * dt);
            // 合成位移
            _posX += (_currentVelocityX + _impulseVelocity) * dt;
            // 边界硬夹紧
            float hw = GameConstants.TRACK_HALF_WIDTH;
            _posX = Mathf.Clamp(_posX, -hw, hw);

            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            transform.position = new Vector3(_posX, _fixedY, z);
            CheckManualCollisions();
        }

        /// <summary>
        /// 计算目标速度：死区过滤 + 响应曲线 + 方向映射
        /// 键盘：GetAxisRaw返回-1/0/1，曲线无效果，直接满速
        /// 摇杆：连续值经曲线后精确控制
        /// </summary>
        private float ComputeTargetVelocity()
        {
            float raw = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(raw) < deadZone) return 0f;
            float sign = Mathf.Sign(raw);
            float mag = (Mathf.Abs(raw) - deadZone) / (1f - deadZone);
            //幂次曲线（对摇杆有效，键盘mag≈1所以无影响）
            float curved = Mathf.Pow(mag, responseCurve);
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

        /// <summary>手动碰撞检测，遍历活跃挡板</summary>
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

        /// <summary>外部冲量接口（挡板反弹等）</summary>
        public void ApplyLateralImpulse(float impulse) { _impulseVelocity += impulse; }
        public float VelocityX => _currentVelocityX + _impulseVelocity;}
}
