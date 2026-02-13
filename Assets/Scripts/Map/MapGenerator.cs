// MapGenerator.cs — 根据MIDI数据生成发声器方块（对象池管理）
using System.Collections.Generic;
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("对象池配置")]
        [SerializeField] private int poolSize = 200;
        [SerializeField] private Vector3 noteScale = new Vector3(0.8f, 0.8f, 0.3f);

        [Header("可视范围（Z轴前方多远开始显示）")]
        [SerializeField] private float spawnAhead = 80f;  // 前方80单位开始显示
        [SerializeField] private float despawnBehind = 10f; // 身后10单位回收

        // 对象池
        private Queue<NoteMarker> _pool = new Queue<NoteMarker>();
        private List<NoteMarker> _activeMarkers = new List<NoteMarker>();
        private Transform _poolParent;

        // 数据
        private IAudioConductor _conductor;
        private NoteData[] _notes;
        private int _nextSpawnIndex;// 下一个待生成的音符索引

        // 公共访问：供NoteJudge查找激活的Marker
        public List<NoteMarker> ActiveMarkers => _activeMarkers;

        void Start()
        {
            _conductor = ServiceLocator.Get<IAudioConductor>();
            if (_conductor?.CurrentSongData == null)
            {
                Debug.LogWarning("[MapGenerator] 无SongData，跳过生成");
                return;
            }
            //阶段三：仅处理主旋律轨道
            _notes = _conductor.CurrentSongData.melodyNotes;
            _nextSpawnIndex = 0;

            InitPool();
            Debug.Log($"[MapGenerator] 初始化完成 | 音符数={_notes.Length} | 池大小={poolSize}");
        }

        void Update()
        {
            if (_notes == null || _conductor == null) return;
            float playerZ = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;

            SpawnNotes(playerZ);RecycleNotes(playerZ);
        }

        private void InitPool()
        {
            _poolParent = new GameObject("NotePool").transform;
            _poolParent.SetParent(transform);

            for (int i = 0; i < poolSize; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(_poolParent);
                cube.transform.localScale = noteScale;
                // 移除碰撞体（用距离判定，不用物理）
                var col = cube.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var marker = cube.AddComponent<NoteMarker>();
                cube.SetActive(false);
                _pool.Enqueue(marker);
            }
        }

        /// <summary>生成前方即将进入视野的音符方块</summary>
        private void SpawnNotes(float playerZ)
        {
            float spawnZ = playerZ + spawnAhead;

            while (_nextSpawnIndex < _notes.Length)
            {
                float noteZ = (float)_notes[_nextSpawnIndex].timeInSeconds * GameConstants.SCROLL_SPEED;
                if (noteZ > spawnZ) break; // 还没到生成距离

                if (_pool.Count > 0)
                {
                    var marker = _pool.Dequeue();
                    Vector3 pos = new Vector3(_notes[_nextSpawnIndex].xPosition, 0.5f, noteZ);
                    marker.Reset(_nextSpawnIndex, pos);
                    _activeMarkers.Add(marker);
                }
                else
                {
                    Debug.LogWarning("[MapGenerator] 对象池耗尽");
                }
                _nextSpawnIndex++;
            }
        }

        /// <summary>回收已经在玩家身后的方块</summary>
        private void RecycleNotes(float playerZ)
        {
            float recycleZ = playerZ - despawnBehind;
            for (int i = _activeMarkers.Count - 1; i >= 0; i--)
            {
                if (_activeMarkers[i].transform.position.z < recycleZ)
                {
                    _activeMarkers[i].Recycle();
                    _pool.Enqueue(_activeMarkers[i]);
                    _activeMarkers.RemoveAt(i);
                }
            }
        }
    }
}
