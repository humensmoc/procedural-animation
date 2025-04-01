using UnityEngine;
using System.Collections.Generic;

public class Chain : MonoBehaviour
{
    [Header("链式结构")]
    public int segmentCount = 10;     // 节段数量
    public float segmentLength = 0.5f; // 节段间距
    public float angleConstraint = 45f;// 角度约束（度数）

    [Header("调试")]
    public bool drawGizmos = true;    // 显示调试球

    public List<Vector3> joints = new List<Vector3>();
    private List<float> angles = new List<float>();
    private Vector3 lastTargetPos;

    void Start()
    {
        InitializeChain(transform.position);
        lastTargetPos = transform.position;
    }

    // 初始化链式结构
    public void InitializeChain(Vector3 startPos)
    {
        joints.Clear();
        angles.Clear();
        
        for (int i = 0; i < segmentCount; i++)
        {
            joints.Add(startPos + Vector3.down * i * segmentLength);
            angles.Add(0f);
        }
        lastTargetPos = startPos;
    }

    // 简化角度到[0, 2π)范围
    private float SimplifyAngle(float angle)
    {
        while (angle >= 2 * Mathf.PI)
            angle -= 2 * Mathf.PI;
        while (angle < 0)
            angle += 2 * Mathf.PI;
        return angle;
    }

    // 计算相对角度差
    private float RelativeAngleDiff(float angle, float anchor)
    {
        angle = SimplifyAngle(angle + Mathf.PI - anchor);
        anchor = Mathf.PI;
        return anchor - angle;
    }

    // 约束角度
    private float ConstrainAngle(float angle, float anchor, float constraint)
    {
        float constraintRad = constraint * Mathf.Deg2Rad;
        if (Mathf.Abs(RelativeAngleDiff(angle, anchor)) <= constraintRad)
        {
            return SimplifyAngle(angle);
        }

        if (RelativeAngleDiff(angle, anchor) > constraintRad)
        {
            return SimplifyAngle(anchor - constraintRad);
        }

        return SimplifyAngle(anchor + constraintRad);
    }

    // 链条运动（每帧更新）
    public void Resolve(Vector3 targetPos)
    {
        if (joints.Count == 0) return;

        // 限制目标位置的移动速度
        float maxMoveDistance = segmentLength * 2f;
        if (Vector3.Distance(targetPos, lastTargetPos) > maxMoveDistance)
        {
            targetPos = lastTargetPos + (targetPos - lastTargetPos).normalized * maxMoveDistance;
        }
        lastTargetPos = targetPos;

        // 更新第一个关节
        joints[0] = targetPos;
        
        // 正向传播：从头到尾更新位置和角度
        for (int i = 1; i < joints.Count; i++)
        {
            Vector3 dir = (joints[i] - joints[i - 1]).normalized;
            joints[i] = joints[i - 1] + dir * segmentLength;

            // 计算并约束角度
            float currentAngle = Mathf.Atan2(dir.y, dir.x);
            if (i > 1)
            {
                float prevAngle = Mathf.Atan2(
                    joints[i-1].y - joints[i-2].y,
                    joints[i-1].x - joints[i-2].x
                );
                currentAngle = ConstrainAngle(currentAngle, prevAngle, angleConstraint);
                
                // 使用约束后的角度更新位置
                joints[i] = joints[i-1] + new Vector3(
                    Mathf.Cos(currentAngle),
                    Mathf.Sin(currentAngle),
                    0
                ) * segmentLength;
            }
            angles[i] = currentAngle;
        }
    }

    public void FabrikResolve(Vector3 targetPos, Vector3 anchorPos)
    {
        // Forward pass
        joints[0] = targetPos;
        for (int i = 1; i < joints.Count; i++)
        {
            joints[i] = ConstrainDistance(joints[i], joints[i-1], segmentLength);
        }

        // Backward pass
        joints[joints.Count - 1] = anchorPos;
        for (int i = joints.Count - 2; i >= 0; i--)
        {
            joints[i] = ConstrainDistance(joints[i], joints[i+1], segmentLength);
        }
    }

    private Vector3 ConstrainDistance(Vector3 pos, Vector3 anchor, float constraint)
    {
        return anchor + (pos - anchor).normalized * constraint;
    }

    // 调试绘制
    void OnDrawGizmos()
    {
        if (!drawGizmos || joints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 1; i < joints.Count; i++)
        {
            Gizmos.DrawLine(joints[i - 1], joints[i]);
            Gizmos.DrawSphere(joints[i], 0.1f);
        }
    }
}