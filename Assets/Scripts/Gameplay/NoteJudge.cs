// NoteJudge.cs — 音符判定系统，基于距离检测
// 独立模块，通过EventBus广播Hit/Miss事件
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    public class NoteJudge : MonoBehaviour
    {
        private IAudioConductor _conductor;
        private MapGenerator _mapGen;
        private Transform _player;
        private NoteData[] _notes;

        private int _nextJudgeIndex; // 下一个待判定的音符索引（在melodyNotes中）
        private int _combo;

        void Start()
        {
            _conductor = ServiceLocator.Get<IAudioConductor>();
            _mapGen = FindObjectOfType<MapGenerator>();
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) _player = pc.transform;

            if (_conductor?.CurrentSongData != null)
                _notes = _conductor.CurrentSongData.melodyNotes;

            _nextJudgeIndex = 0;
            _combo = 0;}

        void Update()
        {
            if (_notes == null || _player == null || !_conductor.IsPlaying) return;
            float songTime = (float)_conductor.SongTime;
            float tol = GameConstants.JUDGE_TOLERANCE;

            // 扫描所有到达判定窗口的音符
            while (_nextJudgeIndex < _notes.Length)
            {
                float noteTime = (float)_notes[_nextJudgeIndex].timeInSeconds;

                // 还没进入判定窗口
                if (songTime < noteTime - tol) break;

                // 已超过判定窗口 -> Miss
                if (songTime > noteTime + tol)
                {
                    ProcessMiss(_nextJudgeIndex);
                    _nextJudgeIndex++;
                    continue;
                }

                // 在判定窗口内：检查X距离
                float dx = Mathf.Abs(_player.position.x - _notes[_nextJudgeIndex].xPosition);
                if (dx <= GameConstants.HIT_RADIUS)
                {
                    ProcessHit(_nextJudgeIndex);
                    _nextJudgeIndex++;
                }
                else if (songTime > noteTime + tol)
                {
                    //窗口结束仍未命中
                    ProcessMiss(_nextJudgeIndex);
                    _nextJudgeIndex++;
                }
                else
                {
                    break; // 还在窗口内，等下一帧
                }
            }
        }

        private void ProcessHit(int melodyIndex)
        {
            _combo++;
            var note = _notes[melodyIndex];
            Vector3 pos = new Vector3(note.xPosition, 0.5f,
                (float)note.timeInSeconds * GameConstants.SCROLL_SPEED);

            // 更新方块视觉
            UpdateMarkerState(melodyIndex, true);

            EventBus.OnNoteHit?.Invoke(note.noteType, pos);
            EventBus.OnComboChanged?.Invoke(_combo);
            Debug.Log($"[Judge] HIT #{melodyIndex} combo={_combo} | x={note.xPosition:F1}");
        }

        private void ProcessMiss(int melodyIndex)
        {
            _combo = 0;
            var note = _notes[melodyIndex];
            Vector3 pos = new Vector3(note.xPosition, 0.5f,
                (float)note.timeInSeconds * GameConstants.SCROLL_SPEED);

            UpdateMarkerState(melodyIndex, false);

            EventBus.OnNoteMiss?.Invoke(pos);
            EventBus.OnComboChanged?.Invoke(0);
            Debug.Log($"[Judge] MISS #{melodyIndex} | x={note.xPosition:F1}");
        }

        /// <summary>查找对应Marker并设置Hit/Miss状态</summary>
        private void UpdateMarkerState(int melodyIndex, bool isHit)
        {
            if (_mapGen == null) return;
            foreach (var marker in _mapGen.ActiveMarkers)
            {
                if (marker.noteIndex == melodyIndex && !marker.isJudged)
                {
                    if (isHit) marker.SetHit();
                    else marker.SetMiss();
                    break;
                }
            }
        }
    }
}
