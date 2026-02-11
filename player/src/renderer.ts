import { Application, Container, Graphics } from 'pixi.js'
import type { MdblData, TimelineEvent, BallKeyframe } from './types'
import { Camera } from './camera'
import { ChildBallManager } from './child-ball-manager'

const WALL_LENGTH = 10
const WALL_THICK = 3
const BALL_RADIUS = 5
const TRAIL_LENGTH = 15
const DENSITY_WINDOW = 2000
const DENSITY_MAX = 15
const ZOOM_MIN = 0.5
const ZOOM_MAX = 1.6
const GLOW_RADIUS = 12
const GLOW_FADE_MS = 600

export class Renderer {
  app: Application
  private scene: Container
  private camera: Camera
  private ball: Graphics
  private wallGraphics: Graphics[] = []
  private glowGraphics: Graphics[] = []
  private trail: Graphics[] = []
  private data: MdblData | null = null
  private activatedSet = new Set<number>()
  private childMgr: ChildBallManager
  private splitterSpawned = new Set<number>()
  onWallHit: ((event: TimelineEvent) => void) | null = null
  onChildWallHit: ((event: TimelineEvent) => void) | null = null

  constructor(canvas: HTMLCanvasElement) {
    this.app = new Application({
      view: canvas, resizeTo: window,
      backgroundColor: 0x0a0a0a, antialias: true,
    })
    this.scene = new Container()
    this.app.stage.addChild(this.scene)
    this.camera = new Camera(this.scene)
    this.camera.resize(this.app.screen.width, this.app.screen.height)
    this.childMgr = new ChildBallManager(this.scene)
    this.ball = new Graphics()
    this.ball.beginFill(0xffffff)
    this.ball.drawCircle(0, 0, BALL_RADIUS)
    this.ball.endFill()
    this.scene.addChild(this.ball)
    window.addEventListener('resize', () => {
      this.camera.resize(this.app.screen.width, this.app.screen.height)
    })
  }

  loadData(data: MdblData) {
    this.data = data
    this.activatedSet.clear()
    this.splitterSpawned.clear()
    this.wallGraphics.forEach(g => g.destroy())
    this.glowGraphics.forEach(g => g.destroy())
    this.wallGraphics = []
    this.glowGraphics = []
    this.trail.forEach(g => g.destroy())
    this.trail = []
    this.childMgr.destroyAll()
    for (const event of data.timeline) {
      const glow = new Graphics()
      glow.beginFill(0xffffff, 0.4)
      if (event.type === 'SPLITTER') {
        const s = GLOW_RADIUS
        glow.moveTo(0, -s); glow.lineTo(s, 0)
        glow.lineTo(0, s); glow.lineTo(-s, 0); glow.closePath()
      } else if (event.type === 'MERGER') {
        glow.drawCircle(0, 0, GLOW_RADIUS)
      } else {
        glow.drawRoundedRect(
          -WALL_LENGTH / 2 - GLOW_RADIUS / 2, -WALL_THICK / 2 - GLOW_RADIUS / 2,
          WALL_LENGTH + GLOW_RADIUS, WALL_THICK + GLOW_RADIUS, GLOW_RADIUS / 2)
      }
      glow.endFill()
      glow.x = event.pos.x; glow.y = event.pos.y
      glow.angle = event.rotation; glow.visible = false
      this.scene.addChild(glow); this.glowGraphics.push(glow)
      const g = new Graphics()
      g.beginFill(0xffffff, 1.0)
      g.drawRect(-WALL_LENGTH / 2, -WALL_THICK / 2, WALL_LENGTH, WALL_THICK)
      g.endFill()
      if (event.type === 'SPLITTER') {
        g.lineStyle(1, 0xFFD700, 0.6)
        const s = WALL_LENGTH / 2 + 2
        g.moveTo(0, -s / 2); g.lineTo(s / 2, 0)
        g.lineTo(0, s / 2); g.lineTo(-s / 2, 0); g.closePath()
      } else if (event.type === 'MERGER') {
        g.lineStyle(1, 0x00FFAA, 0.6)
        g.drawCircle(0, 0, WALL_LENGTH / 2 + 2)
      }
      g.x = event.pos.x; g.y = event.pos.y; g.angle = event.rotation
      g.tint = 0x888888; g.alpha = 0.9
      this.scene.addChild(g); this.wallGraphics.push(g)
    }
    for (let i = 0; i < TRAIL_LENGTH; i++) {
      const t = new Graphics()
      const alpha = 1 - i / TRAIL_LENGTH
      const radius = BALL_RADIUS * (1 - i / TRAIL_LENGTH * 0.6)
      t.beginFill(0x4FC3F7, alpha * 0.5); t.drawCircle(0, 0, radius)
      t.endFill(); t.visible = false
      this.scene.addChild(t); this.trail.push(t)
    }
    this.scene.addChild(this.ball)
    if (data.ballPath.length > 0) {
      this.ball.x = data.ballPath[0].x; this.ball.y = data.ballPath[0].y
      this.camera.snapTo(this.ball.x, this.ball.y)
    }
  }

