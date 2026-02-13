// GameConstants.cs — 全局只读常量，所有模块可读
using UnityEngine;

namespace StarPipe.Core
{
    public static class GameConstants
    {
        //轨道几何（缩窄管道，左右发声器更近）
        public const float TRACK_HALF_WIDTH = 3.5f;  // X轴范围[-3.5, 3.5]（原5.0）
        public const float EMITTER_MIN_X = 1.5f;     // 发声器最小|X|距离（原2.5）
        public const float EMITTER_MAX_X = 3.2f;     // 发声器最大|X|距离（原4.8）

        // 速度参数（增大=同侧发声器Z间距更远）
        public const float SCROLL_SPEED = 30.0f;     // Z轴滚动速度（原20）
        public const float FORWARD_SPEED = 30.0f;    // 同步（原20）public const float LATERAL_ACCEL = 60.0f;

        // 判定
        public const float HIT_RADIUS = 0.5f;public const float JUDGE_TOLERANCE = 0.15f;

        // 游戏机制
        public const int COMBO_TO_UNLOCK = 5;
        public const int MISS_TO_PENALTY = 5;

        // MIDI映射：音高 -> X坐标
        public static float MidiNoteToX(int midiNote)
        {
            float t = midiNote / 127f;
            if (t < 0.5f)
                return Mathf.Lerp(-EMITTER_MAX_X, -EMITTER_MIN_X, t * 2f);
            else
                return Mathf.Lerp(EMITTER_MIN_X, EMITTER_MAX_X, (t - 0.5f) * 2f);
        }
    }
}
