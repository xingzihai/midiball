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

            // --- Player碰撞组件 ---
            var playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerObj.tag = "Player";
                var sc = playerObj.GetComponent<SphereCollider>();
                if (sc == null) sc = playerObj.AddComponent<SphereCollider>();
                sc.radius = 0.4f;
                sc.isTrigger = false;
                var mc = playerObj.GetComponent<MeshCollider>();
                if (mc != null) Object.DestroyImmediate(mc);
                var rb = playerObj.GetComponent<Rigidbody>();
                if (rb == null) rb = playerObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None; // 关闭插值防抖动
                Debug.Log("[SceneSetup] Player碰撞组件已配置");
            }

            // --- MapGenerator ---
            var mapObj = GameObject.Find("MapGenerator");
            if (mapObj == null)
            {
                mapObj = new GameObject("MapGenerator");
                Debug.Log("[SceneSetup] 创建 MapGenerator");
            }
            var mapGen = mapObj.GetComponent<MapGenerator>();
            if (mapGen == null) mapGen = mapObj.AddComponent<MapGenerator>();
            var so = new SerializedObject(mapGen);
            var scaleProp = so.FindProperty("noteScale");
            if (scaleProp != null)
            {
                scaleProp.vector3Value = new Vector3(0.15f, 1.5f, 5.0f);
                so.ApplyModifiedProperties();
                Debug.Log("[SceneSetup] noteScale 已设为薄墙 (0.15, 1.5, 5.0)");
            }

            // --- NoteJudge ---
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
            Debug.Log("[SceneSetup] ✓ Phase3 场景配置完成！");
        }
    }
}
#endif
