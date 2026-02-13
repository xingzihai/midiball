// NoteMarker.cs — 发声器方块的数据标记组件
// 挂载在每个音符方块上，存储索引和判定状态
using UnityEngine;

namespace StarPipe.Map
{
    public class NoteMarker : MonoBehaviour
    {
        public int noteIndex;// 对应SongData.allNotes的索引
        public bool isJudged;    // 是否已被判定
        public bool isActive;    // 是否在激活状态（对象池用）

        private Renderer _renderer;
        private static readonly Color ColorDefault = new Color(0.5f, 0.5f, 0.6f); // 灰蓝
        private static readonly Color ColorHit = new Color(0.2f, 1f, 0.4f);// 亮绿
        private static readonly Color ColorMiss = new Color(1f, 0.2f, 0.2f);// 红

        void Awake() { _renderer = GetComponent<Renderer>(); }

        /// <summary>重置为默认状态（对象池复用时调用）</summary>
        public void Reset(int index, Vector3 pos)
        {
            noteIndex = index;
            isJudged = false;
            isActive = true;
            transform.position = pos;
            gameObject.SetActive(true);if (_renderer != null)
                _renderer.material.color = ColorDefault;
        }

        public void SetHit()
        {
            isJudged = true;
            if (_renderer != null)
                _renderer.material.color = ColorHit;
        }

        public void SetMiss()
        {
            isJudged = true;
            if (_renderer != null)
                _renderer.material.color = ColorMiss;
        }

        /// <summary>回收到对象池</summary>
        public void Recycle()
        {
            isActive = false;
            gameObject.SetActive(false);}
    }
}
