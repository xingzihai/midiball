// NoteJudge.cs — 音符判定系统，基于距离检测
// Hit时施加向管道中心的反弹冲量，Miss时触发事件
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    public class NoteJudge : MonoBehaviour
    {
        [Header("反弹参数")]
        [SerializeField] private float hitBounceForce = 12f; // 命中后向中心的弹射力

        private IAudioConductor _conductor;
        private MapGenerator _mapGen;
        private PlayerController _playerCtrl;
        private Transform _player;
        private NoteData[] _notes;
        private int _nextJudgeIndex;
        private int _combo;
        private bool _initialized;

        void Update()
        {
            if (!_initialized) { TryInit(); return; }
            if (_notes == null || _player == null || !_conductor.IsPlaying) return;

            float songTime = (float)_conductor.SongTime;
            float tol = GameConstants.JUDGE_TOLERANCE;

            while (_nextJudgeIndex < _notes.Length)
            {
                float noteTime = (float)_notes[_nextJudgeIndex].timeInSeconds;
                if (songTime < noteTime - tol) break;

                if (songTime > noteTime + tol)
                {
                    ProcessMiss(_nextJudgeIndex);
                    _nextJudgeIndex++;
                    continue;
                }

                float dx = Mathf.Abs(_player.position.x - _notes[_nextJudgeIndex].xPosition);
                if (dx <= GameConstants.HIT_RADIUS)
                {
                    ProcessHit(_nextJudgeIndex);
                    _nextJudgeIndex++;
                }
                else if (songTime > noteTime + tol)
                {
                    ProcessMiss(_nextJudgeIndex);
                    _nextJudgeIndex++;
                }
                else { break; }
            }
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            if (_conductor?.CurrentSongData == null) return;

            _mapGen = FindObjectOfType<MapGenerator>();
            _playerCtrl = FindObjectOfType<PlayerController>();
            if (_playerCtrl != null) _player = _playerCtrl.transform;

            _notes = _conductor.CurrentSongData.allNotes;
            _nextJudgeIndex = 0;
            _combo = 0;
            _initialized = true;
            Debug.Log($"[NoteJudge] 初始化完成 | 待判定音符={_notes.Length}");
        }

        private void ProcessHit(int idx)
        {
            _combo++;
            var note = _notes[idx];
            Vector3 pos = new Vector3(note.xPosition, 0.5f,
                (float)note.timeInSeconds * GameConstants.SCROLL_SPEED);

            // 向管道中心弹射：音符在右侧则向左弹，反之向右
            if (_playerCtrl != null)
            {
                float dir = (_player.position.x > 0) ? -1f : 1f;
                _playerCtrl.ApplyLateralImpulse(dir * hitBounceForce);
            }

            UpdateMarkerState(idx, true);
            EventBus.OnNoteHit?.Invoke(note.noteType, pos);
            EventBus.OnComboChanged?.Invoke(_combo);
            Debug.Log($"[Judge] HIT #{idx} combo={_combo} | x={note.xPosition:F1}");
        }

        private void ProcessMiss(int idx)
        {
            _combo = 0;
            var note = _notes[idx];
            Vector3 pos = new Vector3(note.xPosition, 0.5f,
                (float)note.timeInSeconds * GameConstants.SCROLL_SPEED);
            UpdateMarkerState(idx, false);
            EventBus.OnNoteMiss?.Invoke(pos);
            EventBus.OnComboChanged?.Invoke(0);
            Debug.Log($"[Judge] MISS #{idx} | x={note.xPosition:F1}");
        }

        private void UpdateMarkerState(int noteIndex, bool isHit)
        {
            if (_mapGen == null) return;
            foreach (var marker in _mapGen.ActiveMarkers)
            {
                if (marker.noteIndex == noteIndex && !marker.isJudged)
                {
                    if (isHit) marker.SetHit();
                    else marker.SetMiss();
                    break;
                }
            }
        }
    }
}
