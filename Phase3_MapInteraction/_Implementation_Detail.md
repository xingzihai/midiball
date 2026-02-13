# 阶段三：细节实现规划（已更新）

## 3A: MapGenerator —薄墙式发声器

### MapGenerator.cs（Map/下）
- MonoBehaviour，挂载到场景空对象
- Start时通过ServiceLocator获取IAudioConductor，读取SongData
- 发声器形态：**薄长墙壁**，贴在管道两侧- Scale: `(0.15, 1.5, 5.0)` — 极薄(X)、高(Y)、沿Z轴延伸(长)
  - 位置：紧贴管道边缘，`CalcWallX()` 计算X坐标
  - Y=0.75（底部贴地）
- 对象池：预创建1000个Cube，Queue管理
  - BoxCollider保留，设为isTrigger=true
  - 超出视野自动回收复用
- 音符侧面分配：低音→左墙，高音→右墙（由MidiNoteToX决定）

### NoteMarker.cs（Map/下，碰撞+数据组件）
- 存储：noteIndex, isJudged, isActive, isRightSide
- **碰撞反弹（双重检测）**：
  - OnTriggerEnter：物理Trigger回调（需Player有Rigidbody+SphereCollider）
  - ManualCollisionCheck：手动AABB检测（防高速穿透）
- DoBounce()：反弹方向由isRightSide决定（右墙→向左弹，左墙→向右弹）
- 碰撞后调用NoteJudge.Instance.NotifyHit()触发音效和事件
- SetHit()/SetMiss()改变颜色（绿/红）

## 3B: NoteJudge — 碰撞驱动判定

### NoteJudge.cs（Gameplay/下）
- 单例模式（NoteJudge.Instance）
- Hit判定：由NoteMarker碰撞触发NotifyHit()
- Miss判定：每帧检查超时未碰撞的音符（songTime > noteTime + tolerance）
- 音效：Hit/Miss分别播放不同频率的ToneGenerator音调
- 事件：EventBus.OnNoteHit / OnNoteMiss / OnComboChanged

## 3C: PlayerController — 碰撞组件

### PlayerController.cs（Gameplay/下）
- Z轴：Rigidbody.MovePosition()驱动（确保物理碰撞检测）
- X轴：输入加速+阻尼+边界反弹
- 碰撞组件（运行时自动添加）：
  - SphereCollider(radius=0.4, isTrigger=false)
  - Rigidbody(isKinematic=true, interpolation=Interpolate)
  - Tag="Player"
- CheckManualCollisions()：每帧遍历ActiveMarkers做AABB后备检测
- ApplyLateralImpulse()：供NoteMarker碰撞时调用

## 3D: 场景配置

### Phase3SceneSetup.cs（Editor/下）
- 菜单：StarPipe → Setup Phase3 Scene
- 强制通过SerializedObject覆盖noteScale为(0.15, 1.5, 5.0)
- 配置Player碰撞组件（SphereCollider+Rigidbody+Tag）
- 创建MapGenerator、NoteJudge、CameraFollow
