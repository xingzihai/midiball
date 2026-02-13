// Phase3SceneSetup.cs — 一键配置阶段三测试场景
// 使用：Unity菜单 -> StarPipe -> Setup Phase3 Scene
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using StarPipe.Audio;
using StarPipe.Gameplay;
using StarPipe.Map;
using StarPipe.Visuals;

namespace StarPipe.Editor
{
    public static class Phase3SceneSetup
    {
        [MenuItem("StarPipe/Setup Phase3 Scene")]
        public static void SetupScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[SceneSetup] 请先停止Play模式再执行！");
                return;
            }

            Phase2SceneSetup.SetupScene();

            var mapObj = GameObject.Find("MapGenerator");
            if (mapObj == null)
            {
                mapObj = new GameObject("MapGenerator");
                Debug.Log("[SceneSetup] 创建 MapGenerator");
            }
            if (mapObj.GetComponent<MapGenerator>() == null)
                mapObj.AddComponent<MapGenerator>();

            var judgeObj = GameObject.Find("NoteJudge");
            if (judgeObj == null)
            {
                judgeObj = new GameObject("NoteJudge");
                Debug.Log("[SceneSetup] 创建 NoteJudge");
            }
            if (judgeObj.GetComponent<NoteJudge>() == null)
                judgeObj.AddComponent<NoteJudge>();

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraFollow>() == null)
                cam.gameObject.AddComponent<CameraFollow>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[SceneSetup] ✓ Phase3 场景配置完成！点击 Play 验证。");
        }
    }
}
#endif
