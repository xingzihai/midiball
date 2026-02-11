import { Container } from 'pixi.js'

const ZOOM_MIN = 0.2
const ZOOM_MAX = 3.0
const ZOOM_LERP = 0.04
const POS_LERP = 0.08

export class Camera {
  private container: Container
  private screenW = 0
  private screenH = 0
  private targetX = 0
  private targetY = 0
  private currentScale = 1.0
  private targetScale = 1.0
  private manualOffset = 0 // 用户手动缩放偏移

  constructor(container: Container) {
    this.container = container
  }

  resize(w: number, h: number) {
    this.screenW = w
    this.screenH = h
  }

  /** 设置目标缩放值（由renderer根据密度计算后调用） */
  setTargetZoom(zoom: number) {
    const final = zoom + this.manualOffset
    this.targetScale = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, final))
  }

  /** 用户手动调整缩放偏移，范围-1.0~+2.0 */
  setManualZoomOffset(offset: number) {
    this.manualOffset = offset
  }

  /** 每帧调用 */
  update(ballX: number, ballY: number) {
    this.targetX = this.screenW / 2 - ballX * this.currentScale
    this.targetY = this.screenH / 2 - ballY * this.currentScale
    this.container.x += (this.targetX - this.container.x) * POS_LERP
    this.container.y += (this.targetY - this.container.y) * POS_LERP
    this.currentScale += (this.targetScale - this.currentScale) * ZOOM_LERP
    this.container.scale.set(this.currentScale)
  }

  snapTo(ballX: number, ballY: number) {
    this.container.x = this.screenW / 2 - ballX * this.currentScale
    this.container.y = this.screenH / 2 - ballY * this.currentScale
  }
}
