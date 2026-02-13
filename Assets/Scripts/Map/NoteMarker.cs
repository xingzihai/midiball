// NoteMarker.cs — 横向发声器挡板 +碰撞反弹（双重检测）
// OnTriggerEnter +手动AABB检测，防止高速穿透
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
        [SerializeField] private float bounceForce = 8f; // 削弱：18→8

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
            gameObject.SetActive(true);
            if (_renderer != null) _renderer.material.color = ColorDefault;
        }

        /// <summary>物理Trigger回调</summary>
        void OnTriggerEnter(Collider other)
        {
            if (!isActive || isJudged) return;
            if (!other.CompareTag("Player")) return;
            var player = other.GetComponent<PlayerController>();
            if (player != null) DoBounce(player);
        }

        /// <summary>手动AABB碰撞检测（防止高速穿透）</summary>
        public bool ManualCollisionCheck(Transform playerTf, float playerRadius)
        {
            if (!isActive || isJudged || _collider == null) return false;
            Bounds b = _collider.bounds;
            b.Expand(playerRadius * 2f);
            return b.Contains(playerTf.position);
        }

        public void DoBounce(PlayerController player)
        {
            if (isJudged) return;
            float dir = isRightSide ? -1f : 1f;
            player.ApplyLateralImpulse(dir * bounceForce);
            SetHit();
            if (NoteJudge.Instance != null) NoteJudge.Instance.NotifyHit(noteIndex);Debug.Log($"[NoteMarker] 碰撞反弹 #{noteIndex} | dir={dir}");
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
