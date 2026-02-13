// MapGenerator.cs — 根据MIDI数据生成横向发声器挡板（对象池管理）
// 发声器从墙壁向内延伸，小球碰撞后反弹
using System.Collections.Generic;
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("对象池配置")]
        [SerializeField] private int poolSize = 1000;
        // 横向挡板：宽(X)=从墙向内延伸长度, 高(Y), 厚(Z)加大防穿透
        [SerializeField] private Vector3 noteScale = new Vector3(3.0f, 0.6f, 2.0f);

        [Header("可视范围（Z轴前方多远开始显示）")]
        [SerializeField] private float spawnAhead = 80f;
        [SerializeField] private float despawnBehind = 10f;

        private Queue<NoteMarker> _pool = new Queue<NoteMarker>();
        private List<NoteMarker> _activeMarkers = new List<NoteMarker>();
        private Transform _poolParent;
        private IAudioConductor _conductor;
        private NoteData[] _notes;
        private int _nextSpawnIndex;
        private bool _initialized;

        public List<NoteMarker> ActiveMarkers => _activeMarkers;

        void Update()
        {
            if (!_initialized) { TryInit(); return; }
            if (_notes == null || _conductor == null) return;
            float playerZ = (float)_conductor.SongTime * GameConstants.SCROLL_SPEED;
            SpawnNotes(playerZ);
            RecycleNotes(playerZ);
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            if (_conductor?.CurrentSongData == null) return;
            _notes = _conductor.CurrentSongData.allNotes;
            _nextSpawnIndex = 0;
            InitPool();
            _initialized = true;
            Debug.Log($"[MapGenerator] 初始化完成 | 音符数={_notes.Length} | 池大小={poolSize}");
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
                // 保留BoxCollider作为Trigger用于碰撞检测
                var col = cube.GetComponent<BoxCollider>();
                if (col != null) col.isTrigger = true;
                cube.layer = LayerMask.NameToLayer("Default");
                var marker = cube.AddComponent<NoteMarker>();
                cube.SetActive(false);
                _pool.Enqueue(marker);
            }
        }

        /// <summary>
        /// 计算横向挡板的X位置：从墙壁向内延伸
        /// </summary>
        private float CalcBarCenterX(float noteX)
        {
            float halfBar = noteScale.x * 0.5f;
            float hw = GameConstants.TRACK_HALF_WIDTH;
            if (noteX > 0)return hw - halfBar;
            else
                return -hw + halfBar;
        }

        private void SpawnNotes(float playerZ)
        {
            float spawnZ = playerZ + spawnAhead;
            while (_nextSpawnIndex < _notes.Length)
            {
                float noteZ = (float)_notes[_nextSpawnIndex].timeInSeconds * GameConstants.SCROLL_SPEED;
                if (noteZ > spawnZ) break;
                if (_pool.Count > 0)
                {
                    var marker = _pool.Dequeue();
                    float rawX = _notes[_nextSpawnIndex].xPosition;
                    float barX = CalcBarCenterX(rawX);
                    Vector3 pos = new Vector3(barX, 0.5f, noteZ);
                    marker.Reset(_nextSpawnIndex, pos, rawX > 0);
                    _activeMarkers.Add(marker);
                }
                else { Debug.LogWarning("[MapGenerator] 对象池耗尽"); }
                _nextSpawnIndex++;
            }
        }

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
