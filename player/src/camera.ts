import { Container } from 'pixi.js'
import type { BoundingBox } from './child-ball-manager'

const ZOOM_MIN = 0.2
const ZOOM_MAX = 3.0
const ZOOM_LERP = 0.015       // 缩放插值速度（越小越平滑）
const POS_LERP = 0.08
const ZOOM_DEADZONE = 0.03    // 缩放死区：目标变化<此值时忽略
const ZOOM_EMA_ALPHA = 0.05   // EMA低通滤波系数（越小越平滑）
const BBOX_PADDING = 0.8

export class Camera {
  private container: Container
  private screenW = 0
  private screenH = 0
  private targetX = 0
  private targetY = 0
  private currentScale = 1.0
  private targetScale = 1.0
  private smoothTarget = 1.0// EMA平滑后的目标缩放
  private manualOffset = 0

  constructor(container: Container) {
    this.container = container
  }

  resize(w: number, h: number) {
    this.screenW = w
    this.screenH = h
  }

  /** 设置目标缩放，经EMA低通滤波+死区抑制高频抖动 */
  setTargetZoom(zoom: number) {
    const raw = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, zoom + this.manualOffset))
    // EMA低通滤波：平滑目标值
    this.smoothTarget += (raw - this.smoothTarget) * ZOOM_EMA_ALPHA
    // 死区：仅当变化超过阈值时才更新targetScale
    if (Math.abs(this.smoothTarget - this.targetScale) > ZOOM_DEADZONE) {
      this.targetScale = this.smoothTarget
    }
  }

  setManualZoomOffset(offset: number) {
    this.manualOffset = offset
  }

  /** 单球模式：跟随主球，缩放平滑插值 */
  update(ballX: number, ballY: number) {
    this.targetX = this.screenW / 2 - ballX * this.currentScale
    this.targetY = this.screenH / 2 - ballY * this.currentScale
    this.container.x += (this.targetX - this.container.x) * POS_LERP
    this.container.y += (this.targetY - this.container.y) * POS_LERP
    this.currentScale += (this.targetScale - this.currentScale) * ZOOM_LERP
    this.container.scale.set(this.currentScale)
  }

  /** 多球模式：跟随包围盒中心，自动缩放确保所有球可见 */
  updateMultiBall(bbox: BoundingBox) {
    const zoomX = this.screenW / Math.max(bbox.width, 1)
    const zoomY = this.screenH / Math.max(bbox.height, 1)
    const fitZoom = Math.min(zoomX, zoomY) * BBOX_PADDING + this.manualOffset
    this.targetScale = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, fitZoom))
    this.targetX = this.screenW / 2 - bbox.centerX * this.currentScale
    this.targetY = this.screenH / 2 - bbox.centerY * this.currentScale
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
