using UnityEngine;
using System.Collections.Generic;

public class CircleSnake : MonoBehaviour
{
    private Chain body;
    private List<GameObject> segments;
    private Vector3 currentTargetPos;
    private Vector3 smoothVelocity;

    [Header("移动参数")]
    public float minMoveDistance = 0.1f;    // 最小移动距离
    public float moveSpeed = 5f;            // 移动速度
    public float smoothTime = 0.1f;         // 平滑时间

    [Header("蛇身参数")]
    [Tooltip("蛇身节数")]
    [Range(10, 100)]
    public int segmentCount = 48;
    [Tooltip("节点间距")]
    [Range(0.1f, 1f)]
    public float segmentLength = 0.2f;
    [Tooltip("角度限制")]
    [Range(0f, 180f)]
    public float angleConstraint = 30f;

    [Header("渲染参数")]
    [Tooltip("蛇头的大小")]
    public float headSize = 0.8f;
    [Tooltip("蛇尾的大小")]
    public float tailSize = 0.3f;
    [Tooltip("蛇身颜色渐变")]
    public Gradient colorGradient;

    private GameObject segmentPrefab;

    void Start()
    {
        InitializeSnake();
    }

    void OnValidate()
    {
        // 如果在编辑器中修改了参数，重新初始化蛇
        if (Application.isPlaying && body != null)
        {
            InitializeSnake();
        }
    }

    private void InitializeSnake()
    {
        // 清理旧的segments
        CleanupSegments();

        // 清理旧的Chain组件
        if (body != null)
        {
            Destroy(body);
        }

        // 加载圆形Prefab
        if (segmentPrefab == null)
        {
            segmentPrefab = Resources.Load<GameObject>("FluidThing/CircleSegment");
            if (segmentPrefab == null)
            {
                Debug.LogError("找不到CircleSegment预制体！请确保它在Resources/FluidThing/目录下");
                return;
            }
        }

        // 初始化蛇身
        body = gameObject.AddComponent<Chain>();
        body.segmentCount = segmentCount;
        body.segmentLength = segmentLength;
        body.angleConstraint = angleConstraint;
        body.InitializeChain(transform.position);
        currentTargetPos = transform.position;

        // 初始化默认渐变色（如果没有设置）
        if (colorGradient == null)
        {
            colorGradient = new Gradient();
            var colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.67f, 0.22f, 0.19f), 1f);
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            colorGradient.SetKeys(colorKeys, alphaKeys);
        }

        // 创建蛇身段
        CreateSnakeSegments();
    }

    private void CleanupSegments()
    {
        if (segments != null)
        {
            foreach (var segment in segments)
            {
                if (segment != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(segment);
                    }
                    else
                    {
                        DestroyImmediate(segment);
                    }
                }
            }
            segments.Clear();
        }
        segments = new List<GameObject>();
    }

    private void CreateSnakeSegments()
    {
        // 为每个关节创建一个圆形
        for (int i = 0; i < body.joints.Count; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, body.joints[i], Quaternion.identity, transform);
            segment.name = $"Segment_{i}";
            
            // 计算大小
            float t = i / (float)(body.joints.Count - 1);
            float size = Mathf.Lerp(headSize, tailSize, t);
            segment.transform.localScale = new Vector3(size, size, 1f);

            // 设置颜色
            SpriteRenderer renderer = segment.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = colorGradient.Evaluate(t);
            }

            segments.Add(segment);
        }
    }

    private void UpdateSegmentsAppearance()
    {
        if (segments == null) return;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                // 更新大小
                float t = i / (float)(segments.Count - 1);
                float size = Mathf.Lerp(headSize, tailSize, t);
                segments[i].transform.localScale = new Vector3(size, size, 1f);

                // 更新颜色
                SpriteRenderer renderer = segments[i].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = colorGradient.Evaluate(t);
                }
            }
        }
    }

    void Update()
    {
        if (body == null || segments == null) return;

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

        UpdateSegmentsPosition();
    }

    private void UpdateSegmentsPosition()
    {
        if (segments == null || body == null) return;

        for (int i = 0; i < segments.Count && i < body.joints.Count; i++)
        {
            if (segments[i] != null)
            {
                segments[i].transform.position = body.joints[i];
            }
        }
    }

    void OnDestroy()
    {
        CleanupSegments();
        
        if (body != null)
        {
            if (Application.isPlaying)
            {
                Destroy(body);
            }
            else
            {
                DestroyImmediate(body);
            }
        }
    }
} 