using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理所有兵线的路线
/// </summary>
public enum Lane
{
    Top,
    Mid,
    Bot
}

public class LaneManager : MonoBehaviour
{
    public static LaneManager Instance;

    // 红方和蓝方的路线字典
    public Dictionary<Lane, List<Vector3>> redRoutes = new Dictionary<Lane, List<Vector3>>();
    public Dictionary<Lane, List<Vector3>> blueRoutes = new Dictionary<Lane, List<Vector3>>();

    void Awake()
    {
        // 确保只有一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            InitializeRoutes();
        }
    }

    /// <summary>
    /// 初始化红方和蓝方的路线
    /// </summary>
    private void InitializeRoutes()
    {
        // 红方路线
        redRoutes[Lane.Top] = new List<Vector3>
        {
            new Vector3(-200, 0, -200),
            new Vector3(-200, 0, 200),
            new Vector3(200, 0, 200)
        };

        redRoutes[Lane.Mid] = new List<Vector3>
        {
            new Vector3(-200, 0, -200),
            new Vector3(200, 0, 200)
        };

        redRoutes[Lane.Bot] = new List<Vector3>
        {
            new Vector3(-200, 0, -200),
            new Vector3(200, 0, -200),
            new Vector3(200, 0, 200)
        };

        // 蓝方路线（红方路线的逆向）
        blueRoutes[Lane.Top] = new List<Vector3>
        {
            new Vector3(200, 0, 200),
            new Vector3(-200, 0, 200),
            new Vector3(-200, 0, -200)
        };

        blueRoutes[Lane.Mid] = new List<Vector3>
        {
            new Vector3(200, 0, 200),
            new Vector3(-200, 0, -200)
        };

        blueRoutes[Lane.Bot] = new List<Vector3>
        {
            new Vector3(200, 0, 200),
            new Vector3(200, 0, -200),
            new Vector3(-200, 0, -200)
        };
    }
}
