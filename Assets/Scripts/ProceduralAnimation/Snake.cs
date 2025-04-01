using UnityEngine;

public class Snake : MonoBehaviour
{
    private Chain body;
    private LineRenderer lineRenderer;
    private float[] bodyWidths;
    private Vector3 currentTargetPos;
    private Vector3 smoothVelocity;
    private Material snakeMaterial;

    [Header("移动参数")]
    public float minMoveDistance = 0.1f;    // 最小移动距离
    public float moveSpeed = 5f;            // 移动速度
    public float smoothTime = 0.1f;         // 平滑时间

    [Header("渲染参数")]
    [Tooltip("LineRenderer在蛇头位置的宽度")]
    public float lineHeadWidth = 1f;
    [Tooltip("LineRenderer在蛇尾位置的宽度")]
    public float lineTailWidth = 0.5f;
    [Tooltip("蛇头部分的颜色")]
    public Color headColor = new Color(0.3f, 0.1f, 0.1f);
    [Tooltip("蛇尾部分的颜色")]
    public Color tailColor = new Color(0.67f, 0.22f, 0.19f);

    void Start()
    {
        // 初始化身体宽度 - 从头到尾递减
        bodyWidths = new float[48];
        for (int i = 0; i < bodyWidths.Length; i++)
        {
            bodyWidths[i] = 0.8f - i * 0.01f;  // 从0.8开始递减
        }

        // 初始化蛇身
        body = gameObject.AddComponent<Chain>();
        body.segmentCount = 48;
        body.segmentLength = 0.2f;
        body.angleConstraint = 30f;
        body.InitializeChain(transform.position);
        currentTargetPos = transform.position;

        // 设置渲染器
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = body.segmentCount * 2;
        
        // 创建材质
        snakeMaterial = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = snakeMaterial;
        
        UpdateLineRendererSettings();
    }

    private void UpdateLineRendererSettings()
    {
        // 设置LineRenderer的宽度 - 从中间向两边对称
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, lineHeadWidth);      // 头部右侧
        widthCurve.AddKey(0.5f, lineTailWidth);    // 尾部两侧
        widthCurve.AddKey(1f, lineHeadWidth);      // 头部左侧
        lineRenderer.widthCurve = widthCurve;
        lineRenderer.widthMultiplier = 1f;
        
        // 设置渐变色 - 从中间向两边对称
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(headColor, 0f),     // 头部右侧
                new GradientColorKey(tailColor, 0.5f),   // 尾部
                new GradientColorKey(headColor, 1f)      // 头部左侧
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 0.5f),
                new GradientAlphaKey(1, 1)
            }
        );
        lineRenderer.colorGradient = gradient;
    }

    void OnValidate()
    {
        if (lineRenderer != null)
        {
            UpdateLineRendererSettings();
        }
    }

    void OnDestroy()
    {
        if (snakeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(snakeMaterial);
            }
            else
            {
                DestroyImmediate(snakeMaterial);
            }
        }
    }

    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 headPos = body.joints[0];
        float distanceToMouse = Vector3.Distance(mousePos, headPos);

        if (distanceToMouse > minMoveDistance)
        {
            Vector3 dirToMouse = (mousePos - headPos).normalized;
            Vector3 targetPos = mousePos;

            currentTargetPos = Vector3.SmoothDamp(
                currentTargetPos, 
                targetPos, 
                ref smoothVelocity, 
                smoothTime,
                moveSpeed
            );

            body.Resolve(currentTargetPos);
        }

        UpdateSnakeRenderer();
    }

    private void UpdateSnakeRenderer()
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        int currentIndex = 0;

        // 添加身体右侧的顶点
        for (int i = 0; i < body.joints.Count; i++)
        {
            Vector3 direction;
            if (i == 0)
                direction = (body.joints[1] - body.joints[0]).normalized;
            else if (i == body.joints.Count - 1)
                direction = (body.joints[i] - body.joints[i - 1]).normalized;
            else
                direction = (body.joints[i + 1] - body.joints[i - 1]).normalized;

            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * bodyWidths[i];
            positions[currentIndex++] = body.joints[i] + perpendicular;
        }

        // 添加身体左侧的顶点（反向）
        for (int i = body.joints.Count - 1; i >= 0; i--)
        {
            Vector3 direction;
            if (i == 0)
                direction = (body.joints[1] - body.joints[0]).normalized;
            else if (i == body.joints.Count - 1)
                direction = (body.joints[i] - body.joints[i - 1]).normalized;
            else
                direction = (body.joints[i + 1] - body.joints[i - 1]).normalized;

            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * bodyWidths[i];
            positions[currentIndex++] = body.joints[i] - perpendicular;
        }

        lineRenderer.SetPositions(positions);
    }
}