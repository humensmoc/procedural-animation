using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class LegData
{
    [Tooltip("腿在身体上的位置（从头部起的比例，0-1之间）")]
    [Range(0f, 1f)]
    public float positionRatio = 0.5f;
    
    [Tooltip("腿部目标点与身体的横向距离")]
    [Range(0.5f, 3f)]
    public float targetSideOffset = 1f;
    
    [Tooltip("关节到身体和脚的距离")]
    [Range(0.3f, 2f)]
    public float jointLength = 0.8f;
    
    [Tooltip("是否是左侧的腿")]
    public bool isLeft = true;
}

public class CircleLizard : MonoBehaviour
{
    [Header("腿部配置")]
    public List<LegData> legs = new List<LegData>();
    private Chain body;
    private List<Chain> legChains;
    private List<List<GameObject>> legSegments;
    private List<GameObject> jointSegments;
    private List<Vector3> jointPositions;
    private List<LineRenderer> bodyToJointLines;
    private List<LineRenderer> jointToFootLines;
    private List<Vector3> legDesiredPositions;
    private List<Vector3> currentLegTargets;
    private List<Vector3> footVelocities;
    
    // 添加缺失的字段
    private List<GameObject> bodySegments;
    private Vector3 currentTargetPos;
    private Vector3 smoothVelocity;

    [Header("移动参数")]
    public float minMoveDistance = 0.1f;    // 最小移动距离
    public float moveSpeed = 5f;            // 移动速度
    public float smoothTime = 0.1f;         // 平滑时间

    [Header("身体参数")]
    [Tooltip("身体节数")]
    [Range(10, 100)]
    public int bodySegmentCount = 48;
    [Tooltip("身体节点间距")]
    [Range(0.1f, 1f)]
    public float bodySegmentLength = 0.2f;
    [Tooltip("身体角度限制")]
    [Range(0f, 180f)]
    public float bodyAngleConstraint = 30f;

    [Header("腿部参数")]
    [Tooltip("腿部节数")]
    [Range(3, 10)]
    public int legSegmentCount = 3;
    [Tooltip("腿部节点间距")]
    [Range(0.1f, 0.5f)]
    public float legSegmentLength = 0.15f;
    [Tooltip("腿部角度限制")]
    [Range(0f, 180f)]
    public float legAngleConstraint = 45f;
    [Tooltip("腿部移动提前量")]
    [Range(0, 5)]
    public int legMoveAhead = 3;
    [Tooltip("腿部移动阈值")]
    public float legMoveThreshold = 0.5f;
    [Tooltip("腿部目标点插值速度")]
    [Range(0.001f, 1f)]
    public float legLerpSpeed = 0.4f;
    [Tooltip("脚的移动速度（单位/秒）")]
    [Range(1f, 100f)]
    public float footMoveSpeed = 5f;
    [Tooltip("腿部与身体的横向距离")]
    [Range(0.1f, 2f)]
    public float legSideOffset = 0.5f;
    [Tooltip("腿部目标点与身体的横向距离")]
    [Range(0.5f, 3f)]
    public float legTargetSideOffset = 1f;

    [Header("渲染参数")]
    [Tooltip("身体头部大小")]
    public float bodyHeadSize = 0.8f;
    [Tooltip("身体尾部大小")]
    public float bodyTailSize = 0.3f;
    [Tooltip("腿部起始大小")]
    public float legStartSize = 0.4f;
    [Tooltip("腿部末端大小")]
    public float legEndSize = 0.15f;
    [Tooltip("身体颜色渐变")]
    public Gradient bodyColorGradient;
    [Tooltip("腿部颜色渐变")]
    public Gradient legColorGradient;

    

    private GameObject segmentPrefab;
    private GameObject linePrefab;

    void Start()
    {
        InitializeLizard();
    }

    void OnValidate()
    {
        if (Application.isPlaying && body != null)
        {
            InitializeLizard();
        }
    }

    private void InitializeLizard()
    {
        // 清理所有现有对象
        // CleanupAll();

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

        // 加载线条Prefab
        if (linePrefab == null)
        {
            linePrefab = Resources.Load<GameObject>("FluidThing/Line");
            if (linePrefab == null)
            {
                Debug.LogError("找不到Line预制体！请确保它在Resources/FluidThing/目录下");
                return;
            }
        }

        legDesiredPositions = new List<Vector3>();
        currentLegTargets = new List<Vector3>();
        footVelocities = new List<Vector3>();
        legChains = new List<Chain>();
        legSegments = new List<List<GameObject>>();
        jointSegments = new List<GameObject>();
        jointPositions = new List<Vector3>();
        bodyToJointLines = new List<LineRenderer>();
        jointToFootLines = new List<LineRenderer>();

        // 初始化默认渐变色
        InitializeDefaultGradients();

        // 初始化身体和腿部
        InitializeBody();
        InitializeLegs();
    }

