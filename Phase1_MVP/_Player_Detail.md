# Player 细节实现规划 (MVP阶段)

## 1. types.ts - 类型定义

### 职责
定义.mdbl数据结构的TypeScript类型，供所有模块共享。

### 核心类型
- `MdblData`：顶层结构（meta, assets, timeline, ballPath）
- `Meta`：元信息（title, bpm, totalTime, mapBounds）
- `TimelineEvent`：时间轴事件（WALL类型，含pos, rotation, note, velocity）
- `BallKeyframe`：小球关键帧（time, x, y）
- `Instrument`：乐器定义（id, name, color）

---

## 2. mdbl-loader.ts - 数据加载模块

### 职责
加载并校验.mdbl JSON文件。

### 实现要点
- 通过 `fetch` 或 `<input type="file">` 获取文件内容
- JSON.parse 解析
- 基础字段校验（meta/timeline/ballPath是否存在）
- 返回类型化的`MdblData` 对象

---

## 3. renderer.ts - Pixi.js渲染引擎

### 职责
管理Pixi.js Application，渲染墙体和小球。

### 核心组件
- **Pixi.Application**：初始化Canvas，全屏自适应
- **墙体容器 (wallContainer)**：存放所有墙体Graphics
- **小球 (ballSprite)**：圆形Graphics，带简单拖尾

### 渲染逻辑
1. 初始化时，根据timeline创建所有墙体Graphics（初始状态：半透明灰色）
2. 每帧（ticker回调）：
   a. 根据当前播放时间，在ballPath中找到前后两个关键帧
   b. 线性插值计算小球当前位置
   c. 更新小球Graphics位置
   d. 检查是否有墙体被"激活"（当前时间 >= 墙体time），更新颜色为高亮
   e. 调用camera模块更新视口

###墙体绘制
- 每个墙体是一个矩形（宽=WALL_LENGTH, 高=4px）
- 以pos为中心，按rotation旋转
- 状态：未激活(灰色半透明) → 激活瞬间(白色) → 已激活(乐器色)

---

## 4. camera.ts - 摄像机系统

### 职责
控制Pixi.js容器的位移和缩放，实现跟随效果。

### MVP实现
- 将整个场景容器作为摄像机目标
- 每帧将容器平移，使小球始终位于屏幕中心
- 公式：`container.x = screenWidth/2 - ball.x * scale`
- MVP阶段固定缩放比例（scale=1），不做动态缩放
- 平滑跟随：使用lerp插值避免突兀跳动

---

## 5. audio-engine.ts - 音频引擎

### 职责
使用Tone.js在精确时间点播放对应音高的声音。

### 实现要点
- 初始化 `Tone.Synth`（MVP用简单合成器即可）
- 播放开始时，根据timeline预调度所有音符：
  - 使用 `Tone.Transport.schedule()` 在每个墙体的time时刻触发
  - 将MIDI note转换为频率：`Tone.Frequency(note, "midi")`
  - velocity映射为音量
- 使用 `Tone.Transport` 控制播放/暂停
- 关键：不用setTimeout，完全依赖Tone.js的Transport调度保证精度

### MIDI音高转频率
Tone.js内置支持：`Tone.Frequency(60, "midi").toNote()` → "C4"

---

## 6. main.ts - 应用入口

### 职责
初始化各模块，串联数据流，提供基础UI交互。

### 流程
1. 页面加载后初始化Pixi.js Application
2. 提供文件上传按钮（或默认加载demo.mdbl）
3. 加载.mdbl文件 → 传递给renderer初始化场景
4. 用户点击"播放"按钮：
   a. 启动Tone.Transport
   b. 启动渲染循环的时间推进
5. 每帧同步：渲染时间 = Tone.Transport当前时间

### UI元素（MVP极简）
- 文件上传按钮
- 播放/暂停按钮
- 当前时间显示

---

## 7. index.html - 页面结构

### 要点
- 全屏Canvas容器
- 顶部简单控制栏（上传、播放按钮）
- 引入Vite构建的JS bundle
