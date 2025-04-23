using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AuthManager : NetworkBehaviour
{
    public static AuthManager Instance { get; private set; }

    // 内存中的用户数据，服务器启动时加载持久化数据
    private Dictionary<string, string> registeredUsers = new Dictionary<string, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Debug.Log("AuthManager 实例化成功");
        }
        else
        {
            // Debug.Log("AuthManager 实例已存在，销毁当前实例");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (IsSpawned)
        {
            // Debug.Log("AuthManager 已成功注册到网络！");
        }
        else
        {
            // Debug.Log("AuthManager 没有成功注册到网络！");
        }
    }

    // 在服务器启动时加载用户数据
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // // onnetspawn方法本身就是spawn后才会回调的，这里多余了
        // if (!IsSpawned)
        // {
        //     NetworkObject networkObject = GetComponent<NetworkObject>();
        //     if (networkObject != null && !networkObject.IsSpawned)
        //     {
        //         networkObject.Spawn();
        //     }
        // }
        
        if (IsServer)
        {
            // 加载持久化数据
            UserDatabase db = UserDatabaseHelper.LoadDatabase();
            foreach (var user in db.users)
            {
                if (!registeredUsers.ContainsKey(user.username))
                {
                    registeredUsers.Add(user.username, user.password);
                }
            }
            Debug.Log($"从离线数据库中加载了 {registeredUsers.Count} 个用户.");
        }
    }

    // 注册请求：服务器端检查并保存用户
    [ServerRpc(RequireOwnership = false)]
    public void RegisterServerRpc(string username, string password, ServerRpcParams rpcParams = default)
    {
        if (registeredUsers.ContainsKey(username))
        {
            // 用户已存在，返回错误
            RegisterResponseClientRpc(false, "账号已存在", "", rpcParams.Receive.SenderClientId);
        }
        else
        {
            // 添加用户，并持久化保存
            registeredUsers.Add(username, password);
            SaveUsers(); // 更新文件
            RegisterResponseClientRpc(true, "success", "", rpcParams.Receive.SenderClientId);
        }
    }

    // 登录请求：服务器端验证账号密码
    [ServerRpc(RequireOwnership = false)]
    public void LoginServerRpc(string username, string password, ServerRpcParams rpcParams = default)
    {
        if (!registeredUsers.ContainsKey(username))
        {
            LoginResponseClientRpc(false, "账号不存在", "", rpcParams.Receive.SenderClientId);
        }
        else if (registeredUsers[username] != password)
        {
            LoginResponseClientRpc(false, "密码错误", "", rpcParams.Receive.SenderClientId);
        }
        else
        {
            // 登录成功后，在服务器端更新客户端的映射
            SessionManager.Instance.SetClientUsername(rpcParams.Receive.SenderClientId, username);
            LoginResponseClientRpc(true, "success", username, rpcParams.Receive.SenderClientId);
        }
    }

    // 反馈注册结果给指定客户端
    [ClientRpc]
    private void RegisterResponseClientRpc(bool success, string message, string username, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            AuthUIManager.Instance.HandleRegisterResponse(success, message);
        }
    }

    // 反馈登录结果给指定客户端
    [ClientRpc]
    private void LoginResponseClientRpc(bool success, string message, string username, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            // 登录成功后更新 SessionManager 的当前用户
            if (success && SessionManager.Instance != null)
            {
                SessionManager.Instance.SetCurrentUser(username);
                // // 将客户端ID和用户名映射到 SessionManager
                // SessionManager.Instance.SetClientUsername(targetClientId, username);
            }
            AuthUIManager.Instance.HandleLoginResponse(success, message);
        }
    }

    // 保存当前注册用户到持久化文件
    private void SaveUsers()
    {
        UserDatabase db = new UserDatabase();
        foreach (var kv in registeredUsers)
        {
            db.users.Add(new UserInfo { username = kv.Key, password = kv.Value });
        }
        UserDatabaseHelper.SaveDatabase(db);
        Debug.Log("User database saved.");
    }
}
