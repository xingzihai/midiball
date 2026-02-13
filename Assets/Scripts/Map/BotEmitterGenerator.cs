// BotEmitterGenerator.cs —辅助球外侧发声器生成（独立对象池）
// 在管道外侧±BOT_EMITTER_X位置生成辅助发声器，供AutoBot碰撞
using System.Collections.Generic;
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Map
{
    public class BotEmitterGenerator : MonoBehaviour
    {
        [Header("辅助发声器池")]
        [SerializeField] private int poolSize = 500;
        [SerializeField] private Vector3 emitterScale = new Vector3(0.12f, 1.0f, 1.5f);

        [Header("可视范围")]
        [SerializeField] private float spawnAhead = 80f;
        [SerializeField] private float despawnBehind = 10f;

        // 按轨道分别管理：每个轨道有独立的池和活跃列表
        private Dictionary<TrackType, TrackPool> _trackPools = new();
        private IAudioConductor _conductor;
        private bool _initialized;

        //轨道颜色（比AutoBot颜色暗一些）
        private static readonly Dictionary<TrackType, Color> TrackColors = new()
        {
            { TrackType.Drums,  new Color(0.7f, 0.4f, 0f, 0.5f) },
            { TrackType.Bass,   new Color(0.1f, 0.3f, 0.7f, 0.5f) },
            { TrackType.Chords, new Color(0.5f, 0.2f, 0.7f, 0.5f) }
        };
        // 轨道→侧面映射（与AutoBotController一致）
        private static readonly Dictionary<TrackType, float> TrackSide = new()
        {
            { TrackType.Drums,  1f },  // 右
            { TrackType.Bass,-1f },  // 左
            { TrackType.Chords, 1f }   // 右高层
        };
        private static readonly Dictionary<TrackType, float> TrackY = new()
        {
            { TrackType.Drums,  0.3f },
            { TrackType.Bass,   0.3f },
            { TrackType.Chords, 0.8f }
        };

        void Update()
        {
            if (!_initialized) { TryInit(); return; }
            if (_conductor == null) return;
            float playerZ = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            foreach (var kv in _trackPools)UpdateTrackPool(kv.Value, playerZ);
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            var data = _conductor?.CurrentSongData;
            if (data == null) return;
            // 为三个伴奏轨道各建池
            InitTrackPool(TrackType.Drums, data.drumsNotes);
            InitTrackPool(TrackType.Bass, data.bassNotes);
            InitTrackPool(TrackType.Chords, data.chordsNotes);
            _initialized = true;
            Debug.Log("[BotEmitterGen] 初始化完成 | " +
                $"Drums={data.drumsNotes.Length} Bass={data.bassNotes.Length} " +
                $"Chords={data.chordsNotes.Length}");
        }

        private void InitTrackPool(TrackType track, NoteData[] notes)
        {
            var parent = new GameObject($"BotEmitters_{track}").transform;
            parent.SetParent(transform);
            int size = Mathf.Min(poolSize, notes.Length + 50);
            var pool = new TrackPool
            {
                notes = notes, parent = parent, side = TrackSide[track],
                yPos = TrackY[track], color = TrackColors[track]
            };
            for (int i = 0; i < size; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(parent);
                cube.transform.localScale = emitterScale;
                var col = cube.GetComponent<BoxCollider>();
                if (col != null) Destroy(col); // 辅助发声器不需要碰撞
                var rend = cube.GetComponent<Renderer>();
                if (rend != null) SetEmitterMaterial(rend, pool.color);
                cube.SetActive(false);
                pool.pool.Enqueue(cube);
            }
            _trackPools[track] = pool;
        }

        private void UpdateTrackPool(TrackPool tp, float playerZ)
        {
            float spawnZ = playerZ + spawnAhead;
            // 生成
            while (tp.nextIdx < tp.notes.Length)
            {
                float noteZ = (float)tp.notes[tp.nextIdx].timeInSeconds * GameConstants.SCROLL_SPEED;
                if (noteZ > spawnZ) break;
                if (tp.pool.Count > 0)
                {
                    var obj = tp.pool.Dequeue();
                    float x = tp.side * GameConstants.BOT_EMITTER_X;
                    obj.transform.position = new Vector3(x, tp.yPos + 0.5f, noteZ);
                    obj.SetActive(true);
                    tp.active.Add(obj);
                }
                tp.nextIdx++;
            }
            // 回收
            float recycleZ = playerZ - despawnBehind;
            for (int i = tp.active.Count - 1; i >= 0; i--)
            {
                if (tp.active[i].transform.position.z < recycleZ)
                {
                    tp.active[i].SetActive(false);
                    tp.pool.Enqueue(tp.active[i]);
                    tp.active.RemoveAt(i);
                }
            }
        }

        private void SetEmitterMaterial(Renderer rend, Color c)
        {
            var mat = rend.material;
            mat.color = c;
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        /// <summary>轨道池数据</summary>
        private class TrackPool
        {
            public NoteData[] notes;
            public Transform parent;
            public float side, yPos;
            public Color color;
            public int nextIdx;
            public Queue<GameObject> pool = new();
            public List<GameObject> active = new();}
    }
}
