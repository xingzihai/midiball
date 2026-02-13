// WorldCurve.cs —弯曲世界Shader控制器（Phase4占位）
// Phase5接入Curved World插件后，此脚本控制弯曲强度参数
using UnityEngine;

namespace StarPipe.Visuals
{
    public class WorldCurve : MonoBehaviour
    {
        [Header("弯曲参数（Phase5接入Shader后生效）")]
        [SerializeField] private float curveStrength = 0.01f;
        [SerializeField] private float curveOffset = 0f;

        // Phase5: 将这些参数传递给Curved World全局Shader属性
        // Shader.SetGlobalFloat("_CurveStrength", curveStrength);

        public float CurveStrength
        {
            get => curveStrength;
            set => curveStrength = value;
        }

        void Start()
        {
            Debug.Log($"[WorldCurve] 占位初始化 | strength={curveStrength} " +
                      "(Phase5接入实际Shader)");
        }
    }
}
