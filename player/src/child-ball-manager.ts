import { Container, Graphics } from 'pixi.js'
import type { ChildPath, BallKeyframe, TimelineEvent } from './types'

const CHILD_RADIUS = 3.5  // 主球70%
const CHILD_TRAIL = 5
const SPAWN_DURATION = 150// 分裂动画时长ms

/** 单个子球实例 */
interface ChildBall {
  graphic: Graphics
  trail: Graphics[]
  keyframes: BallKeyframe[]
  walls: TimelineEvent[]
  activatedWalls: Set<number>
  mergerTime: number
  color: number
  spawnTime: number
}

/** 包围盒 */
export interface BoundingBox {
  minX: number; maxX: number; minY: number; maxY: number
  centerX: number; centerY: number; width: number; height: number
}

/** 子球生命周期管理器 */
export class ChildBallManager {
  private scene: Container
  private children: ChildBall[] = []
  /** 子球撞墙回调 */
  onChildWallHit: ((event: TimelineEvent) => void) | null = null

  constructor(scene: Container) { this.scene = scene }

  get active(): boolean { return this.children.length > 0 }

  /** SPLITTER触发时创建子球 */
  spawn(childPaths: ChildPath[], currentTime: number, instruments: { color: string }[]) {
    for (const cp of childPaths) {
      const color = parseInt(
        (instruments[cp.instrumentId]?.color || '#4FC3F7').replace('#', ''), 16)
      const g = new Graphics()
      g.beginFill(color, 0.85); g.drawCircle(0, 0, CHILD_RADIUS); g.endFill()
      g.scale.set(0.01) // 分裂动画起始
      this.scene.addChild(g)
      //拖尾
      const trail: Graphics[] = []
      for (let i = 0; i < CHILD_TRAIL; i++) {
        const t = new Graphics()
        const a = 1 - i / CHILD_TRAIL
        t.beginFill(color, a * 0.4)
        t.drawCircle(0, 0, CHILD_RADIUS * (1 - i / CHILD_TRAIL * 0.5))
        t.endFill(); t.visible = false
        this.scene.addChild(t); trail.push(t)
      }
      // 初始位置
      if (cp.keyframes.length > 0) {
        g.x = cp.keyframes[0].x; g.y = cp.keyframes[0].y
      }
      this.children.push({
        graphic: g, trail, keyframes: cp.keyframes,
        walls: cp.walls, activatedWalls: new Set(),
        mergerTime: cp.mergerTime, color, spawnTime: currentTime
      })
    }
  }

  /** 每帧更新所有子球 */
  update(currentTime: number) {
    const toRemove: number[] = []
    for (let i = 0; i < this.children.length; i++) {
      const cb = this.children[i]
      // 分裂动画：缩放从0→1
      const spawnElapsed = currentTime - cb.spawnTime
      if (spawnElapsed < SPAWN_DURATION) {
        cb.graphic.scale.set(spawnElapsed / SPAWN_DURATION)
      } else {
        cb.graphic.scale.set(1)
      }
      // 插值位置
      const pos = this._interpolate(cb.keyframes, currentTime)
      // 更新拖尾
      for (let t = cb.trail.length - 1; t > 0; t--) {
        cb.trail[t].x = cb.trail[t - 1].x
        cb.trail[t].y = cb.trail[t - 1].y
        cb.trail[t].visible = cb.trail[t - 1].visible
      }
      if (cb.trail.length > 0) {
        cb.trail[0].x = cb.graphic.x; cb.trail[0].y = cb.graphic.y
        cb.trail[0].visible = true
      }
      cb.graphic.x = pos.x; cb.graphic.y = pos.y
      // 子墙体撞击检测
      for (let w =0; w < cb.walls.length; w++) {
        if (cb.activatedWalls.has(w)) continue
        if (currentTime >= cb.walls[w].time) {
          cb.activatedWalls.add(w)
          if (this.onChildWallHit) this.onChildWallHit(cb.walls[w])
        }
      }
      // 到达merger时间则标记移除
      if (currentTime >= cb.mergerTime) {
        //汇合收缩动画
        const overTime = currentTime - cb.mergerTime
        if (overTime < SPAWN_DURATION) {
          cb.graphic.scale.set(1 - overTime / SPAWN_DURATION)
        } else {
          toRemove.push(i)
        }
      }
    }
    // 移除已汇合的子球（倒序）
    for (let i = toRemove.length - 1; i >= 0; i--) {
      this._destroyChild(toRemove[i])
    }
  }

  /** 获取所有活跃子球+主球的包围盒 */
  getBoundingBox(mainX: number, mainY: number): BoundingBox {
    let minX = mainX, maxX = mainX, minY = mainY, maxY = mainY
    for (const cb of this.children) {
      minX = Math.min(minX, cb.graphic.x)
      maxX = Math.max(maxX, cb.graphic.x)
      minY = Math.min(minY, cb.graphic.y)
      maxY = Math.max(maxY, cb.graphic.y)
    }
    const pad = 40// 边距
    return {
      minX: minX - pad, maxX: maxX + pad,
      minY: minY - pad, maxY: maxY + pad,
      centerX: (minX + maxX) / 2, centerY: (minY + maxY) / 2,
      width: maxX - minX + pad * 2, height: maxY - minY + pad * 2
    }
  }

  /** 清理所有子球 */
  destroyAll() {
    while (this.children.length > 0) this._destroyChild(0)
  }

  private _destroyChild(index: number) {
    const cb = this.children[index]
    cb.graphic.destroy()
    cb.trail.forEach(t => t.destroy())
    this.children.splice(index, 1)
  }

  private _interpolate(path: BallKeyframe[], t: number): { x: number; y: number } {
    if (path.length === 0) return { x: 0, y: 0 }
    if (t <= path[0].time) return { x: path[0].x, y: path[0].y }
    if (t >= path[path.length - 1].time) {
      const last = path[path.length - 1]; return { x: last.x, y: last.y }
    }
    let lo = 0, hi = path.length - 1
    while (lo < hi - 1) {
      const mid = (lo + hi) >> 1
      if (path[mid].time <= t) lo = mid; else hi = mid
    }
    const a = path[lo], b = path[hi]
    const r = (t - a.time) / (b.time - a.time)
    return { x: a.x + (b.x - a.x) * r, y: a.y + (b.y - a.y) * r }
  }
}
