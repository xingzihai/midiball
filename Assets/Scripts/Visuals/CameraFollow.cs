// CameraFollow.cs — 第三人称追尾摄像机
// Z轴由dspTime驱动，X轴直接跟随玩家（零延迟），Y轴锁定
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Visuals
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随参数")]
        [SerializeField] private Vector3 offset = new Vector3(0, 4f, -12f);
        [SerializeField] private float lookAheadZ = 5f;

        private Transform _target;
        private IAudioConductor _conductor;
        private bool _initialized;

        void Start() { TryInit(); }

        void LateUpdate()
        {
            if (!_initialized) TryInit();
            if (!_initialized || _target == null || _conductor == null) return;

            float z = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED + offset.z;
            // X轴直接跟随，零延迟零SmoothDamp
            float x = _target.position.x + offset.x;
            float y = offset.y;

            transform.position = new Vector3(x, y, z);

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