  update(currentTimeMs: number) {
    if (!this.data) return
    const { ballPath, timeline } = this.data
    const pos = this._interpolateBallPos(ballPath, currentTimeMs)
    for (let i = this.trail.length - 1; i > 0; i--) {
      this.trail[i].x = this.trail[i - 1].x
      this.trail[i].y = this.trail[i - 1].y
      this.trail[i].visible = this.trail[i - 1].visible
    }
    if (this.trail.length > 0) {
      this.trail[0].x = this.ball.x; this.trail[0].y = this.ball.y
      this.trail[0].visible = !this.childMgr.active
    }
    this.ball.x = pos.x; this.ball.y = pos.y
    this.ball.visible = !this.childMgr.active
    for (let i = 0; i < timeline.length; i++) {
      if (this.activatedSet.has(i)) continue
      if (currentTimeMs >= timeline[i].time) {
        this._activateWall(i)
        this.activatedSet.add(i)
        if (this.onWallHit) this.onWallHit(timeline[i])
        const ev = timeline[i]
        if (ev.type === 'SPLITTER' && ev.childPaths && !this.splitterSpawned.has(i)) {
          this.splitterSpawned.add(i)
          this.childMgr.spawn(ev.childPaths, currentTimeMs, this.data!.assets.instruments)
        }
      }
    }
    this.childMgr.update(currentTimeMs)
    // 始终使用正常摄像机跟踪，不因子球缩放
    const zoom = this._calcDensityZoom(currentTimeMs)
    this.camera.setTargetZoom(zoom)
    this.camera.update(this.ball.x, this.ball.y)
  }

  setManualZoomOffset(offset: number) { this.camera.setManualZoomOffset(offset) }

  setChildWallHitCallback(cb: (event: TimelineEvent) => void) {
    this.childMgr.onChildWallHit = cb
  }

  /** 跳转到指定时间：重置状态并快进激活已过墙体 */
  seekTo(timeMs: number) {
    if (!this.data) return
    this.activatedSet.clear()
    this.splitterSpawned.clear()
    this.childMgr.destroyAll()
    this.ball.visible = true
    for (const g of this.wallGraphics) { g.tint = 0x888888; g.alpha = 0.9 }
    for (const glow of this.glowGraphics) { glow.visible = false; glow.alpha = 0 }
    this.trail.forEach(t => (t.visible = false))
    // 快进：标记所有已过时间的墙体为已激活
    const { timeline } = this.data
    for (let i = 0; i < timeline.length; i++) {
      if (timeline[i].time <= timeMs) {
        this.activatedSet.add(i)
        const ev = timeline[i]
        const color = this.data.assets.instruments[ev.instrumentId]?.color || '#4FC3F7'
        const hexColor = parseInt(color.replace('#', ''), 16)
        if (this.wallGraphics[i]) {
          this.wallGraphics[i].tint = hexColor; this.wallGraphics[i].alpha = 1
        }
      }
    }
    // 更新球位置
    const pos = this._interpolateBallPos(this.data.ballPath, timeMs)
    this.ball.x = pos.x; this.ball.y = pos.y
    this.camera.snapTo(pos.x, pos.y)}

  reset() {
    this.activatedSet.clear()
    this.splitterSpawned.clear()
    this.childMgr.destroyAll()
    this.ball.visible = true
    for (const g of this.wallGraphics) { g.tint = 0x888888; g.alpha = 0.9 }
    for (const glow of this.glowGraphics) { glow.visible = false; glow.alpha = 0 }
    this.trail.forEach(t => (t.visible = false))
  }

  private _calcDensityZoom(t: number): number {
    if (!this.data) return 1.0
    const tl = this.data.timeline
    let count = 0
    for (let i = 0; i < tl.length; i++) {
      if (Math.abs(tl[i].time - t) <= DENSITY_WINDOW) count++
    }
    return ZOOM_MAX - (ZOOM_MAX - ZOOM_MIN) * Math.min(count / DENSITY_MAX, 1.0)
  }

  private _interpolateBallPos(path: BallKeyframe[], t: number): { x: number; y: number } {
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
    const ratio = (t - a.time) / (b.time - a.time)
    return { x: a.x + (b.x - a.x) * ratio, y: a.y + (b.y - a.y) * ratio }
  }

  private _activateWall(index: number) {
    const g = this.wallGraphics[index]
    const glow = this.glowGraphics[index]
    if (!g || !this.data) return
    const ev = this.data.timeline[index]
    const color = this.data.assets.instruments[ev.instrumentId]?.color || '#4FC3F7'
    const hexColor = parseInt(color.replace('#', ''), 16)
    g.tint = 0xffffff; g.alpha = 1
    if (glow) {
      glow.tint = hexColor
      glow.alpha = 0.8
      glow.visible = true
      glow.scale.set(1.0)
    }
    let elapsed = 0
    const ticker = this.app.ticker.add((dt: number) => {
      elapsed += dt * (1000 / 60)
      if (elapsed <= 150) {
        g.tint = this._lerpColor(0xffffff, hexColor, elapsed / 150)
      } else { g.tint = hexColor }
      if (glow && elapsed <= GLOW_FADE_MS) {
        const t = elapsed / GLOW_FADE_MS
        glow.alpha = 0.8 * (1 - t); glow.scale.set(1.0 + t * 0.5)
      }
      if (elapsed > GLOW_FADE_MS) {
        g.tint = hexColor; g.alpha = 1
        if (glow) { glow.visible = false; glow.alpha = 0 }
        this.app.ticker.remove(ticker as any)
      }
    })
  }

  private _lerpColor(c1: number, c2: number, t: number): number {
    const r1 = (c1 >> 16) & 0xff, g1 = (c1 >> 8) & 0xff, b1 = c1 & 0xff
    const r2 = (c2 >> 16) & 0xff, g2 = (c2 >> 8) & 0xff, b2 = c2 & 0xff
    const r = Math.round(r1 + (r2 - r1) * t)
    const g = Math.round(g1 + (g2 - g1) * t)
    const b = Math.round(b1 + (b2 - b1) * t)
    return (r << 16) | (g << 8) | b
  }
}
