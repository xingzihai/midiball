// PlayerController.cs — 玩家运动学控制器（Lerp比例式平滑模型）
// Z轴：dspTime驱动，X轴：输入映射目标速度 + Lerp比例过渡
// 核心：差距大=响应快（方向切换），差距小=精确（微调），松手=自然减速
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
        [Tooltip("速度跟随率：越大响应越快，15~25为推荐范围")]
        [SerializeField] private float followRate = 20f;
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
        private float _currentVelocityX; // 当前实际横向速度
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
            float targetVelocity = ComputeTargetVelocity();
            // Lerp比例式过渡：差距大变化快，差距小变化慢
            // 方向切换时差距=70(35到-35)，变化量=70*20*dt≈23/帧(60fps)，约3帧完成
            // 松手时差距=35(35到0)，变化量=35*20*dt≈11.7/帧，约3帧完成
            // 微调时差距小，变化量小，天然精确
            _currentVelocityX = Mathf.Lerp(_currentVelocityX, targetVelocity, followRate * dt);
            // 接近零时直接归零，避免无限趋近的微小抖动
            if (Mathf.Abs(targetVelocity) < 0.01f && Mathf.Abs(_currentVelocityX) < 0.5f)
                _currentVelocityX = 0f;
            // 外部冲量独立衰减
            _impulseVelocity = Mathf.MoveTowards(_impulseVelocity, 0f, impulseDecay * dt);
            // 合成位移
            _posX += (_currentVelocityX + _impulseVelocity) * dt;
            float hw = GameConstants.TRACK_HALF_WIDTH;
            _posX = Mathf.Clamp(_posX, -hw, hw);

            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            transform.position = new Vector3(_posX, _fixedY, z);
            CheckManualCollisions();
        }

        /// <summary>
        /// 目标速度：死区过滤 + 响应曲线
        /// 键盘：-1/0/1直接映射满速，Lerp负责平滑过渡
        /// 摇杆：连续值经曲线后精确控制
        /// </summary>
        private float ComputeTargetVelocity()
        {
            float raw = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(raw)< deadZone) return 0f;
            float sign = Mathf.Sign(raw);
            float mag = (Mathf.Abs(raw) - deadZone) / (1f - deadZone);
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

        /// <summary>手动碰撞检测</summary>
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

        public void ApplyLateralImpulse(float impulse) { _impulseVelocity += impulse; }
        public float VelocityX => _currentVelocityX + _impulseVelocity;}
}
