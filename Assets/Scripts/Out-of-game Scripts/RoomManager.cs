using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct RoomInfoNetwork : INetworkSerializable, IEquatable<RoomInfoNetwork>
{
    public FixedString128Bytes roomId;              // 房间唯一ID（用 Guid 生成）
    public FixedString128Bytes roomHostUsername;    // 房主用户名，注意，不是主机用户名
    public ulong roomHostClientId;                  // 房主的 ClientId，注意，不是主机的ClientId
    public FixedString128Bytes roomName;            // 房间名字
    public bool isPublic;                           // 是否公开
    public FixedString128Bytes roomPassword;        // 房间密码（私密房用）
    public bool joinRequestRequired;                // 是否需要申请加入
    public FixedString128Bytes roomDescription;     // 房间介绍
    public int currentPlayers;                      // 当前人数
    public int maxPlayers;                          // 最大人数

    // 加入者，不包含房主（对于 maxPlayers=2，这里只需要一个加入者字段）
    public FixedString128Bytes joiner;         // 如果没有加入者，则默认为空


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
         serializer.SerializeValue(ref roomId);
         serializer.SerializeValue(ref roomHostUsername);
         serializer.SerializeValue(ref roomHostClientId);
         serializer.SerializeValue(ref roomName);
         serializer.SerializeValue(ref isPublic);
         serializer.SerializeValue(ref roomPassword);
         serializer.SerializeValue(ref joinRequestRequired);
         serializer.SerializeValue(ref roomDescription);
         serializer.SerializeValue(ref currentPlayers);
         serializer.SerializeValue(ref maxPlayers);
         serializer.SerializeValue(ref joiner); // 添加此行，确保 joiner 被同步
    }

    public bool Equals(RoomInfoNetwork other)
    {
        // 这里认为 roomId 是唯一标识
        return roomId.Equals(other.roomId);
    }

    public override bool Equals(object obj)
    {
        return obj is RoomInfoNetwork other && Equals(other);
    }

    public override int GetHashCode()
    {
        return roomId.GetHashCode();
    }
}

public class RoomManager : NetworkBehaviour
{
    public static RoomManager Instance { get; private set; }

    // 使用 NetworkList 同步房间数据到所有客户端
    public NetworkList<RoomInfoNetwork> networkRoomList;

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

