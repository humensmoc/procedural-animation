using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Fish : MonoBehaviour
{
    [Header("运动参数")]
    public float followSpeed = 5f;    // 跟随速度
    public float bodyWaveAmplitude = 0.2f; // 身体波动幅度

    private Chain spine;
    private LineRenderer lineRenderer;

    void Start()
    {
        // 初始化链式结构
        spine = gameObject.AddComponent<Chain>();
        spine.segmentCount = 12;
        spine.segmentLength = 0.3f;
        spine.InitializeChain(transform.position);

        // 配置线条渲染
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = spine.segmentCount;
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.1f;
    }

    void Update()
    {
        // 鼠标位置追踪
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // 平滑移动头部
        Vector3 targetPos = Vector3.Lerp(
            spine.joints[0],
            mousePos,
            Time.deltaTime * followSpeed
        );

        // 更新链式运动
        spine.Resolve(targetPos);

        // 添加身体波动效果
        ApplyBodyWave();

        // 更新线条渲染
        lineRenderer.SetPositions(spine.joints.ToArray());
    }

    // 添加身体波动
    private void ApplyBodyWave()
    {
        for (int i = 0; i < spine.joints.Count; i++)
        {
            float waveOffset = Mathf.Sin(Time.time * 3f + i * 0.5f) * bodyWaveAmplitude;
            spine.joints[i] += new Vector3(waveOffset, 0, 0);
        }
    }
}