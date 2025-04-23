using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartUpSceneController : MonoBehaviour
{
    // public NetworkLauncher networkLauncher;

    // public Button startServerButton;
    // public Button startClientButton;
    // public Button startHostButton;
    // public Button exitButton;

    private void Start()
    {
        /* // 不应该在客户端加载NetworkScene
        // Additive 加载 NetworkScene，确保网络逻辑场景始终存在
        SceneManager.LoadScene("NetworkScene", LoadSceneMode.Additive);
        */
    }
    
    // 跳转到登录场景
    public void GoToLogin()
    {
        SceneManager.LoadScene("LoginScene", LoadSceneMode.Additive);
    }

    // 跳转到注册场景
    public void GoToRegister()
    {
        SceneManager.LoadScene("RegisterScene", LoadSceneMode.Additive);
    }

    public void TestLoginSuc()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public void TestGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
