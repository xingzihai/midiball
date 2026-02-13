# 阶段三：细节实现规划

## 3A: MapGenerator

### MapGenerator.cs（Map/下）
- MonoBehaviour，挂载到场景空对象
- Start时通过ServiceLocator获取IAudioConductor，读取SongData
- 生成逻辑：
  - 遍历SongData.allNotes（仅Melody轨道，阶段三先只处理主旋律）
  - 每个音符 -> 实例化一个Cube占位符
  - 位置：`(noteData.xPosition,0.5, noteData.timeInSeconds * SCROLL_SPEED)`
  -缩放：`(0.8, 0.8, 0.3)` 扁平方块
- 对象池：
  - 预创建池（poolSize由音符数决定，上限200）
  - 超出视野的方块回收复用
  - 简单队列实现：Queue<GameObject>
- 方块标识：
  - 每个方块挂载轻量NoteMarker组件，存储对应NoteData索引
-颜色区分：
  - 默认灰色，被命中后变绿色，Miss后变红色

### NoteMarker.cs（Map/下，轻量数据组件）
- 存储：noteIndex, isJudged标记
- 提供SetHit()/SetMiss()方法改变颜色

## 3B: NoteJudge

### NoteJudge.cs（Gameplay/下）
- MonoBehaviour，独立判定系统
- 依赖：IAudioConductor（songTime）、PlayerController（玩家位置）
- 核心算法：
  - 维护 `nextNoteIndex` 指针，指向下一个待判定音符
  - 每帧检查：当songTime >= note.timeInSeconds - tolerance时进入判定窗口
  - 判定窗口内：检查玩家X与音符X的距离
  - `distance <= HIT_RADIUS` -> Hit
  - songTime > note.timeInSeconds + tolerance -> Miss（超时未命中）
- 判定容差：`JUDGE_TOLERANCE = 0.15s`（前后150ms窗口）
- 事件触发：
  - Hit: `EventBus.OnNoteHit(noteType, position)`
  - Miss: `EventBus.OnNoteMiss(position)`
- 连击计数：内部维护combo，通过EventBus.OnComboChanged广播

## 3C: 集成验证

### 场景配置更新
- 更新Phase3SceneSetup编辑器脚本
- 新增MapGenerator空对象
- 新增NoteJudge空对象（或挂载到Player上）
- 验证项：
  1. Play后前方出现方块阵列
  2. 球体前进经过方块时，接近的变绿（Hit），错过的变红（Miss）
  3. Console输出Hit/Miss + combo信息
