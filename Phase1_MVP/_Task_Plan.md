# Bug修复任务计划

## 问题列表
1. 节奏混乱：墙体位置未考虑球的碰撞半径
2. 墙体/球体过大：缺乏美感
3. 速度不均匀：MIN_DISTANCE导致短间隔音符被强制拉长距离
4. 重播功能失效：播放结束后无法重新开始

## 修复方案

### Encoder修复 (path_planner.py)
-缩小参数：BALL_SPEED=400, WALL_LENGTH=30, MIN_DISTANCE=30
- 移除MIN_DISTANCE对节奏的干扰，改为固定速度严格按时间计算距离
- 墙体位置偏移球半径，确保碰撞点准确

### Player修复
- renderer.ts: WALL_LENGTH=30, WALL_THICK=3, BALL_RADIUS=5
- main.ts: 修复重播逻辑（重置renderer、audio、时间）
