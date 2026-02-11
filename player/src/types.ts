/** .mdbl文件顶层数据结构 */
export interface MdblData {
  meta: Meta
  assets: Assets
  timeline: TimelineEvent[]
  ballPath: BallKeyframe[]
}

export interface Meta {
  title: string
  bpm: number
  totalTime: number
  mapBounds: MapBounds
}

export interface MapBounds {
  minX: number
  maxX: number
  minY: number
  maxY: number
}

export interface Assets {
  instruments: Instrument[]
}

export interface Instrument {
  id: number
  name: string
  color: string
}

/**时间轴事件（MVP阶段只有WALL类型） */
export interface TimelineEvent {
  id: number
  time: number        // 撞击时间(ms)
  type?: string       // "WALL" | "SPLITTER" | "MERGER"（MVP只用WALL）
  pos: { x: number; y: number }
  rotation: number    // 墙体旋转角(度)
  note: number        // MIDI音高
  velocity: number    // 力度
  instrumentId: number
  chordNotes?: { note: number; velocity: number; instrumentId: number }[]
  childPaths?: ChildPath[]
}

/** 子球路径数据（SPLITTER分裂后每个子球的独立路径） */
export interface ChildPath {
  note: number
  velocity: number
  instrumentId: number
  walls: TimelineEvent[]
  keyframes: BallKeyframe[]
  mergerTime: number
}

/** 小球路径关键帧 */
export interface BallKeyframe {
  time: number
  x: number
  y: number
}
