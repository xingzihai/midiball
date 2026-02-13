// PlayerController.cs — 玩家运动学控制器（直接映射+独立冲量）
// Z轴：dspTime驱动，X轴：Input.GetAxis直接映射速度
// 冲量独立衰减，不被输入清零；管道壁硬夹紧不反弹
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
        [SerializeField] private float maxLateralSpeed = 50f;
        [SerializeField] private float playerRadius = 0.4f;

        [Header("外部冲量（发声器反弹等）")]
        [SerializeField] private float impulseDecay = 25f;
        [SerializeField] private float maxImpulse = 20f; // 冲量上限防止叠加爆炸

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
            float inputVelocity = Input.GetAxis("Horizontal") * maxLateralSpeed;
            // 冲量独立衰减，始终与输入叠加（不清零）
            _impulseVelocity = Mathf.MoveTowards(_impulseVelocity, 0f, impulseDecay * dt);
            _posX += (inputVelocity + _impulseVelocity) * dt;
            // 管道壁：硬夹紧 + 清零冲量（不反弹，避免卡墙抖动）
            float hw = GameConstants.TRACK_HALF_WIDTH;
            if (_posX > hw || _posX < -hw)
            {
                _posX = Mathf.Clamp(_posX, -hw, hw);
                _impulseVelocity = 0f; // 撞墙吃掉冲量
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

        /// <summary>外部冲量，带上限防叠加</summary>
        public void ApplyLateralImpulse(float impulse)
        {
            _impulseVelocity = Mathf.Clamp(_impulseVelocity + impulse, -maxImpulse, maxImpulse);
        }public float VelocityX => Input.GetAxis("Horizontal") * maxLateralSpeed + _impulseVelocity;}
}
