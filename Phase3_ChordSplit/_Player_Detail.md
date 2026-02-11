# Player 多球渲染 - 实现细节

## 概述
Player端需支持SPLITTER处主球分裂为多个子球、子球独立运动撞墙发声、MERGER处汇合恢复单球。

## 模块设计

### 1. types.ts 扩展
```typescript
// SPLITTER事件新增childPaths
interfaceChildPath {
  note: number
  velocity: number
  instrumentId: number
  walls: TimelineEvent[]      // 子球专属墙体
  keyframes: BallKeyframe[]   // 子球路径关键帧
  mergerTime: number          // 汇合时间
}
// TimelineEvent.childPaths?: ChildPath[]
```

### 2. child-ball-manager.ts (新建, ~150行)
职责: 管理子球的完整生命周期。

核心接口:
- `spawn(splitterEvent, currentTime)` — SPLITTER触发时创建子球Graphics
- `update(currentTime)` — 每帧更新所有活跃子球位置(插值)
- `getBoundingBox()` — 返回所有活跃子球+主球的包围盒(用于摄像机)
- `isActive()` — 是否有活跃子球
- `destroy()` — 清理所有子球

子球数据结构:
```typescript
interfaceChildBall {
  graphic: Graphics        // pixi渲染对象
  trail: Graphics[]// 拖尾(5段，比主球短)
  keyframes: BallKeyframe[]
  walls: TimelineEvent[]
  activatedWalls: Set<number>
  mergerTime: number
  color: number            // 乐器颜色
}
```

### 3. renderer.ts 修改
-引入ChildBallManager实例
- `update()` 中:
  - 检测SPLITTER激活 → 调用manager.spawn()
  - 调用manager.update()更新子球位置
  - 子球撞墙检测 → 触发子墙体激活+音频
  - 检测MERGER时间到达 → 销毁子球、主球跳到merger位置
- `_calcDensityZoom()` 改为:有活跃子球时使用包围盒缩放

### 4. camera.ts 修改
- 新增 `updateMultiBall(centerX, centerY, boundingBox)` 方法
- 多球模式: zoom = min(screenW/boxW, screenH/boxH) * 0.8
- 单球模式:保持原有密度缩放逻辑
- 平滑过渡: 分裂/汇合时zoom变化使用lerp

### 5. audio-engine.ts 修改
- 子球撞墙时需要独立触发音符
- 新增 `triggerChildNote(note, velocity, time)` 方法
- 子球音符不预调度，改为实时触发（因为子球路径是动态的）

## 视觉效果
- 子球颜色: 使用对应乐器颜色，半透明
- 子球大小: 主球的70%
- 子球拖尾: 5段（主球15段），颜色与子球一致
- 分裂动画: 主球缩小消失(150ms) + 子球从中心扩散出现
- 汇合动画: 子球向merger点收缩(150ms) + 主球从中心放大出现

## 主球隐藏逻辑
- SPLITTER触发时: 主球visible=false，停止主球拖尾
- MERGER触发时: 主球visible=true，恢复拖尾
- 主球位置在分裂期间仍按ballPath插值（但不渲染）
