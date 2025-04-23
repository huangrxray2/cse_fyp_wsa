using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public GameObject NetworkControlPanel;    // 房间相关的 UI 面板
    public GameObject inGamePanel;   // 对局内 UI 面板

    [SerializeField]
    private GameObject playerPrefab; // 玩家角色的Prefab

    private Dictionary<ulong, GameObject> playerObjects = new Dictionary<ulong, GameObject>();

    private Vector3 redBasePosition = new Vector3(-200, 0, -200); // 房主 red 的Base位置
    private Vector3 blueBasePosition = new Vector3(200, 0, 200);      // 加入者 blue 的Base位置

    private Vector3[] redHilltopTowerPositions = new Vector3[]
    {
        new Vector3(-200, 0, -105),  // red 上路高地塔
        new Vector3(-150, 0, -150), // red 中路高地塔
        new Vector3(-105, 0, -200) // red 下路高地塔        
    };

    private Vector3[] blueHilltopTowerPositions = new Vector3[]
    {
        new Vector3(105, 0, 200),   // blue 上路高地塔
        new Vector3(150, 0, 150),  // blue 中路高地塔
        new Vector3(200, 0, 105)  // blue 下路高地塔
    };

    private Vector3[] redInnerTowerPositions = new Vector3[]
    {
        new Vector3(-200, 0, -10),   // red 上路内塔
        new Vector3(-105, 0, -105),  // red 中路内塔
        new Vector3(-10, 0, -200)    // red 下路内塔
    };

    private Vector3[] blueInnerTowerPositions = new Vector3[]
    {
        new Vector3(10, 0, 200),  // blue 上路内塔
        new Vector3(105, 0, 105),    // blue 中路内塔
        new Vector3(200, 0, 10)    // blue 下路内塔
    };

    private Vector3[] redOuterTowerPositions = new Vector3[]
    {
        new Vector3(-200, 0, 100),   // red 上路外塔
        new Vector3(-50, 0, -50),  // red 中路外塔
        new Vector3(100, 0, -200)    // red 下路外塔
    };

    private Vector3[] blueOuterTowerPositions = new Vector3[]
    {
        new Vector3(-100, 0, 200),  // blue 上路外塔
        new Vector3(50, 0, 50),    // blue 中路外塔
        new Vector3(200, 0, -100)    // blue 下路外塔
    };

    [SerializeField]
    private GameObject fogOfWarManagerPrefab;

    // // 用于跟踪已连接的玩家数量
    // private List<ulong> connectedClients = new List<ulong>();

    // 用于同步游戏开始的状态
    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);

    // 网络变量表示建筑已初始化
    public NetworkVariable<bool> structuresInitialized = new NetworkVariable<bool>(false);
    
    // 网络变量表示出兵开始
    public NetworkVariable<bool> spawnStarted = new NetworkVariable<bool>(false);

    void Start()
    {
        // NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        // NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        // 在 GameScene 中，服务器端启动倒计时
        if (IsServer)
        {
            // 立即创建玩家和建筑
            CreatePlayersForAllClients();
            structuresInitialized.Value = true;

            // 等待5秒后才允许开始出兵
            StartCoroutine(CountdownAndStartSpawning());

            // StartCoroutine(CountdownAndStartGame());
        }

        // 监听建筑初始化状态变化
        structuresInitialized.OnValueChanged += OnStructuresInitializedChanged;
        
        // 监听出兵开始状态变化
        spawnStarted.OnValueChanged += OnSpawnStartedChanged;

        // // 监听 gameStarted 状态变化（所有客户端都监听）
        // gameStarted.OnValueChanged += OnGameStartedChanged;
    }
    
    // private void OnClientConnected(ulong clientId)
    // {
    //     Debug.Log("A new client connected, id = " + clientId);
    //     connectedClients.Add(clientId);

    //     if (!IsServer) return; // 只有服务器可以创建玩家

    //     if (clientId != NetworkManager.Singleton.LocalClientId) // 避免为房主重复创建玩家
    //     {
    //         CreatePlayer(clientId); // 创建新连接玩家及其结构
    //     }

    //     // 检查是否所有预期玩家都已连接（2人）
    //     if (connectedClients.Count >= 2 && !gameStarted.Value)
    //     {
    //         StartCountdownToStartGame();
    //     }
    // }

    // private void OnClientDisconnected(ulong clientId)
    // {
    //     Debug.Log("A client disconnected, id = " + clientId);
    //     connectedClients.Remove(clientId);

    //     if (IsServer && playerObjects.ContainsKey(clientId))
    //     {
    //         // 获取玩家对象
    //         GameObject playerObject = playerObjects[clientId];
    //         PlayerManager playerManager = playerObject.GetComponent<PlayerManager>();

    //         // 销毁玩家的所有结构
    //         playerManager.DestroyAllStructures();

    //         // 销毁玩家对象本身
    //         NetworkObject playerNetworkObject = playerObject.GetComponent<NetworkObject>();
    //         if (playerNetworkObject.IsSpawned)
    //         {
    //             playerNetworkObject.Despawn(true); // 使用 true 确保销毁对象
    //         }

    //         // 清理引用
    //         playerObjects.Remove(clientId);

    //         Debug.Log($"Cleaned up all objects for client {clientId}.");
    //     }
    // }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // 此处不再根据客户端连接情况创建玩家，而是等待 gameStarted 为 true 后统一创建
    }

    private IEnumerator CountdownAndStartSpawning()
    {
        Debug.Log("倒计时开始：5秒后开始出兵");
        yield return new WaitForSeconds(5f);
        spawnStarted.Value = true;
    }

    /// <summary>
    /// 当建筑初始化状态变化时的回调
    /// </summary>
    private void OnStructuresInitializedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("所有建筑已初始化!");                                 // 这个if语句是否无效？？？？？？？？？？？？？？？？？？？？？
            
            // 显示游戏UI面板
            if (inGamePanel != null)
                inGamePanel.SetActive(true);
        }
    }

    /// <summary>
    /// 当出兵开始状态变化时的回调
    /// </summary>
    private void OnSpawnStartedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("出兵阶段开始!");
            
            // 通知所有玩家开始出兵
            foreach (var player in playerObjects.Values)
            {
                PlayerManager pm = player.GetComponent<PlayerManager>();
                if (pm != null)
                {
                    pm.StartSpawning();
                }
            }
        }
    }

    /// <summary>
    /// 当游戏开始状态变化时，通知玩家开始游戏逻辑
    /// </summary>
    // private void OnGameStartedChanged(bool oldValue, bool newValue)
    // {
    //     if (newValue)
    //     {
    //         Debug.Log("正式开始游戏!");
    //         // 服务器端生成所有玩家对象（双方）并启动各自的游戏逻辑
    //         if (IsServer)
    //         {
    //             CreatePlayersForAllClients();
    //         }

    //         // 通知所有本地客户端启动游戏逻辑
    //         foreach (var player in playerObjects.Values)
    //         {
    //             PlayerManager pm = player.GetComponent<PlayerManager>();
    //             if (pm != null)
    //             {
    //                 pm.StartGame();
    //             }
    //         }
    //         // 隐藏房间 UI
    //         if (NetworkControlPanel != null)
    //             // NetworkControlPanel.SetActive(false); // 暂时先不要隐藏（虽然也还没能做到隐藏）

    //         // 激活对局内 UI
    //         if (inGamePanel != null)
    //             inGamePanel.SetActive(true);
    //     }
    // }

    /// <summary>
    /// 遍历所有已连接客户端，为未生成玩家对象的客户端创建玩家
    /// </summary>
    private void CreatePlayersForAllClients()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerObjects.ContainsKey(clientId))
            {
                CreatePlayer(clientId);
            }
        }

        // 创建迷雾管理器
        if (fogOfWarManagerPrefab != null)
        {
            GameObject fogManagerObj = Instantiate(fogOfWarManagerPrefab);
            NetworkObject fogManagerNetObj = fogManagerObj.GetComponent<NetworkObject>();
            fogManagerNetObj.Spawn();
        }
    }

    /// <summary>
    /// 为指定客户端创建玩家对象和初始化建筑结构
    /// </summary>
    private void CreatePlayer(ulong clientId)
    {
        // 创建Player对象
        GameObject playerObject = Instantiate(playerPrefab);
        NetworkObject playerNetworkObject = playerObject.GetComponent<NetworkObject>();

        // 分配所有权并同步到所有客户端
        playerNetworkObject.SpawnWithOwnership(clientId);

        playerObjects[clientId] = playerObject;

        // 获取PlayerManager组件
        PlayerManager playerManager = playerObject.GetComponent<PlayerManager>();

        // 获取玩家的用户名
        string playerUsername = SessionManager.Instance.GetClientUsername(clientId);
        if (string.IsNullOrEmpty(playerUsername))
        {
            playerUsername = "未知玩家"; // 如果无法找到用户名，使用默认值
        }

        // 分配红方或蓝方的基地和塔
        if (clientId == RoomManager.Instance.GetRoomHostClientId())
        {
            // 房主（红方）
            playerManager.InitializeStructures(redBasePosition, redHilltopTowerPositions, redInnerTowerPositions, redOuterTowerPositions);
        }
        else
        {
            // 加入者（蓝方）
            playerManager.InitializeStructures(blueBasePosition, blueHilltopTowerPositions, blueInnerTowerPositions, blueOuterTowerPositions);
        }

        // 在初始化建筑后，立即初始化资源
        playerManager.InitializeResources();

        Debug.Log($"创建玩家，其客户端ID为 {clientId} ，用户名为 {playerUsername} ，并分配建筑");
    }

    // public void OnCreateRoomButtonClick()
    // {
    //     if (NetworkManager.Singleton.StartHost())
    //     {
    //         Debug.Log("Create room success.");
    //     }
    //     else
    //     {
    //         Debug.Log("Create room failed.");
    //     }
    // }

    // public void OnJoinRoomButtonClick()
    // {
    //     if (NetworkManager.Singleton.StartClient())
    //     {
    //         Debug.Log("Join room success.");
    //     }
    //     else
    //     {
    //         Debug.Log("Join room failed.");
    //     }
    // }

    public void OnLeaveButtonClick()
    {
        if (RoomManager.Instance != null && RoomManager.Instance.networkRoomList.Count > 0)
        {
            // 这里假设当前玩家在第一个房间中（演示用）
            var room = RoomManager.Instance.networkRoomList[0];
            RoomManager.Instance.LeaveRoomServerRpc(room.roomId.ToString());
        }
        // 这个断开连接只是之前测试用的，现在不再断开。
        // NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene");
        Debug.Log("离开房间并加载主菜单");
    }
}
