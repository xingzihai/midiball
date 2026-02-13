// AutoBotController.cs — 自动球在管道外侧轨道插值飞行
// 碰撞外侧辅助发声器，发出和弦配乐音效
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
        private float _sideSign;
        private Renderer _renderer;
        private AudioSource _sfx;
        private int _lastPlayedIdx = -1;

        private static readonly Color ColorDrums  = new Color(1f, 0.6f, 0f, 0.7f);
        private static readonly Color ColorBass   = new Color(0.2f, 0.5f, 1f, 0.7f);
        private static readonly Color ColorChords = new Color(0.8f, 0.3f, 1f, 0.7f);

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
            switch (track)
            {
                case TrackType.Drums:
                    _sideSign = 1f; _yOffset = 0.3f; break;
                case TrackType.Bass:
                    _sideSign = -1f; _yOffset = 0.3f; break;
                case TrackType.Chords:
                    _sideSign = 1f; _yOffset = 0.8f; break;
                default:
                    _sideSign = 1f; _yOffset = 0.5f; break;
            }
            SetupMaterial(track);
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = 0.4f;
            // 获取conductor（此时GameStateManager已确保其存在）
            if (ServiceLocator.Has<IAudioConductor>())
                _conductor = ServiceLocator.Get<IAudioConductor>();
            Debug.Log($"[AutoBot] {track} 创建 | side={(_sideSign > 0 ? "右" : "左")} " +
                      $"音符={notes.Length} conductor={(_conductor != null ? "OK" : "NULL")}");
        }

        private void SetupMaterial(TrackType track)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null) return;
            var mat = _renderer.material;
            mat.color = GetTrackColor(track);
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
            // 延迟重试获取conductor
            if (_conductor == null)
            {
                if (ServiceLocator.Has<IAudioConductor>())
                    _conductor = ServiceLocator.Get<IAudioConductor>();
                return;
            }
            if (_notes == null || _notes.Length == 0 || !_conductor.IsPlaying) return;

            float songTime = (float)_conductor.SongTime;
            _currentIdx = FindNoteIndex(songTime);
            transform.position = CalcPosition(songTime);
            CheckNoteArrival(songTime);
        }

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

        private Vector3 CalcPosition(float songTime)
        {
            float z = songTime * GameConstants.SCROLL_SPEED;
            float inner = GameConstants.BOT_TRACK_INNER;
            float outer = GameConstants.BOT_TRACK_OUTER;
            if (_currentIdx >= _notes.Length - 1)
            {
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
            float emitterX = _sideSign * GameConstants.BOT_EMITTER_X;
            float centerX = _sideSign * ((inner + outer) * 0.5f);
            float swing = Mathf.Sin(t * Mathf.PI);
            float x2 = Mathf.Lerp(emitterX, centerX, swing);
            return new Vector3(x2, _yOffset, z);
        }

        /// <summary>到达音符时播放音效，窗口放宽到0.15s确保触发</summary>
        private void CheckNoteArrival(float songTime)
        {
            if (_currentIdx == _lastPlayedIdx) return;
            // 只要索引变化就播放（二分查找保证索引跟随songTime前进）
            _lastPlayedIdx = _currentIdx;
            PlayBotTone(_notes[_currentIdx].midiNote);
        }

        private void PlayBotTone(int midiNote)
        {
            if (_sfx == null) return;
            // 不同轨道用不同音色特征
            float baseFreq = Track switch
            {
                TrackType.Drums  => 100f + midiNote * 2f,  // 低沉鼓点
                TrackType.Bass   => 60f + midiNote * 1.5f,  // 深沉贝斯
                TrackType.Chords => 200f + midiNote * 4f,   // 明亮和弦
                _ => 220f + midiNote * 3f
            };
            float dur = Track == TrackType.Drums ? 0.04f : 0.08f;
            _sfx.clip = ToneGenerator.CreateTone(baseFreq, dur, 0.3f);
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
