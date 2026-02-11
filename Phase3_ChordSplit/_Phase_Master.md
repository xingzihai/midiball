# 阶段三：复杂拓扑 - "和弦分裂" 总规划

## 目标
在SPLITTER处将主球分裂为N个子球（N=和弦音符数），每个子球沿独立路径飞行到各自的音符墙，最终所有子球在MERGER点汇合，恢复为单球继续运行。

## 模块划分

### 1. Encoder端改造
- **新增模块**: `encoder/child_path_planner.py` — 子球路径规划器- 输入: SPLITTER位置、和弦音符列表、MERGER位置
  - 输出: 每个子球的独立路径关键帧 + 子墙体列表
  - 核心算法: 扇形展开 → 各自飞行 → 扇形收束汇合
- **修改模块**: `encoder/path_planner.py` — 集成子球路径调用
  - SPLITTER处调用child_path_planner生成子路径
  - 子墙体加入主grid防碰撞
- **修改模块**: `encoder/mdbl_writer.py` — 输出childPaths字段
- **修改模块**: `encoder/main.py` — 日志输出子球统计

### 2. Player端改造
- **修改模块**: `player/src/types.ts` — 增加childPaths类型定义
- **新增模块**: `player/src/child-ball-manager.ts` — 子球生命周期管理
  - 管理子球的创建、插值运动、销毁
  - 提供当前所有活跃球的包围盒信息
- **修改模块**: `player/src/renderer.ts` — 集成子球渲染
  - SPLITTER触发时创建子球
  - MERGER触发时销毁子球、恢复主球
- **修改模块**: `player/src/camera.ts` — 包围盒自动缩放- 多球模式下计算包围盒，自动缩小zoom确保所有球可见

### 3. .mdbl格式扩展
SPLITTER事件新增`childPaths`字段：
```json
{
  "type": "SPLITTER",
  "childPaths": [
    {
      "note": 64,
      "velocity": 75,
      "instrumentId": 0,
      "walls": [
        {"id": 10001, "time": 1600, "pos": {"x": 320, "y": 420}, "rotation": 45, "note": 64, "velocity": 75}
      ],
      "keyframes": [
        {"time": 1500, "x": 300, "y": 400},
        {"time": 1600, "x": 320, "y": 420},
        {"time": 2000, "x": 350, "y": 380}
      ],
      "mergerTime": 2000
    }
  ]
}
```

## 技术选型
- 子球路径算法: 扇形角度分配 + 独立碰撞检测 + 强制汇合约束
- 子墙体ID: 从10000开始编号，避免与主墙体冲突
- 子球渲染: 与主球相同的Graphics对象池，颜色区分

## 目录结构
```
Phase3_ChordSplit/
├── _Phase_Master.md          (本文件)
├── _Encoder_Detail.md        (Encoder子球路径规划细节)
├── _Player_Detail.md         (Player多球渲染细节)
└── _Test_Plan.md             (测试计划)
```

## 风险点
1. 子球路径互相碰撞 — 需要子球间也做碰撞检测
2. MERGER汇合精度 — 需要反向约束计算
3. 子墙体与主墙体碰撞 — 子墙体需加入主grid
4. 性能 — 大量和弦组可能导致子墙体数量爆炸

## 交付物
能够完美演绎复杂钢琴曲的版本：子球在SPLITTER处分裂、各自飞行撞墙发声、在MERGER处汇合。
