using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSceneController : MonoBehaviour
{
    public TMP_Text currentUserText; // 在 Inspector 中绑定，用于显示当前用户

    private void Start()
    {
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.CurrentUsername))
        {
            // currentUserText.text = "当前玩家：" + SessionManager.Instance.CurrentUsername;
            currentUserText.text = "Player: " + SessionManager.Instance.CurrentUsername;
        }
        else
        {
            currentUserText.text = "未登录";
        }
    }

    public void GoToLobby()
    {
        SceneManager.LoadScene("LobbyScene", LoadSceneMode.Additive);
    }

    // 跳转到注册场景
    public void GoToLeaderBoard()
    {
        SceneManager.LoadScene("LeaderBoardScene");
    }

    // 退出游戏
    public void GoToShop()
    {
        SceneManager.LoadScene("ShopScene");
    }
    public void GoToSettings()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    /// <summary>
    /// 退出登录，返回 StartUpScene。
    /// 如果当前运行模式为客户端，则断开客户端连接；
    /// 如果为主机，则关闭客户端连接并关闭服务器。
    /// </summary>
    public void GoToStartUp()
    {
        // 如果网络管理器存在，则根据当前运行模式断开连接
        if (NetworkManager.Singleton != null)
        {
            // 作为服务器模式不可能到达这里
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("当前为主机模式，关闭主机（同时断开客户端连接和关闭服务器）");
                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("当前为客户端模式，断开客户端连接");
                NetworkManager.Singleton.Shutdown();
            }
        }

        // 清除会话信息
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ClearSession();
        }

        // 跳转回启动场景
        SceneManager.LoadScene("StartUpScene");
    }
}
