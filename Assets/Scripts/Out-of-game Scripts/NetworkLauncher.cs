using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkLauncher : MonoBehaviour
{
    public static NetworkLauncher Instance { get; private set; }

    [Header("启动选项面板与状态显示")]
    public GameObject startUpOptionsPanel; // 存放“启动服务器”、“启动客户端”、“启动主机”按钮的面板
    public TMP_Text statusText; // 用于显示当前启动模式提示
    
    [Header("UI 引用")]
    // 引用 StartupCanvas 中 StartUpPanel 下的按钮
    public Button startServerButton;
    public Button startClientButton;
    public Button startHostButton;
    public Button loginButton;      // 登录按钮（仅客户端/主机模式下显示）
    public Button registerButton;   // 注册按钮（仅客户端/主机模式下显示）
    public Button exitButton;       // 退出按钮（始终显示，用于关闭服务器/客户端）
    
    // 如果你希望整个面板在服务器模式下部分禁用，也可以引用面板对象
    // public GameObject startupPanel;

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

        // // 确保 NetworkManager 是唯一的
        // if (NetworkManager.Singleton != null)
        // {
        //     // 如果 NetworkManager.Singleton 已经存在，销毁它
        //     Destroy(NetworkManager.Singleton.gameObject);
        // }

        // // 现在创建新的 NetworkManager（如果还没有实例化）
        // if (NetworkManager.Singleton == null)
        // {
        //     GameObject networkManagerObject = new GameObject("NetworkManager");
        //     networkManagerObject.AddComponent<NetworkManager>();
        //     DontDestroyOnLoad(networkManagerObject); // 保持它在场景切换时不销毁
        // }

        // 订阅场景加载回调，以便在 StartupScene 加载后自动更新按钮引用
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Debug.Log("[NetworkLauncher] Awake: 注册了 sceneLoaded 事件");

        // 初始 UI 状态
        if (startUpOptionsPanel != null)
            startUpOptionsPanel.SetActive(true);
        if (statusText != null)
            // statusText.text = "请选择启动模式";
            statusText.text = "Select Mode";
        if (loginButton != null)
            loginButton.gameObject.SetActive(false);
        if (registerButton != null)
            registerButton.gameObject.SetActive(false);

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
            // Debug.Log("[NetworkLauncher] OnSceneLoaded: 确保 ExitButton 显示");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 暂时先不绑定前几个了
        // // 确保所有 UI 引用重新绑定
        // if (startUpOptionsPanel == null) 
        //     startUpOptionsPanel = GameObject.Find("StartUpOptionsPanel");
        // if (statusText == null)
        //     statusText = GameObject.Find("StatusText").GetComponent<TMP_Text>();
        // if (startServerButton == null)
        //     startServerButton = GameObject.Find("StartServerButton").GetComponent<Button>();
        // if (startClientButton == null)
        //     startClientButton = GameObject.Find("StartClientButton").GetComponent<Button>();
        // if (startHostButton == null)
        //     startHostButton = GameObject.Find("StartHostButton").GetComponent<Button>();
        // 重新加载场景后找不到登录注册退出按钮了？？？？？？？？？？？？？？？？？？？？？
        // if (loginButton == null)
        //     loginButton = GameObject.Find("LoginButton").GetComponent<Button>();
        // if (registerButton == null)
        //     registerButton = GameObject.Find("RegisterButton").GetComponent<Button>();
        // if (exitButton == null)
        //     exitButton = GameObject.Find("ExitButton").GetComponent<Button>();

        // 暂时不监听
        // if (startServerButton != null)
        // {
        //     startServerButton.onClick.AddListener(StartServer);
        // }
        // if (startClientButton != null)
        // {
        //     startClientButton.onClick.AddListener(StartClient);
        // }
        // if (startHostButton != null)
        // {
        //     startHostButton.onClick.AddListener(StartHost);
        // }

        // 重新加载场景后找不到登录注册退出按钮了？？？？？？？？？？？？？？？？？？？？？
        // if (loginButton != null)
        // {
        //     loginButton.onClick.AddListener(???);
        // }
        // if (registerButton != null)
        // {
        //     registerButton.onClick.AddListener(???);
        // }
        // if (exitButton != null)
        // {
        //     exitButton.onClick.AddListener(???);
        // }

        // 更新退出按钮
        if (exitButton != null)
        {
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
            {
                exitButton.GetComponentInChildren<TMP_Text>().text = "重新选择启动模式/Restart";
            }
            else
            {
                exitButton.GetComponentInChildren<TMP_Text>().text = "退出/Exit";
            }
        }
    }

    // 似乎没什么用
    private void OnDestroy()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 以服务器模式启动：只开启服务器，不连接客户端。
    /// 隐藏登录和注册按钮，保留退出按钮。
    /// </summary>
    public void StartServer()
    {
        Debug.Log("尝试启动服务器");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartServer();
            // // 隐藏登录和注册按钮，防止服务器上产生客户端操作
            // Debug.Log("Server started. Login and Register buttons are hidden.");
            // UpdateUIForServerMode("Server 模式已启动");
            UpdateUIForClientHostMode("Start as Server");
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton is null!");
        }
    }

    /// <summary>
    /// 以客户端模式启动：仅作为客户端连接服务器。
    /// UI 保持完整，但不提供服务器端的后续功能。
    /// </summary>
    public void StartClient()
    {
        Debug.Log("尝试启动客户端");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            // // 客户端模式下，所有按钮都显示
            // Debug.Log("Client started. All UI elements are available.");
            // UpdateUIForClientHostMode("Client 模式已启动");
            UpdateUIForClientHostMode("Start as Client");
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton is null!");
        }
    }

    /// <summary>
    /// 以主机模式启动：同时具备服务器和客户端功能。
    /// UI 保持完整，拥有服务器端和客户端的所有功能。
    /// </summary>
    public void StartHost()
    {
        Debug.Log("尝试启动主机");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            // // 主机模式下，所有按钮都显示（因为既能操作UI，也能进行数据存储、计算、同步等服务器功能）
            // Debug.Log("Host started. All UI elements are available.");
            // UpdateUIForClientHostMode("Host 模式已启动");
            UpdateUIForClientHostMode("Start as Host");
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton is null!");
        }
    }
    
    /// <summary>
    /// 更新 UI 为服务器模式：隐藏登录和注册按钮
    /// </summary>
    private void UpdateUIForServerMode(string modeInfo)
    {
        if (startUpOptionsPanel != null)
            startUpOptionsPanel.SetActive(false);
        if (statusText != null)
            statusText.text = modeInfo;
        if (loginButton != null)
            loginButton.gameObject.SetActive(false);
        if (registerButton != null)
            registerButton.gameObject.SetActive(false);
        // 更新退出按钮文本为“重新选择启动方式/Restart”
        if (exitButton != null)
            exitButton.GetComponentInChildren<TMP_Text>().text = "重新选择启动模式/Restart";
    }

    /// <summary>
    /// 更新 UI 为客户端或主机模式：显示登录和注册按钮
    /// </summary>
    private void UpdateUIForClientHostMode(string modeInfo)
    {
        if (startUpOptionsPanel != null)
            startUpOptionsPanel.SetActive(false);
        if (statusText != null)
            statusText.text = modeInfo;
        if (loginButton != null)
            loginButton.gameObject.SetActive(true);
        if (registerButton != null)
            registerButton.gameObject.SetActive(true);
        // 更新退出按钮文本为“重新选择启动方式/Restart”
        if (exitButton != null)
            exitButton.GetComponentInChildren<TMP_Text>().text = "重新选择启动模式/Restart";
    }

    public void UpdateUIForReturningScenes()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // UpdateUIForClientHostMode("Host 模式已启动");
                UpdateUIForClientHostMode("Start as Host");
                Debug.Log("作为 Host 取消登录/注册");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                // UpdateUIForClientHostMode("Client 模式已启动");
                UpdateUIForClientHostMode("Start as Client");
                Debug.Log("作为 Client 取消登录/注册");
            }
        }
    }

    /// <summary>
    /// 退出按钮调用方法：根据当前网络状态关闭服务器、主机或客户端。
    /// </summary>
    public void Exit()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                // 关闭服务器或主机
                NetworkManager.Singleton.Shutdown();
                Debug.Log("服务器/主机断开");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                // 关闭客户端
                NetworkManager.Singleton.Shutdown();
                Debug.Log("客户端断开");
            }
        }

        // 清除会话信息（如果有的话）
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ClearSession();
        }

        // 如果没有网络模式（既不是客户端、主机也不是服务器），则退出游戏
        if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost))
        {
            // 退出游戏（在编辑器下无效，构建后有效）
            Debug.Log("退出游戏");
            Application.Quit();
        }

        // 如果当前为网络模式，返回到启动选择界面，此时 OnSceneLoaded 会重置 UI 为初始状态（显示启动选项面板、状态文本置空、按钮文本恢复为“退出/Exit”）
        else
        {
            // 恢复初始 UI 状态
            if (startUpOptionsPanel != null)
                startUpOptionsPanel.SetActive(true);
            if (statusText != null)
                statusText.text = "请选择启动模式";
            if (loginButton != null)
                loginButton.gameObject.SetActive(false);
            if (registerButton != null)
                registerButton.gameObject.SetActive(false);
            exitButton.GetComponentInChildren<TMP_Text>().text = "退出/Exit";

            // SceneManager.UnloadSceneAsync("NetworkScene");
        }
    }
}
