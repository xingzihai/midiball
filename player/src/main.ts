import './style.css'
import { Renderer } from './renderer'
import { AudioEngine } from './audio-engine'
import { loadMdblFromFile } from './mdbl-loader'
import type { MdblData } from './types'

let renderer: Renderer
let audio: AudioEngine
let data: MdblData | null = null
let playing = false
let animFrameId = 0
let needsRestart = false
let isSeeking = false

const canvas = document.getElementById('canvas') as HTMLCanvasElement
const fileInput = document.getElementById('file-input') as HTMLInputElement
const playBtn = document.getElementById('play-btn') as HTMLButtonElement
const timeDisplay = document.getElementById('time-display') as HTMLSpanElement
const durationDisplay = document.getElementById('duration-display') as HTMLSpanElement
const seekBar = document.getElementById('seek-bar') as HTMLInputElement
const zoomSlider = document.getElementById('zoom-slider') as HTMLInputElement
const zoomValue = document.getElementById('zoom-value') as HTMLSpanElement

renderer = new Renderer(canvas)
audio = new AudioEngine()

renderer.setChildWallHitCallback((event) => {
  audio.triggerChildNote(event.note, event.velocity)
})

zoomSlider.addEventListener('input', () => {
  const offset = parseFloat(zoomSlider.value)
  renderer.setManualZoomOffset(offset)
  zoomValue.textContent = (1.0 + offset).toFixed(1) + 'x'
})

// 进度条拖动
seekBar.addEventListener('mousedown', () => { isSeeking = true })
seekBar.addEventListener('touchstart', () => { isSeeking = true })
seekBar.addEventListener('input', () => {
  if (!data) return
  const seekMs = (parseFloat(seekBar.value) / 1000) * data.meta.totalTime
  timeDisplay.textContent = _formatTime(seekMs)
  renderer.seekTo(seekMs)
})
seekBar.addEventListener('change', () => {
  if (!data) return
  isSeeking = false
  const seekMs = (parseFloat(seekBar.value) / 1000) * data.meta.totalTime
  audio.seekTo(seekMs)
  renderer.seekTo(seekMs)
  needsRestart = false
  if (!playing) {
    playBtn.textContent = '▶ 继续'
  }
})
seekBar.addEventListener('mouseup', () => { isSeeking = false })
seekBar.addEventListener('touchend', () => { isSeeking = false })

fileInput.addEventListener('change', async () => {
  const file = fileInput.files?.[0]
  if (!file) return
  try {
    data = await loadMdblFromFile(file)
    renderer.loadData(data)
    audio.scheduleAll(data)
    playBtn.disabled = false
    seekBar.disabled = false
    playBtn.textContent = '▶ 播放'
    playing = false
    needsRestart = false
    timeDisplay.textContent = '0:00'
    durationDisplay.textContent = _formatTime(data.meta.totalTime)
    seekBar.value = '0'
  } catch (e) {
    alert('加载失败: ' + (e as Error).message)
  }
})

playBtn.addEventListener('click', async () => {
  if (!data) return
  if (needsRestart) {
    audio.stop()
    renderer.reset()
    renderer.loadData(data)
    audio.scheduleAll(data)
    seekBar.value = '0'
    needsRestart = false
  }
  if (!playing) {
    await audio.start()
    playing = true
    playBtn.textContent = '⏸ 暂停'
    startRenderLoop()
  } else {
    audio.pause()
    playing = false
    playBtn.textContent = '▶ 继续'
    cancelAnimationFrame(animFrameId)
  }
})

function startRenderLoop() {
  function tick() {
    if (!playing || !data) return
    const t = audio.getCurrentTimeMs()
    renderer.update(t)
    timeDisplay.textContent = _formatTime(t)
    if (!isSeeking) {
      seekBar.value = String((t / data.meta.totalTime) * 1000)
    }
    if (t >= data.meta.totalTime) {
      playing = false
      needsRestart = true
      audio.stop()
      playBtn.textContent = '▶ 重播'
      seekBar.value = '1000'
      return
    }
    animFrameId = requestAnimationFrame(tick)
  }
  tick()
}

function _formatTime(ms: number): string {
  const sec = Math.floor(ms / 1000)
  const min = Math.floor(sec / 60)
  return `${min}:${String(sec % 60).padStart(2, '0')}`
}
