// Phase4SceneSetup.cs — 编辑器一键配置Phase4场景
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using StarPipe.Gameplay;
using StarPipe.Visuals;

namespace StarPipe.EditorTools
{
    public static class Phase4SceneSetup
    {
        [MenuItem("StarPipe/Setup Phase4 Scene")]
        public static void Setup()
        {
            // GameStateManager
            if (Object.FindObjectOfType<GameStateManager>() == null)
            {
                var go = new GameObject("GameStateManager");
                go.AddComponent<GameStateManager>();
                Debug.Log("[Phase4Setup] 创建 GameStateManager");
            }
            else Debug.Log("[Phase4Setup] GameStateManager 已存在");

            // WorldCurve占位
            if (Object.FindObjectOfType<WorldCurve>() == null)
            {
                var go = new GameObject("WorldCurve");
                go.AddComponent<WorldCurve>();
                Debug.Log("[Phase4Setup] 创建 WorldCurve（占位）");
            }
            else Debug.Log("[Phase4Setup] WorldCurve 已存在");

            // 检查前置组件
            CheckExists<StarPipe.Audio.AudioConductor>("AudioConductor");
            CheckExists<StarPipe.Map.MapGenerator>("MapGenerator");
            CheckExists<NoteJudge>("NoteJudge");
            CheckExists<PlayerController>("PlayerController");

            Debug.Log("[Phase4Setup] ✓ Phase4 场景配置完成");
        }

        private static void CheckExists<T>(string name) where T : Component
        {
            if (Object.FindObjectOfType<T>() == null)
                Debug.LogWarning($"[Phase4Setup] ⚠ 缺少 {name}，请先运行Phase2/3Setup");
        }
    }
}
#endif
