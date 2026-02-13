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
                // 确保有SphereCollider
                var sc = playerObj.GetComponent<SphereCollider>();
                if (sc == null) sc = playerObj.AddComponent<SphereCollider>();
                sc.radius = 0.4f;
                sc.isTrigger = false;
                // 移除Sphere原始体自带的MeshCollider（如果有）
                var mc = playerObj.GetComponent<MeshCollider>();
                if (mc != null) Object.DestroyImmediate(mc);// 确保有Rigidbody
                var rb = playerObj.GetComponent<Rigidbody>();
                if (rb == null) rb = playerObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Debug.Log("[SceneSetup] Player碰撞组件已配置: SphereCollider + Kinematic Rigidbody");
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
            // 强制通过SerializedObject设置noteScale，覆盖Inspector中的旧值
            var so = new SerializedObject(mapGen);
            var scaleProp = so.FindProperty("noteScale");
            if (scaleProp != null)
            {
                scaleProp.vector3Value = new Vector3(3.0f, 0.6f, 2.0f);so.ApplyModifiedProperties();Debug.Log("[SceneSetup] MapGenerator.noteScale 已强制设为 (3.0, 0.6, 2.0)");
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

            // --- CameraFollow ---
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraFollow>() == null)
                cam.gameObject.AddComponent<CameraFollow>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());Debug.Log("[SceneSetup] ✓ Phase3 场景配置完成！请重新点击 Play 验证碰撞反弹。");
        }
    }
}
#endif
