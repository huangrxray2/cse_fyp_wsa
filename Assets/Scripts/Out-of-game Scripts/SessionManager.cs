using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    // 当前登录账号
    public string CurrentUsername { get; private set; } = "";

    // 存储客户端与用户名的映射
    private Dictionary<ulong, string> clientIdToUsernameMap = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentUser(string username)
    {
        CurrentUsername = username;
        Debug.Log($"SessionManager: 当前玩家： {username}");
    }

    // 为客户端设置用户名（服务器端调用）
    public void SetClientUsername(ulong clientId, string username)
    {
        if (!clientIdToUsernameMap.ContainsKey(clientId))
        {
            clientIdToUsernameMap.Add(clientId, username);
            Debug.Log($"SessionManager: 添加客户端 {clientId} ，玩家名 {username}");
        }
        else
        {
            clientIdToUsernameMap[clientId] = username;
            Debug.Log($"SessionManager: 更新客户端 {clientId} ，玩家名 {username}");
        }
    }

    // 根据 ClientId 获取用户名
    public string GetClientUsername(ulong clientId)
    {
        if (clientIdToUsernameMap.ContainsKey(clientId))
        {
            return clientIdToUsernameMap[clientId];
        }
        Debug.Log("sessionManager：无法getclientusername");
        return null;  // 如果没有找到用户名
    }

    public void ClearSession()
    {
        CurrentUsername = "";
        Debug.Log("SessionManager: 用户会话已清空");
    }
}
