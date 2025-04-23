using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerUIController : MonoBehaviour
{
    // InGamePanel 内部的内容
    [Header("游戏内面板InGame Panel")]
    [SerializeField] private GameObject inGamePanel; // 将所有对局内 UI 作为 inGamePanel 的子物体

    // UI 显示部分：分别为三路的文本（可根据需要扩展为面板）
    [Header("上路UI")]
    [SerializeField] private TextMeshProUGUI topPopulationText;
    [SerializeField] private TextMeshProUGUI topResourceText;
    [SerializeField] private TextMeshProUGUI topProductionText;

    [SerializeField] private Button topUnitChipmunkButton;  // ID: 1
    [SerializeField] private Button topUnitRabbitButton;    // ID: 2
    [SerializeField] private Button topUnitCatButton;       // ID: 3
    [SerializeField] private Button topUnitChickenButton;   // ID: 4
    [SerializeField] private Button topUnitDogButton;       // ID: 5
    [SerializeField] private Button topUnitDuckButton;      // ID: 6
    [SerializeField] private Button topUnitPandaButton;     // ID: 7
    [SerializeField] private Button topUnitMonkeyButton;    // ID: 8
    [SerializeField] private Button topUnitDeerButton;      // ID: 9
    [SerializeField] private Button topUnitBearButton;      // ID: 10
    [SerializeField] private Button topUnitMooseButton;     // ID: 11
    [SerializeField] private Button topUnitCrocButton;      // ID: 12
    [SerializeField] private Button topUnitHippoButton;     // ID: 13
    [SerializeField] private Button topUnitRhinoButton;     // ID: 14

    [Header("中路UI")]
    [SerializeField] private TextMeshProUGUI midPopulationText;
    [SerializeField] private TextMeshProUGUI midResourceText;
    [SerializeField] private TextMeshProUGUI midProductionText;

    [SerializeField] private Button midUnitChipmunkButton;  // ID: 1
    [SerializeField] private Button midUnitRabbitButton;    // ID: 2
    [SerializeField] private Button midUnitCatButton;       // ID: 3
    [SerializeField] private Button midUnitChickenButton;   // ID: 4
    [SerializeField] private Button midUnitDogButton;       // ID: 5
    [SerializeField] private Button midUnitDuckButton;      // ID: 6
    [SerializeField] private Button midUnitPandaButton;     // ID: 7
    [SerializeField] private Button midUnitMonkeyButton;    // ID: 8
    [SerializeField] private Button midUnitDeerButton;      // ID: 9
    [SerializeField] private Button midUnitBearButton;      // ID: 10
    [SerializeField] private Button midUnitMooseButton;     // ID: 11
    [SerializeField] private Button midUnitCrocButton;      // ID: 12
    [SerializeField] private Button midUnitHippoButton;     // ID: 13
    [SerializeField] private Button midUnitRhinoButton;     // ID: 14

    [Header("下路UI")]
    [SerializeField] private TextMeshProUGUI botPopulationText;
    [SerializeField] private TextMeshProUGUI botResourceText;
    [SerializeField] private TextMeshProUGUI botProductionText;

    [SerializeField] private Button botUnitChipmunkButton;  // ID: 1
    [SerializeField] private Button botUnitRabbitButton;    // ID: 2
    [SerializeField] private Button botUnitCatButton;       // ID: 3
    [SerializeField] private Button botUnitChickenButton;   // ID: 4
    [SerializeField] private Button botUnitDogButton;       // ID: 5
    [SerializeField] private Button botUnitDuckButton;      // ID: 6
    [SerializeField] private Button botUnitPandaButton;     // ID: 7
    [SerializeField] private Button botUnitMonkeyButton;    // ID: 8
    [SerializeField] private Button botUnitDeerButton;      // ID: 9
    [SerializeField] private Button botUnitBearButton;      // ID: 10
    [SerializeField] private Button botUnitMooseButton;     // ID: 11
    [SerializeField] private Button botUnitCrocButton;      // ID: 12
    [SerializeField] private Button botUnitHippoButton;     // ID: 13
    [SerializeField] private Button botUnitRhinoButton;     // ID: 14

    [Header("视角翻转提示")]
    [SerializeField] private GameObject viewFlipPanel; // 视角翻转提示面板
    [SerializeField] private TextMeshProUGUI viewFlipText; // 视角翻转提示文本

    private PlayerManager localPlayerManager;

    void Start()
    {
        // StartCoroutine(WaitForLocalPlayerManager());
    }

    // 如果你希望控制 UI 面板的激活时机，也可以在 Awake 中隐藏
    void Awake()
    {
        // Debug.Log("[PlayerUIController] Awake called.");
        // 在 Awake 中订阅事件，确保即使 UI 内部内容隐藏，也能收到通知
        PlayerManager.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        // 初始时隐藏 InGamePanel 内部内容
        if (inGamePanel != null)
        {
            inGamePanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // 注意：如果在 Awake 中订阅了事件，需要在 OnDestroy 中取消订阅
        PlayerManager.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
    }

    /// <summary>
    /// 当本地玩家生成时，通过事件通知调用此方法
    /// </summary>
    private void HandleLocalPlayerSpawned(PlayerManager localPlayer)
    {
        // Debug.Log("[PlayerUIController] HandleLocalPlayerSpawned called.");
        localPlayerManager = localPlayer;
        // 激活 InGamePanel 内容
        if (inGamePanel != null)
        {
            inGamePanel.SetActive(true);
        }
        InitializeUI();
    }

    void InitializeUI()
    {
        // 订阅事件、绑定按钮等初始化操作（订阅兵线资源数据变化事件）
        localPlayerManager.TopLaneData.OnValueChanged += UpdateTopLaneUI;
        localPlayerManager.MidLaneData.OnValueChanged += UpdateMidLaneUI;
        localPlayerManager.BotLaneData.OnValueChanged += UpdateBotLaneUI;

        // 立即更新 UI
        UpdateTopLaneUI(default, localPlayerManager.TopLaneData.Value);
        UpdateMidLaneUI(default, localPlayerManager.MidLaneData.Value);
        UpdateBotLaneUI(default, localPlayerManager.BotLaneData.Value);

        // 为每条兵线按钮添加点击事件（UnitChipmunk 对应 id=1，UnitRabbit 对应 id=2）
        // 上路按键监听
        topUnitChipmunkButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 1));
        topUnitRabbitButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 2));
        topUnitCatButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 3));
        topUnitChickenButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 4));
        topUnitDogButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 5));
        topUnitDuckButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 6));
        topUnitPandaButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 7));
        topUnitMonkeyButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 8));
        topUnitDeerButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 9));
        topUnitBearButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 10));
        topUnitMooseButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 11));
        topUnitCrocButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 12));
        topUnitHippoButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 13));
        topUnitRhinoButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Top, 14));

        // 中路按键监听
        midUnitChipmunkButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 1));
        midUnitRabbitButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 2));
        midUnitCatButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 3));
        midUnitChickenButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 4));
        midUnitDogButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 5));
        midUnitDuckButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 6));
        midUnitPandaButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 7));
        midUnitMonkeyButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 8));
        midUnitDeerButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 9));
        midUnitBearButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 10));
        midUnitMooseButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 11));
        midUnitCrocButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 12));
        midUnitHippoButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 13));
        midUnitRhinoButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Mid, 14));

        // 下路按键监听
        botUnitChipmunkButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 1));
        botUnitRabbitButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 2));
        botUnitCatButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 3));
        botUnitChickenButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 4));
        botUnitDogButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 5));
        botUnitDuckButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 6));
        botUnitPandaButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 7));
        botUnitMonkeyButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 8));
        botUnitDeerButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 9));
        botUnitBearButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 10));
        botUnitMooseButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 11));
        botUnitCrocButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 12));
        botUnitHippoButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 13));
        botUnitRhinoButton?.onClick.AddListener(() => OnChangeLaneUnitButtonClicked(Lane.Bot, 14));

        // 设置视角翻转提示
        SetupViewFlipNotification();
    }

    /// <summary>
    /// 设置视角翻转提示
    /// </summary>
    private void SetupViewFlipNotification()
    {
        if (viewFlipPanel != null)
        {
            // 检查当前玩家是否为房主
            bool isHost = localPlayerManager.OwnerClientId == RoomManager.Instance.GetRoomHostClientId();
            
            // 如果不是房主，显示提示
            viewFlipPanel.SetActive(!isHost);
            
            // 如果提示文本组件存在且不是房主，设置提示文本
            if (viewFlipText != null && !isHost)
            {
            // 使用 <br> 标签代替 \n 实现换行
            viewFlipText.text = "Your view has been flipped.<br>The top of the map is BOT lane. The bottom of the map is TOP lane";
            viewFlipText.color = Color.red; // 使用醒目的颜色
            }
        }
    }

    #region UI 更新方法

    private void UpdateTopLaneUI(LaneResourceData previous, LaneResourceData current)
    {
        // topPopulationText.text = $"人口：{current.availablePopulation}";
        // topResourceText.text = $"资源：{current.availableResource}";
        // topProductionText.text = $"产量：{current.production}";
        topPopulationText.text = $"Population: {current.availablePopulation}";
        topResourceText.text = $"Resource: {current.availableResource}";
        topProductionText.text = $"Output: {current.production}";
    }

    private void UpdateMidLaneUI(LaneResourceData previous, LaneResourceData current)
    {
        // midPopulationText.text = $"人口：{current.availablePopulation}";
        // midResourceText.text = $"资源：{current.availableResource}";
        // midProductionText.text = $"产量：{current.production}";
        midPopulationText.text = $"Population: {current.availablePopulation}";
        midResourceText.text = $"Resource: {current.availableResource}";
        midProductionText.text = $"Output: {current.production}";
    }

    private void UpdateBotLaneUI(LaneResourceData previous, LaneResourceData current)
    {
        // botPopulationText.text = $"人口：{current.availablePopulation}";
        // botResourceText.text = $"资源：{current.availableResource}";
        // botProductionText.text = $"产量：{current.production}";
        botPopulationText.text = $"Population: {current.availablePopulation}";
        botResourceText.text = $"Resource: {current.availableResource}";
        botProductionText.text = $"Output: {current.production}";
    }

    #endregion

    #region 按钮事件处理

    private void OnChangeLaneUnitButtonClicked(Lane lane, int unitId)
    {
        if (localPlayerManager != null)
        {
            // 调用服务器 RPC 修改对应兵线的选择兵种
            localPlayerManager.SetSelectedUnitForLaneServerRpc(lane, unitId);
        }
    }

    #endregion
}