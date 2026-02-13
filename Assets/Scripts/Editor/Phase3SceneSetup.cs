// Phase3SceneSetup.cs — 一键配置阶段三测试场景
// 使用：Unity菜单 -> StarPipe -> Setup Phase3 Scene
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using StarPipe.Audio;
using StarPipe.Gameplay;
using StarPipe.Map;

namespace StarPipe.Editor
{
    public static class Phase3SceneSetup
    {
        [MenuItem("StarPipe/Setup Phase3 Scene")]
        public static void SetupScene()
        {
            // 先执行Phase2配置
            Phase2SceneSetup.SetupScene();

            // 创建/查找 MapGenerator
            var mapObj = GameObject.Find("MapGenerator");
            if (mapObj == null)
            {
                mapObj = new GameObject("MapGenerator");
                Debug.Log("[SceneSetup] 创建 MapGenerator");
            }
            if (mapObj.GetComponent<MapGenerator>() == null)
                mapObj.AddComponent<MapGenerator>();

            // 创建/查找 NoteJudge
            var judgeObj = GameObject.Find("NoteJudge");
            if (judgeObj == null)
            {
                judgeObj = new GameObject("NoteJudge");
                Debug.Log("[SceneSetup] 创建 NoteJudge");
            }
            if (judgeObj.GetComponent<NoteJudge>() == null)
                judgeObj.AddComponent<NoteJudge>();

            // 调整摄像机：稍微拉远以看到更多方块
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 8, -15);
                cam.transform.eulerAngles = new Vector3(25, 0, 0);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());Debug.Log("[SceneSetup] ✓ Phase3 场景配置完成！点击 Play 验证。");
        }
    }
}
#endif
