using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Fish : MonoBehaviour
{
    [Header("�˶�����")]
    public float followSpeed = 5f;    // �����ٶ�
    public float bodyWaveAmplitude = 0.2f; // ���岨������

    private Chain spine;
    private LineRenderer lineRenderer;

    void Start()
    {
        // ��ʼ����ʽ�ṹ
        spine = gameObject.AddComponent<Chain>();
        spine.segmentCount = 12;
        spine.segmentLength = 0.3f;
        spine.InitializeChain(transform.position);

        // ����������Ⱦ
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = spine.segmentCount;
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.1f;
    }

    void Update()
    {
        // ���λ��׷��
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // ƽ���ƶ�ͷ��
        Vector3 targetPos = Vector3.Lerp(
            spine.joints[0],
            mousePos,
            Time.deltaTime * followSpeed
        );

        // ������ʽ�˶�
        spine.Resolve(targetPos);

        // ������岨��Ч��
        ApplyBodyWave();

        // ����������Ⱦ
        lineRenderer.SetPositions(spine.joints.ToArray());
    }

    // ������岨��
    private void ApplyBodyWave()
    {
        for (int i = 0; i < spine.joints.Count; i++)
        {
            float waveOffset = Mathf.Sin(Time.time * 3f + i * 0.5f) * bodyWaveAmplitude;
            spine.joints[i] += new Vector3(waveOffset, 0, 0);
        }
    }
}