    private void InitializeDefaultGradients()
    {
        if (bodyColorGradient == null)
        {
            bodyColorGradient = new Gradient();
            var bodyColorKeys = new GradientColorKey[2];
            bodyColorKeys[0] = new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 0f);
            bodyColorKeys[1] = new GradientColorKey(new Color(0.67f, 0.22f, 0.19f), 1f);
            var bodyAlphaKeys = new GradientAlphaKey[2];
            bodyAlphaKeys[0] = new GradientAlphaKey(1f, 0f);
            bodyAlphaKeys[1] = new GradientAlphaKey(1f, 1f);
            bodyColorGradient.SetKeys(bodyColorKeys, bodyAlphaKeys);
        }

        if (legColorGradient == null)
        {
            legColorGradient = new Gradient();
            var legColorKeys = new GradientColorKey[2];
            legColorKeys[0] = new GradientColorKey(new Color(0.4f, 0.15f, 0.15f), 0f);
            legColorKeys[1] = new GradientColorKey(new Color(0.8f, 0.3f, 0.25f), 1f);
            var legAlphaKeys = new GradientAlphaKey[2];
            legAlphaKeys[0] = new GradientAlphaKey(1f, 0f);
            legAlphaKeys[1] = new GradientAlphaKey(1f, 1f);
            legColorGradient.SetKeys(legColorKeys, legAlphaKeys);
        }
    }

    private void InitializeBody()
    {
        // 初始化身体
        body = gameObject.AddComponent<Chain>();
        body.segmentCount = bodySegmentCount;
        body.segmentLength = bodySegmentLength;
        body.angleConstraint = bodyAngleConstraint;
        body.InitializeChain(transform.position);
        currentTargetPos = transform.position;

        // 创建身体段
        bodySegments = new List<GameObject>();
        CreateSegments(body.joints, bodySegments, bodyHeadSize, bodyTailSize, bodyColorGradient);
    }

    private void InitializeLegs()
    {

        // 为每条腿初始化数据结构
        for (int i = 0; i < legs.Count; i++)
        {
            // 创建Chain
            Chain legChain = gameObject.AddComponent<Chain>();
            legChain.segmentCount = 1;  // 只生成一个节点作为脚
            legChain.segmentLength = legSegmentLength;
            legChain.angleConstraint = legAngleConstraint;
            legChains.Add(legChain);

            // 计算腿在身体上的位置索引
            int bodyIndex = Mathf.RoundToInt(legs[i].positionRatio * (bodySegmentCount - 1));
            float side = legs[i].isLeft ? -1f : 1f;

            // 初始化腿的位置
            Vector3 startPos = body.joints[bodyIndex] + Vector3.right * side * legs[i].targetSideOffset;
            legChain.InitializeChain(startPos);

            // 初始化目标位置数组
            legDesiredPositions.Add(GetLegTargetPosition(i));
            currentLegTargets.Add(legDesiredPositions[i]);
            footVelocities.Add(Vector3.zero);

            // 创建腿部渲染
            List<GameObject> segments = new List<GameObject>();
            legSegments.Add(segments);
            CreateFootSegment(legChain.joints, segments, legEndSize, legColorGradient);

            // 创建关节
            GameObject jointObj = Instantiate(segmentPrefab, Vector3.zero, Quaternion.identity, transform);
            jointObj.name = $"Joint_{i}";
            jointObj.transform.localScale = Vector3.one * (legEndSize * 0.5f);
            
            SpriteRenderer renderer = jointObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = legColorGradient.Evaluate(0.5f);
            }
            
            jointSegments.Add(jointObj);
            jointPositions.Add(Vector3.zero);

            // 创建连接线
            bodyToJointLines.Add(CreateLegLine($"BodyToJoint_{i}"));
            jointToFootLines.Add(CreateLegLine($"JointToFoot_{i}"));
        }
    }

    private LineRenderer CreateLegLine(string name)
    {
        GameObject lineObj = Instantiate(linePrefab, transform);
        lineObj.name = name;
        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        
        // 设置线条属性
        line.positionCount = 2;  // 只需要两个点：身体端点和脚
        line.startWidth = 0.1f;  // 起始宽度
        line.endWidth = 0.05f;   // 结束宽度
        line.useWorldSpace = true;  // 使用世界坐标
        
        return line;
    }

    private void UpdateLegLines()
    {
        if (body == null) return;

        for (int i = 0; i < legs.Count; i++)
        {
            Chain leg = legChains[i];
            if (leg == null || leg.joints.Count == 0) continue;

            // 计算身体连接点的索引
            int bodyIndex = Mathf.RoundToInt(legs[i].positionRatio * (bodySegmentCount - 1));
            if (bodyIndex >= body.joints.Count) continue;

            // 获取身体段上的准确连接点
            Vector3 bodyConnectionPoint = GetBodyConnectionPoint(bodyIndex, legs[i].isLeft);
            Vector3 footPos = leg.joints[0];

            // 计算关节位置
            jointPositions[i] = CalculateJointPosition(bodyConnectionPoint, footPos, legs[i], bodyIndex);
            
            // 更新关节渲染位置
            if (jointSegments[i] != null)
            {
                jointSegments[i].transform.position = jointPositions[i];
            }

            // 更新身体到关节的线
            if (bodyToJointLines[i] != null)
            {
                bodyToJointLines[i].SetPosition(0, bodyConnectionPoint);
                bodyToJointLines[i].SetPosition(1, jointPositions[i]);
            }

            // 更新关节到脚的线
            if (jointToFootLines[i] != null)
            {
                jointToFootLines[i].SetPosition(0, jointPositions[i]);
                jointToFootLines[i].SetPosition(1, footPos);
            }
        }
    }

    private Vector3 GetBodyConnectionPoint(int bodyIndex, bool isLeft)
    {
        if (bodyIndex <= 0 || bodyIndex >= body.joints.Count) return body.joints[bodyIndex];

        // 获取当前节点和前一个节点
        Vector3 currentPos = body.joints[bodyIndex];
        Vector3 prevPos = body.joints[bodyIndex - 1];

        // 计算身体段的方向
        Vector3 bodyDir = (currentPos - prevPos).normalized;
        
        // 计算垂直于身体方向的向量（左侧为负，右侧为正）
        Vector3 perpendicular = new Vector3(-bodyDir.y, bodyDir.x, 0);
        
        // 计算圆上的连接点（使用bodyStartSize作为圆的半径）
        float radius = bodyHeadSize * 0.5f; // 使用一半的尺寸作为实际半径
        return currentPos + (isLeft ? -perpendicular : perpendicular) * radius;
    }

    private Vector3 CalculateJointPosition(Vector3 bodyConnectionPoint, Vector3 footPos, LegData legData, int bodyIndex)
    {
        float d = Vector3.Distance(bodyConnectionPoint, footPos);
        float jointLength = legData.jointLength;
        
        if (d > jointLength * 2)
        {
            return (bodyConnectionPoint + footPos) * 0.5f;
        }
        
        if (d < 0.1f)
        {
            return (bodyConnectionPoint + footPos) * 0.5f;
        }

        float a = (jointLength * jointLength - jointLength * jointLength + d * d) / (2 * d);
        float h = Mathf.Sqrt(jointLength * jointLength - a * a);

        Vector3 directionVector = (footPos - bodyConnectionPoint).normalized;
        Vector3 perpendicular = new Vector3(-directionVector.y, directionVector.x, 0);

        Vector3 midPoint = bodyConnectionPoint + directionVector * a;
        Vector3 intersection1 = midPoint + perpendicular * h;
        Vector3 intersection2 = midPoint - perpendicular * h;

        // 获取身体的整体方向（从尾部到头部）
        Vector3 bodyDirection = (body.joints[0] - body.joints[body.joints.Count - 1]).normalized;

        // 计算两个交点相对于连接线的方向向量
        Vector3 toIntersection1 = (intersection1 - midPoint).normalized;
        Vector3 toIntersection2 = (intersection2 - midPoint).normalized;

        float dot1 = Vector3.Dot(toIntersection1, bodyDirection);
        float dot2 = Vector3.Dot(toIntersection2, bodyDirection);

        // 根据腿的位置选择合适的交点
        float midPoint_ratio = 0.5f;
        bool isFrontLeg = legData.positionRatio < midPoint_ratio;
        
        return isFrontLeg ? 
            (dot1 < dot2 ? intersection1 : intersection2) :
            (dot1 > dot2 ? intersection1 : intersection2);
    }

    private Vector3 GetLegTargetPosition(int legIndex)
    {
        if (legIndex >= legs.Count) return Vector3.zero;
        
        LegData legData = legs[legIndex];
        int bodyIndex = Mathf.RoundToInt(legData.positionRatio * (bodySegmentCount - 1));
        float side = legData.isLeft ? -1f : 1f;
        
        //保持脚在身体前方
        if(bodyIndex-legMoveAhead >= 0)
        {
            bodyIndex -= legMoveAhead;
        }else{
            bodyIndex=0;
        }
        
        Vector3 bodyPos = body.joints[bodyIndex];
        
        // 获取身体段的方向
        Vector3 bodyDir = Vector3.right;
        if (bodyIndex > 0)
        {
            bodyDir = (body.joints[bodyIndex] - body.joints[bodyIndex - 1]).normalized;
        }
        
        // 计算垂直于身体方向的向量
        Vector3 perpendicular = new Vector3(-bodyDir.y, bodyDir.x, 0);
        
        // 计算腿部目标位置
        return bodyPos + perpendicular * side * legData.targetSideOffset;
    }

    private void CreateSegments(List<Vector3> joints, List<GameObject> segmentsList, float startSize, float endSize, Gradient gradient)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, joints[i], Quaternion.identity, transform);
            segment.name = $"Segment_{segmentsList.Count}_{i}";
            
            float t = i / (float)(joints.Count - 1);
            float size = Mathf.Lerp(startSize, endSize, t);
            segment.transform.localScale = new Vector3(size, size, 1f);

            SpriteRenderer renderer = segment.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = gradient.Evaluate(t);
            }

            segmentsList.Add(segment);
        }
    }

    private void CreateFootSegment(List<Vector3> joints, List<GameObject> segmentsList, float size, Gradient gradient)
    {
        if (joints.Count > 0)
        {
            GameObject segment = Instantiate(segmentPrefab, joints[0], Quaternion.identity, transform);
            segment.name = $"Foot_{segmentsList.Count}";
            
            segment.transform.localScale = new Vector3(size, size, 1f);

            SpriteRenderer renderer = segment.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = gradient.Evaluate(1f);  // 使用渐变色的末端颜色
            }

            segmentsList.Add(segment);
        }
    }

    private void UpdateSegmentsPositions(List<Vector3> joints, List<GameObject> segments)
    {
        for (int i = 0; i < segments.Count && i < joints.Count; i++)
        {
            if (segments[i] != null)
            {
                segments[i].transform.position = joints[i];
            }
        }
    }

    void Update()
    {
        if (body == null) return;

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

        // 更新腿部位置（移到这里，确保每帧都更新，而不是只在移动时更新）
        UpdateLegs();

        // 更新所有段的位置
        UpdateAllSegmentsPositions();
    }

    private void UpdateLegs()
    {
        if (body == null || legChains == null) return;

        for (int i = 0; i < legs.Count; i++)
        {
            Chain leg = legChains[i];
            if (leg == null || leg.joints.Count == 0) continue;

            // 计算新的目标位置
            Vector3 desiredPos = GetLegTargetPosition(i);

            // 只有当目标位置与当前脚的位置距离超过阈值时才更新目标
            float distanceToFoot = Vector3.Distance(desiredPos, leg.joints[0]);
            if (distanceToFoot > legMoveThreshold)
            {
                legDesiredPositions[i] = desiredPos;
            }

            // 创建临时变量来存储速度
            Vector3 tempVelocity = footVelocities[i];
            
            // 平滑移动到目标位置
            leg.joints[0] = Vector3.SmoothDamp(
                leg.joints[0],
                legDesiredPositions[i],
                ref tempVelocity,
                legLerpSpeed,
                footMoveSpeed
            );
            
            // 更新速度
            footVelocities[i] = tempVelocity;
        }
    }

    private void UpdateAllSegmentsPositions()
    {
        UpdateSegmentsPositions(body.joints, bodySegments);
        
        // 更新所有腿的段位置
        for (int i = 0; i < legChains.Count && i < legSegments.Count; i++)
        {
            UpdateSegmentsPositions(legChains[i].joints, legSegments[i]);
        }

        // 更新连接线位置
        UpdateLegLines();
    }
} 