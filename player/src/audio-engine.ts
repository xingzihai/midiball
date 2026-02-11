import * as Tone from 'tone'
import type { MdblData, TimelineEvent } from './types'

/** 音频引擎 - 使用Tone.js精确调度音符播放 */
export class AudioEngine {
  private synth: Tone.PolySynth
  private scheduled = false

  constructor() {
    this.synth = new Tone.PolySynth(Tone.Synth, {
      oscillator: { type: 'triangle' },
      envelope: { attack: 0.01, decay: 0.3, sustain: 0.1, release: 0.5 },
    })
    this.synth.toDestination()
    this.synth.volume.value = -6
  }

  /** 预调度所有音符到Transport时间轴 */
  scheduleAll(data: MdblData) {
    Tone.getTransport().cancel()
    for (const event of data.timeline) {
      const timeSec = event.time / 1000
      Tone.getTransport().schedule((time: number) => {
        this._playNote(event, time)
      }, timeSec)
    }
    this.scheduled = true
  }

  /** 实时触发单个音符(备用方案) */
  triggerNote(event: TimelineEvent) {
    const noteName = Tone.Frequency(event.note, 'midi').toNote()
    const vel = event.velocity / 127
    this.synth.triggerAttackRelease(noteName, '8n', undefined, vel)
  }/** 子球撞墙时实时触发音符 */
  triggerChildNote(note: number, velocity: number) {
    const noteName = Tone.Frequency(note, 'midi').toNote()
    this.synth.triggerAttackRelease(noteName, '8n', undefined, velocity / 127)
  }

  async start() {
    await Tone.start()
    Tone.getTransport().start()
  }

  stop() {
    Tone.getTransport().stop()
    Tone.getTransport().position = 0
  }

  pause() {
    Tone.getTransport().pause()
  }

  resume() {
    Tone.getTransport().start()
  }

  /** 获取当前播放时间(ms) */
  getCurrentTimeMs(): number {
    return Tone.getTransport().seconds * 1000
  }/** 跳转到指定时间(ms) */
  seekTo(timeMs: number) {
    const wasPlaying = Tone.getTransport().state === 'started'
    Tone.getTransport().seconds = timeMs / 1000
    if (!wasPlaying) Tone.getTransport().pause()
  }

  private _playNote(event: TimelineEvent, time: number) {
    const noteName = Tone.Frequency(event.note, 'midi').toNote()
    const vel = event.velocity / 127
    this.synth.triggerAttackRelease(noteName, '8n', time, vel)// SPLITTER: 同时播放和弦内所有附加音符
    if (event.type === 'SPLITTER' && event.chordNotes) {
      for (const cn of event.chordNotes) {
        const cnName = Tone.Frequency(cn.note, 'midi').toNote()
        this.synth.triggerAttackRelease(cnName, '8n', time, cn.velocity / 127)
      }
    }
  }
}
