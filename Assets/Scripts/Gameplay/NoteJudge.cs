// NoteJudge.cs — 音符判定系统（适配碰撞反弹机制）
// Hit由NoteMarker.OnTriggerEnter触发，本类负责Miss检测、音效和事件
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;
using StarPipe.Map;

namespace StarPipe.Gameplay
{
    public class NoteJudge : MonoBehaviour
    {
        [Header("音效")]
        [SerializeField] private float hitToneFreq = 880f;
        [SerializeField] private float hitToneDuration = 0.08f;
        [SerializeField] private float missToneFreq = 220f;
        [SerializeField] private float missToneDuration = 0.12f;

        private AudioSource _sfxSource;
        private IAudioConductor _conductor;
        private MapGenerator _mapGen;
        private NoteData[] _notes;
        private int _nextMissCheckIndex;
        private int _combo;
        private bool _initialized;

        // 供NoteMarker碰撞时调用的静态引用
        private static NoteJudge _instance;
        public static NoteJudge Instance => _instance;

        void Awake() { _instance = this; }

        void Update()
        {
            if (!_initialized) { TryInit(); return; }
            if (_notes == null || !_conductor.IsPlaying) return;
            CheckMissedNotes();
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            if (_conductor?.CurrentSongData == null) return;

            _mapGen = FindObjectOfType<MapGenerator>();
            _notes = _conductor.CurrentSongData.allNotes;
            _nextMissCheckIndex = 0;
            _combo = 0;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;

            _initialized = true;
            Debug.Log($"[NoteJudge] 初始化完成 | 待判定音符={_notes.Length}");
        }

        /// <summary>检测超时未碰撞的音符，标记为Miss</summary>
        private void CheckMissedNotes()
        {
            float songTime = (float)_conductor.SongTime;
            float tol = GameConstants.JUDGE_TOLERANCE;

            while (_nextMissCheckIndex < _notes.Length)
            {
                float noteTime = (float)_notes[_nextMissCheckIndex].timeInSeconds;
                // 还没到判定窗口，停止检查
                if (songTime < noteTime + tol) break;
                // 超过判定窗口且未被碰撞判定 -> Miss
                if (!IsNoteJudged(_nextMissCheckIndex))
                {
                    ProcessMiss(_nextMissCheckIndex);
                }
                _nextMissCheckIndex++;
            }
        }

        private bool IsNoteJudged(int idx)
        {
            if (_mapGen == null) return false;
            foreach (var marker in _mapGen.ActiveMarkers)
            {
                if (marker.noteIndex == idx && marker.isJudged) return true;
            }
            return false;
        }

        /// <summary>由NoteMarker碰撞时调用</summary>
        public void NotifyHit(int noteIndex)
        {
            if (noteIndex < 0 || noteIndex >= _notes.Length) return;
            _combo++;
            var note = _notes[noteIndex];
            Vector3 pos = new Vector3(note.xPosition, 0.5f,
                (float)note.timeInSeconds * GameConstants.SCROLL_SPEED);

            PlayTone(hitToneFreq + note.midiNote * 5f, hitToneDuration);
            EventBus.OnNoteHit?.Invoke(note.noteType, pos);
            EventBus.OnComboChanged?.Invoke(_combo);
            Debug.Log($"[Judge] HIT #{noteIndex} combo={_combo}");
        }

        private void ProcessMiss(int idx)
        {
            _combo = 0;
            var note = _notes[idx];
            Vector3 pos = new Vector3(note.xPosition, 0.5f,
                (float)note.timeInSeconds * GameConstants.SCROLL_SPEED);

            PlayTone(missToneFreq, missToneDuration,0.3f);
            UpdateMarkerMiss(idx);
            EventBus.OnNoteMiss?.Invoke(pos);
            EventBus.OnComboChanged?.Invoke(0);
            Debug.Log($"[Judge] MISS #{idx}");
        }

        private void PlayTone(float freq, float dur, float vol = 0.5f)
        {
            if (_sfxSource == null) return;
            _sfxSource.clip = ToneGenerator.CreateTone(freq, dur, vol);
            _sfxSource.Play();
        }

        private void UpdateMarkerMiss(int noteIndex)
        {
            if (_mapGen == null) return;
            foreach (var marker in _mapGen.ActiveMarkers)
            {
                if (marker.noteIndex == noteIndex && !marker.isJudged)
                {
                    marker.SetMiss();
                    break;
                }
            }
        }
    }
}
