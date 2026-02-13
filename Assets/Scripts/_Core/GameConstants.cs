// GameConstants.cs — 全局只读常量，所有模块可读
using UnityEngine;

namespace StarPipe.Core
{
    public static class GameConstants
    {
        // 轨道几何
        public const float TRACK_HALF_WIDTH = 5.0f;  // X轴范围[-5, 5]

        // 速度参数（待调参）
        public const float SCROLL_SPEED = 10.0f;     // Z轴滚动速度
        public const float FORWARD_SPEED = 10.0f;    // 玩家前进速度
        public const float LATERAL_ACCEL = 30.0f;    // 横向加速度

        // 判定
        public const float HIT_RADIUS = 0.5f;// 音符判定半径
        public const float JUDGE_TOLERANCE = 0.15f;// 判定时间窗口（秒，前后150ms）

        // 游戏机制
        public const int COMBO_TO_UNLOCK = 5;         // 召唤所需连续命中
        public const int MISS_TO_PENALTY = 5;// 惩罚触发连续失误数

        // MIDI映射：音高 -> X坐标
        // midiNote范围[0,127] -> x范围[-5,5]
        public static float MidiNoteToX(int midiNote)
        {
            return Mathf.Lerp(-TRACK_HALF_WIDTH, TRACK_HALF_WIDTH, midiNote / 127f);
        }
    }
}
