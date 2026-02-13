// AudioConductor.cs — 核心音频控制器，dspTime驱动 + 分轨播放管理
//挂载到场景中的空GameObject上，Awake时自动注册到ServiceLocator
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

        // --- IAudioConductor 属性 ---
        public double SongTime => _songTime;
        public bool IsPlaying => _isPlaying;
        public SongData CurrentSongData => _songData;

        // 内部状态
        private double _dspStartTime;
        private double _songTime;
        private double _pauseTime;     // 暂停时记录的songTime
        private bool _isPlaying;
        private SongData _songData;

        // 分轨AudioSource（运行时创建）
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
            if (autoPlayOnStart)
                Play();
        }

        void Update()
        {
            if (!_isPlaying) return;
            // 核心：dspTime驱动，零漂移
            _songTime = AudioSettings.dspTime - _dspStartTime;
        }

        // --- 分轨初始化 ---
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
                if (_clips[i] != null)_stems[i].clip = _clips[i];
            }
        }

        // --- IAudioConductor 方法 ---

        public void LoadSong(string fileName)
        {
            // 从Resources/MIDI/加载（去掉扩展名）
            string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string fullPath = System.IO.Path.Combine(
                Application.dataPath, "Resources", "MIDI", fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogWarning($"[AudioConductor] MIDI文件不存在: {fullPath}，将以静默模式运行");
                return;
            }
            _songData = MidiParser.Parse(fullPath);
            Debug.Log($"[AudioConductor] 歌曲加载完成: {_songData.songName} | " +
                      $"音符数={_songData.allNotes.Length} | 时长={_songData.totalDuration:F2}s");
        }

        public void Play()
        {
            // 使用PlayScheduled确保多轨同步
            double scheduleTime = AudioSettings.dspTime +0.1; // 100ms缓冲
            _dspStartTime = scheduleTime;
            _songTime = 0;
            _isPlaying = true;

            for (int i = 0; i < _stems.Length; i++)
            {
                if (_stems[i].clip != null)
                    _stems[i].PlayScheduled(scheduleTime);
            }
            Debug.Log($"[AudioConductor] 播放开始 | dspStart={_dspStartTime:F4}");EventBus.OnGameStateChanged?.Invoke(GameState.Playing);
        }

        public void Pause()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _pauseTime = _songTime;
            foreach (var s in _stems)
                if (s.isPlaying) s.Pause();
            Debug.Log($"[AudioConductor] 暂停 | songTime={_songTime:F3}s");
            EventBus.OnGameStateChanged?.Invoke(GameState.Paused);
        }

        public void Resume()
        {
            if (_isPlaying) return;
            // 重新校准dspStartTime，使songTime从pauseTime继续
            _dspStartTime = AudioSettings.dspTime - _pauseTime;
            _isPlaying = true;
            foreach (var s in _stems)
                s.UnPause();
            Debug.Log($"[AudioConductor] 恢复 | songTime={_pauseTime:F3}s");
            EventBus.OnGameStateChanged?.Invoke(GameState.Playing);
        }

        public void Stop()
        {
            _isPlaying = false;
            _songTime = 0;
            foreach (var s in _stems)
                s.Stop();
            Debug.Log("[AudioConductor] 停止");
            EventBus.OnGameStateChanged?.Invoke(GameState.GameOver);
        }

        // --- 分轨静音控制（供后续阶段使用）---

        public void MuteTrack(TrackType track)
        {
            int idx = (int)track;
            if (idx >= 0 && idx < _stems.Length)
                _stems[idx].mute = true;
        }

        public void UnmuteTrack(TrackType track)
        {
            int idx = (int)track;
            if (idx >= 0 && idx < _stems.Length)
                _stems[idx].mute = false;
        }

        void OnDestroy()
        {
            // 清理ServiceLocator注册
            if (ServiceLocator.Has<IAudioConductor>())
                ServiceLocator.Reset();
        }
    }
}
