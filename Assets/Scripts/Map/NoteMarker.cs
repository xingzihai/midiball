// NoteMarker.cs — 横向发声器挡板的数据标记 + 碰撞反弹
// 挂载在每个挡板上，检测小球碰撞并触发反弹+判定通知
using UnityEngine;
using StarPipe.Gameplay;

namespace StarPipe.Map
{
    public class NoteMarker : MonoBehaviour
    {
        public int noteIndex;   // 对应SongData.allNotes的索引
        public bool isJudged;   // 是否已被判定
        public bool isActive;   // 是否在激活状态（对象池用）
        public bool isRightSide;// 是否从右墙延伸（用于确定反弹方向）

        [Header("反弹参数")]
        [SerializeField] private float bounceForce = 18f;

        private Renderer _renderer;
        private static readonly Color ColorDefault = new Color(0.5f, 0.5f, 0.6f);
        private static readonly Color ColorHit = new Color(0.2f, 1f, 0.4f);
        private static readonly Color ColorMiss = new Color(1f, 0.2f, 0.2f);

        void Awake() { _renderer = GetComponent<Renderer>(); }

        /// <summary>重置为默认状态（对象池复用时调用）</summary>
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

        /// <summary>碰撞检测：小球触碰挡板时反弹并通知判定系统</summary>
        void OnTriggerEnter(Collider other)
        {
            if (!isActive || isJudged) return;
            if (!other.CompareTag("Player")) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            // 反弹方向：右侧挡板向左弹，左侧挡板向右弹
            float dir = isRightSide ? -1f : 1f;
            player.ApplyLateralImpulse(dir * bounceForce);

            SetHit();

            // 通知判定系统触发Hit音效和事件
            if (NoteJudge.Instance != null)
                NoteJudge.Instance.NotifyHit(noteIndex);

            Debug.Log($"[NoteMarker] 碰撞反弹 #{noteIndex} | dir={dir}");
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

        /// <summary>回收到对象池</summary>
        public void Recycle()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
