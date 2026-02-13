// PlayerController.cs —玩家运动学控制器（零延迟直接映射）
// Z轴：dspTime驱动，X轴：GetAxisRaw直接映射速度，零平滑零延迟
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("横向控制")]
        [SerializeField] private float maxLateralSpeed = 70f;
        [SerializeField] private float playerRadius = 0.4f;

        [Header("外部冲量")]
        [SerializeField] private float impulseDecay = 25f;
        [SerializeField] private float maxImpulse = 20f;

        private IAudioConductor _conductor;
        private Rigidbody _rb;
        private MapGenerator _mapGen;
        private float _posX;
        private float _fixedY;
        private float _impulseVelocity;
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
            // GetAxisRaw：键盘直接返回-1/0/1，零延迟，不依赖InputManager平滑
            float inputVelocity = Input.GetAxisRaw("Horizontal") * maxLateralSpeed;
            _impulseVelocity = Mathf.MoveTowards(_impulseVelocity, 0f, impulseDecay * dt);
            _posX += (inputVelocity + _impulseVelocity) * dt;
            float hw = GameConstants.TRACK_HALF_WIDTH;
            if (_posX > hw || _posX < -hw)
            {
                _posX = Mathf.Clamp(_posX, -hw, hw);
                _impulseVelocity = 0f;
            }

            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            transform.position = new Vector3(_posX, _fixedY, z);
            CheckManualCollisions();
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

        public void ApplyLateralImpulse(float impulse)
        {
            _impulseVelocity = Mathf.Clamp(_impulseVelocity + impulse, -maxImpulse, maxImpulse);
        }public float VelocityX => Input.GetAxisRaw("Horizontal") * maxLateralSpeed + _impulseVelocity;
    }
}
