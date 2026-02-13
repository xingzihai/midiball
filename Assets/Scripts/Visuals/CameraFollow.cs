// CameraFollow.cs — 第三人称追尾摄像机
// Z轴直接由dspTime驱动（与玩家同源，消除追踪延迟）
// X轴SmoothDamp平滑跟随，Y轴锁定，旋转固定
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Visuals
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随参数")]
        [SerializeField] private Vector3 offset = new Vector3(0, 4f, -12f);
        [SerializeField] private float xSmoothTime = 0.06f;  // 降低延迟，配合直接速度映射
        [SerializeField] private float lookAheadZ = 5f;       // 视线前方偏移

        private Transform _target;
        private IAudioConductor _conductor;
        private float _xVelocity; // SmoothDamp内部速度缓存
        private bool _initialized;

        void Start() { TryInit(); }

        void LateUpdate()
        {
            if (!_initialized) TryInit();
            if (!_initialized || _target == null || _conductor == null) return;

            // Z轴：直接由dspTime计算，与PlayerController完全同源，零延迟
            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED + offset.z;
            // X轴：SmoothDamp平滑跟随玩家横向位置
            float x = Mathf.SmoothDamp(transform.position.x,
                _target.position.x + offset.x, ref _xVelocity, xSmoothTime);
            // Y轴：固定偏移，不跟随任何变化
            float y = offset.y;

            transform.position = new Vector3(x, y, z);

            // 固定看向前方点（Y锁定为0避免俯仰抖动）
            float lookZ = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED + lookAheadZ;
            transform.LookAt(new Vector3(x, 0f, lookZ));
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            var player = FindObjectOfType<StarPipe.Gameplay.PlayerController>();
            if (player != null) _target = player.transform;
            _initialized = _conductor != null && _target != null;
        }
    }
}
