// PlayerController.cs — 玩家运动学控制器
// Z轴：dspTime驱动，X轴：输入驱动 + 边界反弹
// Rigidbody.MovePosition + 手动AABB碰撞检测（双保险）
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("横向物理参数")]
        [SerializeField] private float lateralAccel = 150f;
        [SerializeField] private float maxLateralSpeed = 50f;
        [SerializeField] private float damping = 0.88f;
        [SerializeField] private float playerRadius = 0.4f;

        private IAudioConductor _conductor;
        private Rigidbody _rb;
        private MapGenerator _mapGen;
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
            // 手动碰撞检测（防止高速穿透Trigger失效）
            CheckManualCollisions();
        }

        void FixedUpdate()
        {
            if (!_initialized || _conductor == null || _rb == null) return;
            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            _rb.MovePosition(new Vector3(_posX, transform.position.y, z));
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
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            gameObject.tag = "Player";
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            _posX = transform.position.x;
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

        public void ApplyLateralImpulse(float impulse) { _velocityX += impulse; }
        public float VelocityX => _velocityX;
    }
}
