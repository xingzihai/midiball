// NoteMarker.cs — 横向发声器挡板 + 精确碰撞检测
// 使用球心到bounds距离检测替代AABB扩展，消除双重检测竞态
using UnityEngine;
using StarPipe.Gameplay;

namespace StarPipe.Map
{
    public class NoteMarker : MonoBehaviour
    {
        public int noteIndex;
        public bool isJudged;
        public bool isActive;
        public bool isRightSide;

        [Header("反弹参数")]
        [SerializeField] private float bounceForce = 8f;

        private Renderer _renderer;
        private BoxCollider _collider;
        private static readonly Color ColorDefault = new Color(0.5f, 0.5f, 0.6f);
        private static readonly Color ColorHit = new Color(0.2f, 1f, 0.4f);
        private static readonly Color ColorMiss = new Color(1f, 0.2f, 0.2f);

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<BoxCollider>();
        }

        public void Reset(int index, Vector3 pos, bool rightSide)
        {
            noteIndex = index;
            isJudged = false;
            isActive = true;
            isRightSide = rightSide;
            transform.position = pos;
            gameObject.SetActive(true);if (_renderer != null) _renderer.material.color = ColorDefault;
        }

        // 移除OnTriggerEnter，统一用ManualCollisionCheck避免双重触发

        /// <summary>精确碰撞：球心到bounds的距离 < 球半径</summary>
        public bool ManualCollisionCheck(Transform playerTf, float playerRadius)
        {
            if (!isActive || isJudged || _collider == null) return false;Bounds b = _collider.bounds;
            // SqrDistance：点在bounds内返回0，在外返回最近点距离的平方
            float sqrDist = b.SqrDistance(playerTf.position);
            return sqrDist < playerRadius * playerRadius;
        }

        public void DoBounce(PlayerController player)
        {
            if (isJudged) return;
            float dir = isRightSide ? -1f : 1f;
            player.ApplyLateralImpulse(dir * bounceForce);
            SetHit();
            if (NoteJudge.Instance != null) NoteJudge.Instance.NotifyHit(noteIndex);
        }

        public void SetHit()
        {
            isJudged = true;
            if (_renderer != null) _renderer.material.color = ColorHit;
        }

        public void SetMiss()
        {
            isJudged = true;
            if (_renderer != null) _renderer.material.color = ColorMiss;
        }

        public void Recycle()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
