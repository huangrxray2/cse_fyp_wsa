using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class GameRoomSceneController : MonoBehaviour
{
    public TMP_Text roomInfoText;           // 显示房间基本信息
    public GameObject hostManagementPanel;  // 仅房主显示的管理面板
    public Transform playerListContent;     // 玩家列表的父对象（用于动态显示玩家信息）
    public GameObject playerInfo;           // 用于显示玩家信息的Prefab（包括TextMeshPro组件）
    public Button startGameButton;          // 开始游戏按钮
    public Button leaveRoomButton;          // 离开房间按钮

    private void OnEnable()
    {
        // 订阅网络列表变化事件
        if (RoomManager.Instance != null && RoomManager.Instance.networkRoomList != null)
        {
            RoomManager.Instance.networkRoomList.OnListChanged += OnRoomListChanged;
        }
    }

    private void OnDisable()
    {
        // 取消订阅
        if (RoomManager.Instance != null && RoomManager.Instance.networkRoomList != null)
        {
            RoomManager.Instance.networkRoomList.OnListChanged -= OnRoomListChanged;
        }
    }

    private IEnumerator Start()
    {
        // 确保按钮绑定了对应事件
        startGameButton.onClick.AddListener(OnStartGameButton);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomButton);

        // 等待 RoomManager 和同步数据可用（最多等待 5 秒，根据实际情况调整）
        float timeout = 5f;
        float timer = 0f;
        while ((RoomManager.Instance == null || RoomManager.Instance.networkRoomList.Count == 0) && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        UpdateRoomInfo();
    }

    /// <summary>
    /// 当房间列表发生变化时，调用此方法更新房间信息
    /// </summary>
    /// <param name="changeEvent">网络列表变化事件参数</param>
    private void OnRoomListChanged(NetworkListEvent<RoomInfoNetwork> changeEvent)
    {
        // 更新 UI（注意：在 UI 更新前可以加入一些过滤逻辑，例如只更新当前玩家所在房间的 UI）
        UpdateRoomInfo();
    }

    /// <summary>
    /// 更新并显示房间信息。这里简单假设取 networkRoomList 中第一个房间。
    /// 实际中应根据当前玩家所在房间做匹配。
    /// </summary>
    public void UpdateRoomInfo()
    {
        if (RoomManager.Instance != null && RoomManager.Instance.networkRoomList.Count > 0)
        {
            var room = RoomManager.Instance.networkRoomList[0];
            // roomInfoText.text = $"房间名: {room.roomName.ToString()}\t" +
            //                     $"类型: {(room.isPublic ? "公开" : "私密")}\n" +
            //                     $"房主: {room.roomHostUsername.ToString()}\t" +
            //                     $"当前人数: {room.currentPlayers}/{room.maxPlayers}\n" +
            //                     $"介绍: {room.roomDescription.ToString()}\n";

            roomInfoText.text = $"Room Name: {room.roomName.ToString()}\t" +
                                $"Room Type: {(room.isPublic ? "public" : "private")}\n" +
                                $"Room Host: {room.roomHostUsername.ToString()}\t" +
                                $"Num of People: {room.currentPlayers}/{room.maxPlayers}\n" +
                                $"Intro: {room.roomDescription.ToString()}\n";

            // 显示房主和其他玩家信息
            DisplayPlayerInfo(room);

            // 如果当前用户是房主，则显示管理面板
            if (SessionManager.Instance.CurrentUsername == room.roomHostUsername.ToString())
            {
                hostManagementPanel.SetActive(true);
            }
            else
            {
                hostManagementPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 显示房主和其他玩家的信息
    /// </summary>
    private void DisplayPlayerInfo(RoomInfoNetwork room)
    {
        // 清空旧的玩家信息
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // 创建房主的玩家信息
        GameObject hostInfo = Instantiate(playerInfo, playerListContent);
        TMP_Text hostText = hostInfo.GetComponentInChildren<TMP_Text>();
        if(hostText != null)
        {
            // hostText.text = $"房主：{room.roomHostUsername.ToString()}";
            hostText.text = $"Room Host: {room.roomHostUsername.ToString()}";
        }
        else
        {
            Debug.LogError("无法找到 hostInfo 子物体中的 TMP_Text 组件");
        }

        // 显示加入者（如果有）
        if (!room.joiner.Equals(default(FixedString128Bytes)))
        {
            GameObject joinerInfo = Instantiate(playerInfo, playerListContent);
            TMP_Text joinerText = joinerInfo.GetComponentInChildren<TMP_Text>();
            // joinerText.text = $"加入者：{room.joiner.ToString()}";
            joinerText.text = $"Joiner: {room.joiner.ToString()}";
        }
        else
        {
            // Debug.LogError("无法找到加入者？");
        }
    }

    /// <summary>
    /// 离开房间，调用 LeaveRoomServerRpc 由服务器更新房间数据，然后卸载 GameRoomScene。
    /// </summary>
    public void OnLeaveRoomButton()
    {
        if (RoomManager.Instance != null && RoomManager.Instance.networkRoomList.Count > 0)
        {
            // 这里假设认为当前玩家在第一个房间中
            var room = RoomManager.Instance.networkRoomList[0];
            RoomManager.Instance.LeaveRoomServerRpc(room.roomId.ToString());
        }
        SceneManager.UnloadSceneAsync("GameRoomScene");
    }

    /// <summary>
    /// 开始游戏按钮点击（仅房主可用）
    /// </summary>
    public void OnStartGameButton()
    {
        if (RoomManager.Instance != null && RoomManager.Instance.networkRoomList.Count > 0)
        {
            // 取第一个房间（演示中只有这一个房间）
            var room = RoomManager.Instance.networkRoomList[0];
            // 判断当前玩家是否为房主
            if (SessionManager.Instance.CurrentUsername == room.roomHostUsername.ToString())
            {
                // 房主调用服务器 RPC 启动游戏
                RoomManager.Instance.StartGameServerRpc();
            }
            else
            {
                Debug.LogWarning("只有房主可以启动游戏！");
            }
        }
    }
    
    public void TestGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
