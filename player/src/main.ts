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

const canvas = document.getElementById('canvas') as HTMLCanvasElement
const fileInput = document.getElementById('file-input') as HTMLInputElement
const playBtn = document.getElementById('play-btn') as HTMLButtonElement
const timeDisplay = document.getElementById('time-display') as HTMLSpanElement
const zoomSlider = document.getElementById('zoom-slider') as HTMLInputElement
const zoomValue = document.getElementById('zoom-value') as HTMLSpanElement

renderer = new Renderer(canvas)
audio = new AudioEngine()

// 缩放滑条
zoomSlider.addEventListener('input', () => {
  const offset = parseFloat(zoomSlider.value)
  renderer.setManualZoomOffset(offset)
  zoomValue.textContent = (1.0 + offset).toFixed(1) + 'x'
})

// 文件上传
fileInput.addEventListener('change', async () => {
  const file = fileInput.files?.[0]
  if (!file) return
  try {
    data = await loadMdblFromFile(file)
    renderer.loadData(data)
    audio.scheduleAll(data)
    playBtn.disabled = false
    playBtn.textContent = '▶ 播放'
    playing = false
    needsRestart = false
    timeDisplay.textContent = '0:00'
  } catch (e) {
    alert('加载失败: ' + (e as Error).message)
  }
})

// 播放/暂停/重播
playBtn.addEventListener('click', async () => {
  if (!data) return
  if (needsRestart) {
    audio.stop()
    renderer.reset()
    renderer.loadData(data)
    audio.scheduleAll(data)
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
    const sec = Math.floor(t / 1000)
    const min = Math.floor(sec / 60)
    timeDisplay.textContent = `${min}:${String(sec % 60).padStart(2, '0')}`
    if (t >= data.meta.totalTime) {
      playing = false
      needsRestart = true
      audio.stop()
      playBtn.textContent = '▶ 重播'
      return
    }
    animFrameId = requestAnimationFrame(tick)
  }
  tick()
}
