// Phase2SceneSetup.cs — 一键配置阶段二测试场景（编辑器菜单工具）
// 使用：Unity菜单 -> StarPipe -> Setup Phase2 Scene
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using StarPipe.Audio;
using StarPipe.Gameplay;

namespace StarPipe.Editor
{
    public static class Phase2SceneSetup
    {
        [MenuItem("StarPipe/Setup Phase2 Scene")]
        public static void SetupScene()
        {
            // 1. 清理旧的MidiParserTest（如果存在）
            var oldTest = Object.FindObjectOfType<MidiParserTest>();
            if (oldTest != null)
            {
                Object.DestroyImmediate(oldTest);
                Debug.Log("[SceneSetup] 已移除 MidiParserTest");
            }

            // 2. 创建/查找 AudioConductor
            var conductorObj = GameObject.Find("AudioConductor");
            if (conductorObj == null)
            {
                conductorObj = new GameObject("AudioConductor");
                Debug.Log("[SceneSetup] 创建 AudioConductor");
            }
            if (conductorObj.GetComponent<AudioConductor>() == null)
                conductorObj.AddComponent<AudioConductor>();

            // 3. 创建/查找 Player
            var playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                playerObj.name = "Player";
                Debug.Log("[SceneSetup] 创建 Player (Sphere)");
            }
            playerObj.transform.position = new Vector3(0, 0.5f, 0);
            if (playerObj.GetComponent<PlayerController>() == null)
                playerObj.AddComponent<PlayerController>();

            // 4. 调整 Main Camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.eulerAngles = new Vector3(30, 0, 0);Debug.Log("[SceneSetup] 已调整 Main Camera");
            }

            // 标记场景为已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[SceneSetup]✓ Phase2 场景配置完成！点击 Play 验证。");
        }
    }
}
#endif
