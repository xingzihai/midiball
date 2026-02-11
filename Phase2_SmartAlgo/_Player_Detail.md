# 阶段二 Player 细节规划

## 1. camera.ts（增强，约70行）

### 1.1 新增功能：动态缩放
- 新增属性：`scale`, `targetScale`, `zoomLerp`
- 常量：`ZOOM_MIN=0.4`, `ZOOM_MAX=1.8`, `ZOOM_LERP=0.04`

### 1.2 缩放逻辑
- `setTargetZoom(zoom: number)` — 由renderer根据当前播放密度调用
- `update()` 中增加scale的lerp插值：`scale += (targetScale - scale) * zoomLerp`
- 将scale应用到container的scale属性

### 1.3 缩放触发条件（由renderer计算传入）
- 基于当前时间前后2秒窗口内的墙体数量
- 墙体密集→ zoom缩小（看全局）；墙体稀疏 → zoom放大（看细节）
- 映射：`zoom = ZOOM_MAX - (ZOOM_MAX - ZOOM_MIN) * density_ratio`

## 2. renderer.ts（小改，增加约30行）

### 2.1 新增方法
- `_calcDensityZoom(currentTimeMs: number)` → number
  - 统计当前时间±2000ms窗口内的timeline事件数
  - 映射到zoom值，传给camera.setTargetZoom()

### 2.2 修改点
- `update()` 中调用 `_calcDensityZoom()` 并传给camera
- camera.update() 调用签名不变，内部自动应用zoom

## 3. 其余文件无需修改
- types.ts：现有接口已兼容
- audio-engine.ts：不涉及
- main.ts：不涉及
- mdbl-loader.ts：不涉及
