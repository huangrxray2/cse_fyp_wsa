using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthUIManager : MonoBehaviour
{
    public static AuthUIManager Instance;

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

    // 处理注册结果
    public void HandleRegisterResponse(bool success, string message)
    {
        if (success)
        {
            // 注册成功后跳转到登录场景
            SceneManager.LoadScene("LoginScene", LoadSceneMode.Additive);
        }
        else
        {
            // 在注册场景中显示错误信息
            RegisterSceneController controller = FindObjectOfType<RegisterSceneController>();
            if (controller != null)
            {
                controller.feedbackText.text = "注册失败：" + message;
            }
        }
    }

    // 处理登录结果
    public void HandleLoginResponse(bool success, string message)
    {
        if (success)
        {
            // 登录成功后跳转到主菜单
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            LoginSceneController controller = FindObjectOfType<LoginSceneController>();
            if (controller != null)
            {
                controller.feedbackText.text = "登录失败：" + message;
            }
        }
    }
}
