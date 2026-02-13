// AudioConductor.cs — 核心音频控制器，dspTime驱动 + 分轨播放管理
using UnityEngine;
using StarPipe.Core;

namespace StarPipe.Audio
{
    public class AudioConductor : MonoBehaviour, IAudioConductor
    {
        [Header("MIDI配置")]
        [SerializeField] private string midiFileName = "时间煮雨.mid";
        [SerializeField] private bool autoPlayOnStart = true;

        [Header("音频分轨（可选，无文件时静默模式）")]
        [SerializeField] private AudioClip melodyClip;
        [SerializeField] private AudioClip drumsClip;
        [SerializeField] private AudioClip bassClip;
        [SerializeField] private AudioClip chordsClip;

        public double SongTime => _smoothTime;
        public bool IsPlaying => _isPlaying;
        public SongData CurrentSongData => _songData;

        private double _dspStartTime;
        private double _songTime;
        private double _smoothTime;
        private double _lastDspTime;
        private double _pauseTime;
        private bool _isPlaying;
        private SongData _songData;
        private AudioSource[] _stems;
        private AudioClip[] _clips;

        void Awake()
        {
            ServiceLocator.Register<IAudioConductor>(this);
            InitStems();
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(midiFileName))
                LoadSong(midiFileName);
            if (autoPlayOnStart) Play();
        }

        void Update()
        {
            if (!_isPlaying) return;
            double currentDsp = AudioSettings.dspTime;
            _songTime = currentDsp - _dspStartTime;
            if (currentDsp != _lastDspTime)
            {
                _smoothTime = _songTime;
                _lastDspTime = currentDsp;
            }
            else
            {
                _smoothTime += Time.deltaTime;
            }
        }

        private void InitStems()
        {
            _clips = new[] { melodyClip, drumsClip, bassClip, chordsClip };
            _stems = new AudioSource[4];
            string[] names = { "Stem_Melody", "Stem_Drums", "Stem_Bass", "Stem_Chords" };
            for (int i = 0; i < 4; i++)
            {
                var child = new GameObject(names[i]);
                child.transform.SetParent(transform);
                _stems[i] = child.AddComponent<AudioSource>();
                _stems[i].playOnAwake = false;
                _stems[i].loop = false;
                if (_clips[i] != null) _stems[i].clip = _clips[i];
            }
        }

        public void LoadSong(string fileName)
        {
            string midiDir = System.IO.Path.Combine(Application.dataPath, "Resources", "MIDI");
            string fullPath = System.IO.Path.Combine(midiDir, fileName);

            // 回退逻辑：优先加载指定文件，不存在则尝试时间煮雨.mid
            if (!System.IO.File.Exists(fullPath))
            {
                string fallback = System.IO.Path.Combine(midiDir, "时间煮雨.mid");
                if (System.IO.File.Exists(fallback))
                {
                    Debug.LogWarning($"[AudioConductor] {fileName}不存在，回退到时间煮雨.mid");
                    fullPath = fallback;
                }else
                {
                    Debug.LogWarning($"[AudioConductor] 无MIDI文件，使用程序化生成");
                    _songData = MidiParser.GenerateTestNotes(500);
                    return;
                }
            }
            _songData = MidiParser.Parse(fullPath);
            Debug.Log($"[AudioConductor] 歌曲加载: {_songData.songName} | " +
                      $"总音符={_songData.allNotes.Length} 时长={_songData.totalDuration:F1}s | " +
                      $"M={_songData.melodyNotes.Length} D={_songData.drumsNotes.Length} " +
                      $"B={_songData.bassNotes.Length} C={_songData.chordsNotes.Length}");
        }

        public void Play()
        {
            double scheduleTime = AudioSettings.dspTime + 0.1;
            _dspStartTime = scheduleTime;
            _songTime = 0;
            _smoothTime = 0;
            _lastDspTime = 0;
            _isPlaying = true;
            for (int i = 0; i < _stems.Length; i++)
                if (_stems[i].clip != null) _stems[i].PlayScheduled(scheduleTime);
            Debug.Log($"[AudioConductor] 播放开始 | dspStart={_dspStartTime:F4}");
            EventBus.OnGameStateChanged?.Invoke(GameState.Playing);
        }

        public void Pause()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _pauseTime = _smoothTime;
            foreach (var s in _stems) if (s.isPlaying) s.Pause();
            EventBus.OnGameStateChanged?.Invoke(GameState.Paused);
        }

        public void Resume()
        {
            if (_isPlaying) return;
            _dspStartTime = AudioSettings.dspTime - _pauseTime;
            _smoothTime = _pauseTime;
            _lastDspTime = 0;
            _isPlaying = true;
            foreach (var s in _stems) s.UnPause();
            EventBus.OnGameStateChanged?.Invoke(GameState.Playing);
        }

        public void Stop()
        {
            _isPlaying = false;
            _songTime = 0;
            _smoothTime = 0;
            foreach (var s in _stems) s.Stop();
            EventBus.OnGameStateChanged?.Invoke(GameState.GameOver);
        }

        public void MuteTrack(TrackType track)
        {
            int idx = (int)track;
            if (idx >= 0 && idx < _stems.Length) _stems[idx].mute = true;
        }

        public void UnmuteTrack(TrackType track)
        {
            int idx = (int)track;
            if (idx >= 0 && idx < _stems.Length) _stems[idx].mute = false;
        }

        void OnDestroy()
        {
            if (ServiceLocator.Has<IAudioConductor>()) ServiceLocator.Reset();
        }
    }
}
