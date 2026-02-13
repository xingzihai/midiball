// GameConstants.cs — 全局只读常量，所有模块可读
using UnityEngine;

namespace StarPipe.Core
{
    public static class GameConstants
    {
        // 轨道几何
        public const float TRACK_HALF_WIDTH = 5.0f;  // X轴范围[-5, 5]
        public const float EMITTER_MIN_X = 2.5f;     // 发声器最小|X|距离（靠近边缘）
        public const float EMITTER_MAX_X = 4.8f;     // 发声器最大|X|距离

        // 速度参数
        public const float SCROLL_SPEED = 20.0f;     // Z轴滚动速度（增大=发声器间距更远）
        public const float FORWARD_SPEED = 20.0f;
        public const float LATERAL_ACCEL = 60.0f;    // 横向加速度（提高响应速度）

        // 判定
        public const float HIT_RADIUS = 0.5f;
        public const float JUDGE_TOLERANCE = 0.15f;

        // 游戏机制
        public const int COMBO_TO_UNLOCK = 5;
        public const int MISS_TO_PENALTY = 5;

        // MIDI映射：音高 -> X坐标（靠近管道两侧边缘）
        // 低音->左墙(-4.8~-2.5)，高音->右墙(2.5~4.8)
        public static float MidiNoteToX(int midiNote)
        {
            float t = midiNote / 127f; // 0~1
            // 映射到[-EMITTER_MAX_X, -EMITTER_MIN_X] U [EMITTER_MIN_X, EMITTER_MAX_X]
            if (t < 0.5f)
                return Mathf.Lerp(-EMITTER_MAX_X, -EMITTER_MIN_X, t * 2f);
            else
                return Mathf.Lerp(EMITTER_MIN_X, EMITTER_MAX_X, (t - 0.5f) * 2f);
        }
    }
}
