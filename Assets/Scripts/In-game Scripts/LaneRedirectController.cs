using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LaneRedirectController : MonoBehaviour
{
    [Header("上路转线按钮")]
    [SerializeField] private Button topToTopButton;
    [SerializeField] private Button topToMidButton;
    [SerializeField] private Button topToBotButton;
    
    [Header("中路转线按钮")]
    [SerializeField] private Button midToTopButton;
    [SerializeField] private Button midToMidButton;
    [SerializeField] private Button midToBotButton;
    
    [Header("下路转线按钮")]
    [SerializeField] private Button botToTopButton;
    [SerializeField] private Button botToMidButton;
    [SerializeField] private Button botToBotButton;
    
    // 当前玩家的PlayerManager引用
    private PlayerManager localPlayerManager;
    
    void Awake()
    {
        // 订阅本地玩家生成事件
        PlayerManager.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
    }
    
    void Start()
    {
        // 立即注册按钮事件，即使PlayerManager还没准备好
        RegisterButtonEvents();
    }
    
    void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
    }
    
    private void HandleLocalPlayerSpawned(PlayerManager playerManager)
    {
        localPlayerManager = playerManager;
        
        // 注册网络变量变化事件
        localPlayerManager.topLaneTargetLane.OnValueChanged += (oldValue, newValue) => UpdateButtonVisuals(Lane.Top, newValue);
        localPlayerManager.midLaneTargetLane.OnValueChanged += (oldValue, newValue) => UpdateButtonVisuals(Lane.Mid, newValue);
        localPlayerManager.botLaneTargetLane.OnValueChanged += (oldValue, newValue) => UpdateButtonVisuals(Lane.Bot, newValue);
        
        // 初始化按钮视觉状态
        UpdateButtonVisuals(Lane.Top, localPlayerManager.topLaneTargetLane.Value);
        UpdateButtonVisuals(Lane.Mid, localPlayerManager.midLaneTargetLane.Value);
        UpdateButtonVisuals(Lane.Bot, localPlayerManager.botLaneTargetLane.Value);
    }
    
    private void RegisterButtonEvents()
    {
        // 上路转线按钮
        if (topToTopButton != null)
            topToTopButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Top, Lane.Top));
        if (topToMidButton != null)
            topToMidButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Top, Lane.Mid));
        if (topToBotButton != null)
            topToBotButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Top, Lane.Bot));
        
        // 中路转线按钮
        if (midToTopButton != null)
            midToTopButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Mid, Lane.Top));
        if (midToMidButton != null)
            midToMidButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Mid, Lane.Mid));
        if (midToBotButton != null)
            midToBotButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Mid, Lane.Bot));
        
        // 下路转线按钮
        if (botToTopButton != null)
            botToTopButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Bot, Lane.Top));
        if (botToMidButton != null)
            botToMidButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Bot, Lane.Mid));
        if (botToBotButton != null)
            botToBotButton.onClick.AddListener(() => OnRedirectButtonClicked(Lane.Bot, Lane.Bot));
        
        // Debug.Log("所有转线按钮事件已注册");
    }
    
    private void OnRedirectButtonClicked(Lane sourceLane, Lane targetLane)
    {
        Debug.Log($"点击了转线按钮: {sourceLane} -> {targetLane}");
        
        if (localPlayerManager != null)
        {
            localPlayerManager.SetLaneTargetServerRpc(sourceLane, targetLane);
            Debug.Log($"已发送转线请求: {sourceLane} -> {targetLane}");
        }
        else
        {
            Debug.LogWarning("本地PlayerManager未找到，无法发送转线请求"); //应该不是大问题
            
            // 尝试查找本地PlayerManager
            var players = FindObjectsOfType<PlayerManager>();
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    localPlayerManager = player;
                    localPlayerManager.SetLaneTargetServerRpc(sourceLane, targetLane);
                    Debug.Log($"找到PlayerManager并发送转线请求: {sourceLane} -> {targetLane}");
                    break;
                }
            }
        }
    }
    
    private void UpdateButtonVisuals(Lane sourceLane, Lane targetLane)
    {
        // 根据当前转线状态更新按钮视觉效果
        switch (sourceLane)
        {
            case Lane.Top:
                SetButtonActive(topToTopButton, targetLane == Lane.Top);
                SetButtonActive(topToMidButton, targetLane == Lane.Mid);
                SetButtonActive(topToBotButton, targetLane == Lane.Bot);
                break;
            case Lane.Mid:
                SetButtonActive(midToTopButton, targetLane == Lane.Top);
                SetButtonActive(midToMidButton, targetLane == Lane.Mid);
                SetButtonActive(midToBotButton, targetLane == Lane.Bot);
                break;
            case Lane.Bot:
                SetButtonActive(botToTopButton, targetLane == Lane.Top);
                SetButtonActive(botToMidButton, targetLane == Lane.Mid);
                SetButtonActive(botToBotButton, targetLane == Lane.Bot);
                break;
        }
    }
    
    private void SetButtonActive(Button button, bool isActive)
    {
        if (button != null)
        {
            // 改变按钮颜色或其他视觉效果
            ColorBlock colors = button.colors;
            colors.normalColor = isActive ? Color.green : Color.white;
            button.colors = colors;
        }
    }
}
