// AudioConductor.cs — 核心音频控制器，dspTime驱动 + 分轨播放管理
// 挂载到场景中的空GameObject上，Awake时自动注册到ServiceLocator
// 关键：dspTime按音频缓冲块更新，帧间用deltaTime插值平滑
using UnityEngine;
using StarPipe.Core;

namespace StarPipe.Audio
{
    public class AudioConductor : MonoBehaviour, IAudioConductor
    {
        [Header("MIDI配置")]
        [SerializeField] private string midiFileName = "test.mid";
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
            }else
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
            string fullPath = System.IO.Path.Combine(
                Application.dataPath, "Resources", "MIDI", fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogWarning($"[AudioConductor] MIDI文件不存在: {fullPath}，使用程序化生成");
                _songData = MidiParser.GenerateTestNotes(500);
                return;
            }
            // 直接使用真实MIDI数据，不再因音符数不足而丢弃
            _songData = MidiParser.Parse(fullPath);
            Debug.Log($"[AudioConductor] 歌曲加载完成: {_songData.songName} | " +
                      $"音符数={_songData.allNotes.Length} | 时长={_songData.totalDuration:F2}s | " +
                      $"Melody={_songData.melodyNotes.Length} Drums={_songData.drumsNotes.Length} " +
                      $"Bass={_songData.bassNotes.Length} Chords={_songData.chordsNotes.Length}");
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
            {
                if (_stems[i].clip != null)
                    _stems[i].PlayScheduled(scheduleTime);
            }
            Debug.Log($"[AudioConductor] 播放开始 | dspStart={_dspStartTime:F4}");
            EventBus.OnGameStateChanged?.Invoke(GameState.Playing);
        }

        public void Pause()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _pauseTime = _smoothTime;
            foreach (var s in _stems)
                if (s.isPlaying) s.Pause();
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
