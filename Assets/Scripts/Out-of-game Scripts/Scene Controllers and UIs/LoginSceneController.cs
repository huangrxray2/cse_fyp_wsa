using System.Collections;
using System.Collections.Generic;
using TMPro;  // 引入 TextMeshPro 命名空间
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Networking;

public class LoginSceneController : MonoBehaviour
{
    public TMP_InputField usernameInputField;
    public TMP_InputField passwordInputField;
    public TMP_Text feedbackText;

    public void Login()
    {
        string username = usernameInputField.text;
        string password = passwordInputField.text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "用户名或密码不能为空！";
            return;
        }

        // 直接通过单例访问 AuthManager
        AuthManager authManager = AuthManager.Instance;

        // if (authManager == null)
        // {
        //     feedbackText.text = "未找到 AuthManager 实例！";
        //     return;
        // }

        // // 确保 AuthManager 已经 Spawn
        // // 跨场景对象必须要手动再次spawn????????????????????????
        // NetworkObject networkObject = authManager.GetComponent<NetworkObject>();
        // if (networkObject != null && !networkObject.IsSpawned)
        // {
        //     networkObject.Spawn();
        // }

        // 继续调用登录逻辑
        authManager.LoginServerRpc(username, password);
    }

    // 修改后的退出方法
    public void GoToStartUp()
    {
        // 尝试找到 NetworkLauncher 对象
        NetworkLauncher networkLauncher = FindObjectOfType<NetworkLauncher>();
        if (networkLauncher != null)
        {
            networkLauncher.UpdateUIForReturningScenes();
        }
        else
        {
            Debug.LogError("未找到 NetworkLauncher 对象，回退加载 StartUpScene");
        }
        // 卸载当前登录场景，返回到原本加载的 StartUpScene
        SceneManager.UnloadSceneAsync("LoginScene");
    }
}