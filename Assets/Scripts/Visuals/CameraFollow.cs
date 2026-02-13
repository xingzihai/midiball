// CameraFollow.cs — 第三人称追尾摄像机，平滑跟随玩家
using UnityEngine;

namespace StarPipe.Visuals
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随参数")]
        [SerializeField] private Vector3 offset = new Vector3(0, 4f, -12f);
        [SerializeField] private float smoothSpeed = 8f;     // 平滑跟随速度
        [SerializeField] private float lookAheadZ = 5f;      // 视线前方偏移

        private Transform _target;

        void Start()
        {
            var player = FindObjectOfType<StarPipe.Gameplay.PlayerController>();
            if (player != null) _target = player.transform;
        }

        void LateUpdate()
        {
            if (_target == null) return;
            // 目标位置：玩家位置 + 偏移
            Vector3 desiredPos = _target.position + offset;
            // 平滑插值
            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
            // 看向玩家前方
            Vector3 lookTarget = _target.position + Vector3.forward * lookAheadZ;
            transform.LookAt(lookTarget);
        }
    }
}
