import type { MdblData } from './types'

/**从File对象加载.mdbl数据 */
export async function loadMdblFromFile(file: File): Promise<MdblData> {
  const text = await file.text()
  return parseMdbl(text)
}

/** 从URL加载.mdbl数据 */
export async function loadMdblFromUrl(url: string): Promise<MdblData> {
  const resp = await fetch(url)
  if (!resp.ok) throw new Error(`加载失败: ${resp.status}`)
  const text = await resp.text()
  return parseMdbl(text)
}

/** 解析并校验.mdbl JSON字符串 */
function parseMdbl(text: string): MdblData {
  const data = JSON.parse(text) as MdblData
  // 基础字段校验
  if (!data.meta) throw new Error('.mdbl缺少meta字段')
  if (!data.timeline || !Array.isArray(data.timeline))
    throw new Error('.mdbl缺少timeline字段')
  if (!data.ballPath || !Array.isArray(data.ballPath))
    throw new Error('.mdbl缺少ballPath字段')
  if (!data.assets) throw new Error('.mdbl缺少assets字段')
  return data
}
