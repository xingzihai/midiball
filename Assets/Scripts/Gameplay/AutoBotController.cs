// AutoBotController.cs — 自动球在管道外侧轨道插值飞行
//碰撞外侧辅助发声器，发出和弦配乐音效
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Gameplay
{
    public class AutoBotController : MonoBehaviour
    {
        public TrackType Track { get; private set; }

        private NoteData[] _notes;
        private IAudioConductor _conductor;
        private int _currentIdx;
        private float _yOffset;
        private float _sideSign; // +1右侧轨道, -1左侧轨道
        private Renderer _renderer;
        private AudioSource _sfx;
        private int _lastPlayedIdx = -1;

        //轨道颜色
        private static readonly Color ColorDrums= new Color(1f, 0.6f, 0f, 0.7f);
        private static readonly Color ColorBass   = new Color(0.2f, 0.5f, 1f, 0.7f);
        private static readonly Color ColorChords = new Color(0.8f, 0.3f, 1f, 0.7f);

        /// <summary>静态工厂：创建自动球并初始化</summary>
        public static AutoBotController Create(TrackType track, NoteData[] notes, Transform parent)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"AutoBot_{track}";
            sphere.transform.SetParent(parent);
            sphere.transform.localScale = Vector3.one * 0.5f;
            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var bot = sphere.AddComponent<AutoBotController>();
            bot.Init(track, notes);
            return bot;
        }

        private void Init(TrackType track, NoteData[] notes)
        {
            Track = track;
            _notes = notes;
            _currentIdx = 0;
            // 不同轨道分配到不同侧+不同Y高度
            switch (track)
            {
                case TrackType.Drums:
                    _sideSign = 1f; _yOffset = 0.3f; break;  // 右侧
                case TrackType.Bass:
                    _sideSign = -1f; _yOffset = 0.3f; break; // 左侧
                case TrackType.Chords:
                    _sideSign = 1f; _yOffset = 0.8f; break;  // 右侧高层
                default:
                    _sideSign = 1f; _yOffset = 0.5f; break;
            }
            SetupMaterial(track);
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = 0.3f;
            if (ServiceLocator.Has<IAudioConductor>())
                _conductor = ServiceLocator.Get<IAudioConductor>();
            Debug.Log($"[AutoBot] {track} 创建 | side={(_sideSign>0?"右":"左")} 音符={notes.Length}");
        }

        private void SetupMaterial(TrackType track)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null) return;
            var mat = _renderer.material;
            mat.color = GetTrackColor(track);
            // 半透明设置
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        void Update()
        {
            if (_conductor == null || _notes == null || _notes.Length == 0) return;
            if (!_conductor.IsPlaying) return;
            float songTime = (float)_conductor.SongTime;
            _currentIdx = FindNoteIndex(songTime);
            transform.position = CalcPosition(songTime);
            CheckNoteArrival(songTime);
        }

        /// <summary>二分查找当前音符区间</summary>
        private int FindNoteIndex(float songTime)
        {
            int lo = 0, hi = _notes.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if ((float)_notes[mid].timeInSeconds <= songTime) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        /// <summary>外侧轨道插值：X在BOT_TRACK范围内根据音符左右摆动</summary>
        private Vector3 CalcPosition(float songTime)
        {
            float z = songTime * GameConstants.SCROLL_SPEED;
            float inner = GameConstants.BOT_TRACK_INNER;
            float outer = GameConstants.BOT_TRACK_OUTER;
            if (_currentIdx >= _notes.Length - 1)
            {
                // 停在外侧发声器位置
                float x = _sideSign * GameConstants.BOT_EMITTER_X;
                return new Vector3(x, _yOffset, z);
            }
            var cur = _notes[_currentIdx];
            var next = _notes[_currentIdx + 1];
            float curTime = (float)cur.timeInSeconds;
            float nextTime = (float)next.timeInSeconds;
            float interval = nextTime - curTime;
            float t = interval > 0.001f ? (songTime - curTime) / interval : 1f;
            t = Mathf.Clamp01(t);
            // 音符间X插值：在外侧轨道内摆动（靠近发声器→回中→靠近下一个发声器）
            // 到达音符时刻贴近外壁(emitterX)，中间回到内边界
            float emitterX = _sideSign * GameConstants.BOT_EMITTER_X;
            float centerX = _sideSign * ((inner + outer) * 0.5f);
            // 使用sin曲线：0→π，在两端贴壁，中间回缩
            float swing = Mathf.Sin(t * Mathf.PI);
            float x2 = Mathf.Lerp(emitterX, centerX, swing);
            return new Vector3(x2, _yOffset, z);
        }

        /// <summary>到达音符时播放和弦音效</summary>
        private void CheckNoteArrival(float songTime)
        {
            if (_currentIdx == _lastPlayedIdx) return;
            float noteTime = (float)_notes[_currentIdx].timeInSeconds;
            if (Mathf.Abs(songTime - noteTime) < 0.05f)
            {
                _lastPlayedIdx = _currentIdx;
                PlayBotTone(_notes[_currentIdx].midiNote);
            }
        }

        private void PlayBotTone(int midiNote)
        {
            if (_sfx == null) return;
            float freq = 220f + midiNote * 3f;
            _sfx.clip = ToneGenerator.CreateTone(freq, 0.05f, 0.2f);
            _sfx.Play();
        }

        private static Color GetTrackColor(TrackType t) => t switch
        {
            TrackType.Drums  => ColorDrums,
            TrackType.Bass   => ColorBass,
            TrackType.Chords => ColorChords,
            _ => Color.white
        };
    }
}