        // 确保 networkRoomList 被初始化
        if (networkRoomList == null)
        {
            networkRoomList = new NetworkList<RoomInfoNetwork>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) // 服务器端或主机才初始化 NetworkList
        {
            
        }
    }

    /// <summary>
    /// 客户端调用此方法创建房间，参数为房间设置（房间名、公开/私密、密码、申请加入、介绍）。
    /// 服务器端创建后会通过 ClientRpc 通知调用者（此处简单输出日志）。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CreateRoomServerRpc(string roomName, bool isPublic, string roomPassword, bool joinRequestRequired, string roomDescription, ServerRpcParams rpcParams = default)
    {
        // 获取 RPC 调用者的 ClientId
        ulong senderId = rpcParams.Receive.SenderClientId;
        // Debug.Log($"[RoomManager] RPC 调用者 ClientId: {senderId}");

        // 获取客户端用户名
        string clientUsername = SessionManager.Instance.GetClientUsername(senderId);
        if (string.IsNullOrEmpty(clientUsername))
        {
            Debug.LogError("[RoomManager] 无法根据 ClientId 获取用户名!");
            return;
        }

        // Debug.Log($"[RoomManager] RPC 调用者用户名: {clientUsername}");

        RoomInfoNetwork newRoom = new RoomInfoNetwork();
        newRoom.roomHostUsername = new FixedString128Bytes(clientUsername);  // 设置房主用户名为客户端的用户名
        newRoom.roomHostClientId = senderId; // 记录房主的 ClientId
        newRoom.roomId = new FixedString128Bytes(Guid.NewGuid().ToString());
        newRoom.roomName = string.IsNullOrEmpty(roomName) ? new FixedString128Bytes(clientUsername) : new FixedString128Bytes(roomName);
        newRoom.isPublic = isPublic;
        newRoom.roomPassword = new FixedString128Bytes(roomPassword);
        newRoom.joinRequestRequired = joinRequestRequired;
        newRoom.roomDescription = new FixedString128Bytes(roomDescription);
        newRoom.currentPlayers = 1;
        newRoom.maxPlayers = 2;
        newRoom.joiner = default; // 初始化为空

        networkRoomList.Add(newRoom);
        Debug.Log($"（服务器端）[RoomManager] 房间【{newRoom.roomName.ToString()}】创建成功，ID：{newRoom.roomId.ToString()}，房主：{newRoom.roomHostUsername.ToString()}");
        
        // 通知客户端创建结果
        CreateRoomResultClientRpc(true, newRoom.roomId.ToString(), "房间创建成功", rpcParams.Receive.SenderClientId);
        NotifyClientsOfRoomUpdateClientRpc(); // 通知所有客户端房间更新
    }

    public ulong GetRoomHostClientId() // 获取房主（不是主机！）的ClientId
    {
        if (networkRoomList.Count > 0)
        {
            return networkRoomList[0].roomHostClientId; // 演示中假设只有这一个房间
        }
        return 0;
    }

    // ClientRpc：通知所有客户端房间信息已更新
    [ClientRpc]
    public void NotifyClientsOfRoomUpdateClientRpc()
    {
        // 确保客户端在更新 UI 时能接收到最新的房间信息
        GameRoomSceneController[] sceneControllers = FindObjectsOfType<GameRoomSceneController>();
        foreach (var controller in sceneControllers)
        {
            controller.UpdateRoomInfo();
        }
    }

    [ClientRpc]
    public void CreateRoomResultClientRpc(bool success, string roomId, string message, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log($"（客户端）[RoomManager] 创建结果: {success}, RoomID: {roomId}, Message: {message}");
            // 可在客户端更新 UI 显示结果（例如提示、跳转等）
        }
    }

    /// <summary>
    /// 客户端调用此方法加入房间（传入房间ID及提供的密码）。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void JoinRoomServerRpc(string roomId, string providedPassword, ServerRpcParams rpcParams = default)
    {
        bool success = false;
        string msg = "";
        for (int i = 0; i < networkRoomList.Count; i++)
        {
            if (networkRoomList[i].roomId.ToString() == roomId)
            {
                RoomInfoNetwork room = networkRoomList[i];
                if (!room.isPublic)
                {
                    if (room.roomPassword.ToString() != providedPassword)
                    {
                        msg = "密码错误";
                        break;
                    }
                }
                if (room.currentPlayers < room.maxPlayers)
                {
                    // 增加人数
                    room.currentPlayers++;

                    // 获取加入者的用户名
                    string joinerUsername = SessionManager.Instance.GetClientUsername(rpcParams.Receive.SenderClientId);
                    if (!string.IsNullOrEmpty(joinerUsername))
                    {
                        room.joiner = new FixedString128Bytes(joinerUsername);
                    }
                    else
                    {
                        Debug.LogError("[RoomManager] 加入房间时无法获取用户名!");
                    }

                    networkRoomList[i] = room; // 更新网络列表中对应项
                    success = true;
                    msg = "加入房间成功";

                    NotifyClientsOfRoomUpdateClientRpc(); // 通知所有客户端更新房间数据
                }
                else
                {
                    msg = "房间已满";
                }
                break;
            }
        }
        JoinRoomResultClientRpc(success, roomId, msg, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    public void JoinRoomResultClientRpc(bool success, string roomId, string message, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log($"（客户端）[RoomManager] 加入结果: {success}, 房间ID: {roomId}, 信息: {message}");
            // 客户端可据此跳转到 GameRoomScene（例如由 LobbyController 调用 LoadScene）
        }
    }

    /// <summary>
    /// 客户端调用此方法离开房间（传入房间ID）。
    /// 若当前用户为房主，则移除整个房间；否则减少人数。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void LeaveRoomServerRpc(string roomId, ServerRpcParams rpcParams = default)
    {
        for (int i = 0; i < networkRoomList.Count; i++)
        {
            if (networkRoomList[i].roomId.ToString() == roomId)
            {
                var room = networkRoomList[i];
                if (SessionManager.Instance.CurrentUsername == room.roomHostUsername.ToString())
                {
                    // 房主离开则删除房间
                    networkRoomList.RemoveAt(i);
                    Debug.Log($"[RoomManager] 房主离开，房间 {room.roomName.ToString()} 被删除");
                }
                else
                {
                    room.currentPlayers = Mathf.Max(room.currentPlayers - 1, 0);
                    // 若离开的玩家是加入者，则清空 joiner 字段（注意：对于2人房间，只会有1个加入者）
                    room.joiner = default;
                    networkRoomList[i] = room;
                    Debug.Log($"[RoomManager] 玩家离开，房间 {room.roomName.ToString()} 当前人数更新为 {room.currentPlayers}");
                }
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("服务器收到启动游戏请求，开始切换场景到 GameScene");
        // 使用网络场景管理器加载 GameScene，LoadSceneMode.Single 会卸载当前所有激活场景
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}
