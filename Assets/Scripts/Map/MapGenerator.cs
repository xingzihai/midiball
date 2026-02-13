// MapGenerator.cs — 根据MIDI数据生成墙壁式发声器（对象池管理）
// 发声器竖立在管道两侧，小球碰撞后反弹
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
        // 发声器竖立：窄(X=0.8), 高(Y=1.5), 长(Z=5.0增大碰撞范围)
        [SerializeField] private Vector3 noteScale = new Vector3(0.8f, 1.5f, 5.0f);

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
            _initialized = true;Debug.Log($"[MapGenerator] 初始化完成 | 音符数={_notes.Length} | 池大小={poolSize}");
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
                var col = cube.GetComponent<BoxCollider>();
                if (col != null) col.isTrigger = true;
                cube.layer = LayerMask.NameToLayer("Default");
                var marker = cube.AddComponent<NoteMarker>();
                cube.SetActive(false);
                _pool.Enqueue(marker);
            }
        }

        /// <summary>墙壁紧贴管道边缘放置</summary>
        private float CalcWallX(float noteX)
        {
            float hw = GameConstants.TRACK_HALF_WIDTH;
            float halfThick = noteScale.x * 0.5f;
            // 右侧墙壁：内边缘贴着轨道边界
            if (noteX > 0) return hw - halfThick;
            // 左侧墙壁
            else return -hw + halfThick;
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
                    float wallX = CalcWallX(rawX);
                    // Y=0.75让发声器底部贴地(高度1.5，中心在0.75)
                    Vector3 pos = new Vector3(wallX, 0.75f, noteZ);
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
                if (_activeMarkers[i].transform.position.z< recycleZ)
                {
                    _activeMarkers[i].Recycle();
                    _pool.Enqueue(_activeMarkers[i]);
                    _activeMarkers.RemoveAt(i);
                }
            }
        }
    }
}
