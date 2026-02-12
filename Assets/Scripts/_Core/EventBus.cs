// EventBus.cs — 静态事件总线，模块间唯一通信渠道
using System;
using UnityEngine;

namespace StarPipe.Core
{
    //枚举定义（供全局使用）
    public enum TrackType { Melody, Drums, Bass, Chords }
    public enum NoteType { Normal, Special }
    public enum GameState { Loading, Playing, Paused, GameOver }

    public static class EventBus
    {
        // --- 音符判定事件 ---
        public static Action<NoteType, Vector3> OnNoteHit;
        public static Action<Vector3> OnNoteMiss;

        // --- 音轨状态事件 ---
        public static Action<TrackType> OnTrackUnlocked;
        public static Action<TrackType> OnTrackLost;

        // --- 游戏状态事件 ---
        public static Action<int> OnComboChanged;
        public static Action<GameState> OnGameStateChanged;

        // --- 分数事件 ---
        public static Action<int> OnScoreChanged;

        /// <summary>
        /// 场景切换时清理所有订阅，防止内存泄漏
        /// </summary>
        public static void Clear()
        {
            OnNoteHit = null;
            OnNoteMiss = null;
            OnTrackUnlocked = null;
            OnTrackLost = null;
            OnComboChanged = null;
            OnGameStateChanged = null;
            OnScoreChanged = null;
        }
    }
}
