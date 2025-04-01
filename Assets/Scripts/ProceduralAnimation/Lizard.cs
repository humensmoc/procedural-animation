using UnityEngine;

public class Lizard : MonoBehaviour
{
    private Chain body;
    private Chain[] legs = new Chain[4];
    private LineRenderer bodyRenderer;
    private LineRenderer[] legRenderers = new LineRenderer[4];
    private Vector3[] legTargets = new Vector3[4];
    private float[] bodyWidth = new float[] { 0.52f, 0.58f, 0.4f, 0.6f, 0.68f, 0.71f, 0.65f, 0.5f, 0.28f, 0.15f, 0.11f, 0.09f, 0.07f, 0.07f };

    void Start()
    {
        // 初始化身体
        body = gameObject.AddComponent<Chain>();
        body.segmentCount = 14;
        body.segmentLength = 0.3f;
        body.angleConstraint = 30f;
        body.InitializeChain(transform.position);

        // 添加身体的LineRenderer
        bodyRenderer = gameObject.AddComponent<LineRenderer>();
        bodyRenderer.material = new Material(Shader.Find("Sprites/Default"));
        bodyRenderer.startColor = new Color(0.32f, 0.47f, 0.43f); // 蜥蜴绿色
        bodyRenderer.endColor = new Color(0.32f, 0.47f, 0.43f);
        bodyRenderer.startWidth = 0.3f;
        bodyRenderer.endWidth = 0.2f;
        bodyRenderer.positionCount = body.segmentCount;

        // 初始化四条腿
        for (int i = 0; i < 4; i++)
        {
            GameObject legObj = new GameObject("Leg_" + i);
            legObj.transform.SetParent(transform);
            
            legs[i] = legObj.AddComponent<Chain>();
            legs[i].segmentCount = 3;
            legs[i].segmentLength = (i < 2) ? 0.4f : 0.3f; // 前腿比后腿长
            legs[i].angleConstraint = 45f;
            
            Vector3 shoulderPos = GetShoulderPosition(i);
            legs[i].InitializeChain(shoulderPos);
            legTargets[i] = shoulderPos + new Vector3((i % 2 == 0 ? 1 : -1) * 0.8f, -0.8f, 0);

            // 为每条腿添加LineRenderer
            legRenderers[i] = legObj.AddComponent<LineRenderer>();
            legRenderers[i].material = new Material(Shader.Find("Sprites/Default"));
            legRenderers[i].startColor = new Color(0.32f, 0.47f, 0.43f);
            legRenderers[i].endColor = new Color(0.32f, 0.47f, 0.43f);
            legRenderers[i].startWidth = 0.15f;
            legRenderers[i].endWidth = 0.05f;
            legRenderers[i].positionCount = legs[i].segmentCount;
        }
    }

    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        // 更新身体
        Vector3 targetPos = transform.position + (mousePos - transform.position).normalized * 0.5f;
        body.Resolve(targetPos);
        bodyRenderer.SetPositions(body.joints.ToArray());

        // 更新四肢
        for (int i = 0; i < 4; i++)
        {
            Vector3 shoulderPos = GetShoulderPosition(i);
            Vector3 desiredPos = shoulderPos + new Vector3(
                (i % 2 == 0 ? 1 : -1) * 0.8f,
                -0.8f,
                0
            );

            // 如果目标点距离太远，更新目标位置
            if (Vector3.Distance(legTargets[i], desiredPos) > 1f)
            {
                legTargets[i] = desiredPos;
            }

            // 平滑移动到目标位置
            legTargets[i] = Vector3.Lerp(legTargets[i], desiredPos, Time.deltaTime * 5f);
            
            // 解算腿部IK
            legs[i].Resolve(legTargets[i]);
            legRenderers[i].SetPositions(legs[i].joints.ToArray());
        }
    }

    private Vector3 GetShoulderPosition(int legIndex)
    {
        int bodyIndex = legIndex < 2 ? 3 : 7;
        float side = legIndex % 2 == 0 ? 1 : -1;
        Vector3 shoulderPos = body.joints[bodyIndex];
        shoulderPos += new Vector3(side * bodyWidth[bodyIndex], 0, 0);
        return shoulderPos;
    }
}