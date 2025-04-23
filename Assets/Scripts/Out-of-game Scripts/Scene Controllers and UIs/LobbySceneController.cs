using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

public class LobbySceneController : MonoBehaviour
{
    public Transform contentParent;
    public GameObject roomItemPrefab;

    private void Start()
    {
        // 每隔 5 秒刷新一次房间列表
        InvokeRepeating(nameof(RefreshRooms), 0.5f, 5f);

        // 确保 RoomManager 实例被创建并且 Spawn
        SpawnRoomManager();
    }

    private void SpawnRoomManager()
    {
        RoomManager roomManager = RoomManager.Instance;

        // 如果没有找到 RoomManager，则通过 NetworkObject 进行实例化
        if (roomManager == null)
        {
            Debug.Log("没有找到 RoomManager 实例，创建新的 RoomManager");

            // GameObject roomManagerObject = new GameObject("RoomManager");
            // roomManager = roomManagerObject.AddComponent<RoomManager>();
            // NetworkObject networkObject = roomManagerObject.AddComponent<NetworkObject>();

            // // 仅当对象尚未 Spawn 时才调用 Spawn
            // if (!networkObject.IsSpawned)
            // {
            //     networkObject.Spawn();
            // }
        }
        else
        {
            // 如果已经存在实例，确保它被 Spawn
            NetworkObject networkObject = roomManager.GetComponent<NetworkObject>();
            if (networkObject != null && !networkObject.IsSpawned)
            {
                networkObject.Spawn();
            }
        }
    }

    private void RefreshRooms()
    {
        // 清空旧列表
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // 填充新列表
        foreach (var room in RoomManager.Instance.networkRoomList)
        {
            if (!room.isPublic) continue;

            var go = Instantiate(roomItemPrefab, contentParent);
            var roomItemUI = go.GetComponent<RoomListItemUI>();
            
            // 绑定加入房间的回调
            roomItemUI.OnJoinRoomButtonClicked = OnJoinRoomButton;
            
            // 设置房间信息
            roomItemUI.Setup(room);
        }
    }

    /// <summary>
    /// 创建房间。这里示例中使用默认设置：房间名为空则用房主ID、公开、不需要密码、介绍为空。
    /// </summary>
    public void OnCreateRoomButton()
    {
        if (RoomManager.Instance != null)
        {
            // 调用服务器端 RPC 创建房间
            RoomManager.Instance.CreateRoomServerRpc("", true, "", false, "");
            // 以 Additive 模式加载 GameRoomScene，注意 LobbyScene 保留在后台
            SceneManager.LoadScene("GameRoomScene", LoadSceneMode.Additive);
        }
    }

    /// <summary>
    /// 加入房间。示例中直接加入列表中第一个房间（公开房间，无密码）。
    /// </summary>
    public void OnJoinRoomButton(string roomId)
    {
        Debug.Log($"尝试加入房间，房间ID: {roomId}");

        if (RoomManager.Instance != null && !string.IsNullOrEmpty(roomId))
        {
            // string roomId = RoomManager.Instance.networkRoomList[0].roomId.ToString();
            RoomManager.Instance.JoinRoomServerRpc(roomId, "");
            SceneManager.LoadScene("GameRoomScene", LoadSceneMode.Additive);
        }
        else
        {
            Debug.Log("当前没有可加入的房间");
        }
    }
    
    public void GoToMainMenu()
    {
        SceneManager.UnloadSceneAsync("LobbyScene");
    }
}
