// GameStateManager.cs — 召唤/惩罚状态机 + 分数管理
// 监听EventBus事件，管理AutoBot生命周期和音轨Mute/Unmute
using System.Collections.Generic;
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Gameplay
{
    public class GameStateManager : MonoBehaviour
    {
        // 解锁顺序：Drums → Bass → Chords（Melody由玩家控制）
        private static readonly TrackType[] UnlockOrder = {
            TrackType.Drums, TrackType.Bass, TrackType.Chords
        };

        private List<TrackType> _unlockedTracks = new List<TrackType>();
        private Dictionary<TrackType, AutoBotController> _autoBots = new();
        private IAudioConductor _conductor;
        private Transform _botParent;
        private int _consecutiveMisses;
        private int _score;
        private int _lastCombo;
        private bool _initialized;

        void OnEnable()
        {
            EventBus.OnComboChanged += HandleComboChanged;
            EventBus.OnNoteHit += HandleNoteHit;
            EventBus.OnNoteMiss += HandleNoteMiss;}

        void OnDisable()
        {
            EventBus.OnComboChanged -= HandleComboChanged;
            EventBus.OnNoteHit -= HandleNoteHit;
            EventBus.OnNoteMiss -= HandleNoteMiss;
        }

        void Update()
        {
            if (!_initialized) TryInit();
        }

        private void TryInit()
        {
            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            if (_conductor?.CurrentSongData == null) return;
            _botParent = new GameObject("AutoBots").transform;
            _botParent.SetParent(transform);
            // 初始状态：所有伴奏轨道静音
            foreach (var t in UnlockOrder) _conductor.MuteTrack(t);
            _initialized = true;Debug.Log("[GameStateManager] 初始化完成 | 伴奏轨道已全部静音");
        }

        // --- 事件回调 ---

        private void HandleComboChanged(int combo)
        {
            _lastCombo = combo;
            if (combo >= GameConstants.COMBO_TO_UNLOCK) TrySummon();
        }

        private void HandleNoteHit(NoteType type, Vector3 pos)
        {
            _consecutiveMisses = 0; // Hit重置连续Miss
            int multiplier = 1 + _unlockedTracks.Count;
            _score += 100 * multiplier;
            EventBus.OnScoreChanged?.Invoke(_score);
        }

        private void HandleNoteMiss(Vector3 pos)
        {
            _consecutiveMisses++;
            if (_consecutiveMisses >= GameConstants.MISS_TO_PENALTY)TryPenalty();
        }

        // --- 召唤逻辑 ---

        private void TrySummon()
        {
            if (_conductor == null) return;
            // 找到下一个未解锁的轨道
            TrackType? next = GetNextUnlockTrack();
            if (next == null) return; // 全部已解锁

            TrackType track = next.Value;
            NoteData[] trackNotes = GetTrackNotes(track);
            if (trackNotes == null || trackNotes.Length == 0)
            {
                Debug.LogWarning($"[GameStateManager] {track} 无音符数据，跳过召唤");
                return;
            }
            // 创建自动球
            var bot = AutoBotController.Create(track, trackNotes, _botParent);
            _autoBots[track] = bot;
            _unlockedTracks.Add(track);
            // 解除音轨静音
            _conductor.UnmuteTrack(track);
            EventBus.OnTrackUnlocked?.Invoke(track);
            Debug.Log($"[GameStateManager] ★ 召唤 {track} | 已解锁={_unlockedTracks.Count}/3");
        }

        // --- 惩罚逻辑 ---

        private void TryPenalty()
        {
            if (_unlockedTracks.Count == 0) return;
            // 逆序剥离：最后解锁的先被销毁
            TrackType last = _unlockedTracks[_unlockedTracks.Count - 1];
            if (_autoBots.TryGetValue(last, out var bot))
            {
                Destroy(bot.gameObject);
                _autoBots.Remove(last);
            }
            _unlockedTracks.Remove(last);
            _conductor?.MuteTrack(last);
            EventBus.OnTrackLost?.Invoke(last);
            _consecutiveMisses = 0; // 惩罚后重置
            Debug.Log($"[GameStateManager] ✖ 惩罚 {last} | 剩余={_unlockedTracks.Count}/3");
        }

        // --- 辅助方法 ---

        private TrackType? GetNextUnlockTrack()
        {
            foreach (var t in UnlockOrder)
                if (!_unlockedTracks.Contains(t)) return t;
            return null;
        }

        private NoteData[] GetTrackNotes(TrackType track)
        {
            var data = _conductor.CurrentSongData;
            if (data == null) return null;
            return track switch
            {
                TrackType.Drums  => data.drumsNotes,
                TrackType.Bass   => data.bassNotes,
                TrackType.Chords => data.chordsNotes,
                _ => null
            };
        }
    }
}
