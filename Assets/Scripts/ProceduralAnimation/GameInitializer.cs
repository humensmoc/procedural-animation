using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    void Start()
    {
        // 创建鱼
        // CreateFish();
        
        // 创建蛇
        CreateSnake();
        
        // 创建蜥蜴
        // CreateLizard();
    }

    private void CreateFish()
    {
        GameObject fishObj = new GameObject("Fish");
        fishObj.AddComponent<LineRenderer>();
        var lineRenderer = fishObj.GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.blue;
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.1f;
        fishObj.AddComponent<Fish>();
        fishObj.transform.position = new Vector3(-5f, 0f, 0f);
    }

    private void CreateSnake()
    {
        GameObject snakeObj = new GameObject("Snake");
        snakeObj.AddComponent<LineRenderer>();
        var lineRenderer = snakeObj.GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.1f;
        snakeObj.AddComponent<Snake>();
        snakeObj.transform.position = new Vector3(0f, 0f, 0f);
    }

    private void CreateLizard()
    {
        GameObject lizardObj = new GameObject("Lizard");
        lizardObj.AddComponent<Lizard>();
        lizardObj.transform.position = new Vector3(5f, 0f, 0f);
    }
} 