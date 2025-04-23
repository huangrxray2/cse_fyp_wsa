using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class PlayerManager : NetworkBehaviour
{
    #region 内部数据结构

    // 直接在 PlayerManager 上声明兵线资源数据（如果需要更多兵线，也可以扩展为数组或字典）
    [SerializeField] 
    private NetworkVariable<LaneResourceData> topLaneData = new NetworkVariable<LaneResourceData>(
        new LaneResourceData(60, 0, 1000)); // 若运行不动，改成40

    [SerializeField]
    private NetworkVariable<LaneResourceData> midLaneData = new NetworkVariable<LaneResourceData>(
        new LaneResourceData(60, 0, 1000));

    [SerializeField]
    private NetworkVariable<LaneResourceData> botLaneData = new NetworkVariable<LaneResourceData>(
        new LaneResourceData(60, 0, 1000));

    // 公共只读属性
    public NetworkVariable<LaneResourceData> TopLaneData => topLaneData;
    public NetworkVariable<LaneResourceData> MidLaneData => midLaneData;
    public NetworkVariable<LaneResourceData> BotLaneData => botLaneData;

    // 定义一个结构体保存各兵种的出兵参数
    private struct UnitParameters
    {
        public int costPopulation;
        public int costResource;
        public int addProduction;
        public GameObject unitPrefab;
    }
    #endregion

    #region 引用和预制体
    [Header("预制体")]
    
    [SerializeField] private GameObject basePrefab;
    [SerializeField] private GameObject hilltopTowerPrefab;
    [SerializeField] private GameObject innerTowerPrefab;
    [SerializeField] private GameObject outerTowerPrefab;
    [SerializeField] private GameObject junglePrefab;        // 打野的 Prefab

    [SerializeField] private GameObject unitChipmunkPrefab;  // 单位1花栗鼠的Prefab
    [SerializeField] private GameObject unitRabbitPrefab;    // 单位2兔子的Prefab
    [SerializeField] private GameObject unitCatPrefab;       // 单位3猫的Prefab
    [SerializeField] private GameObject unitChickenPrefab;   // 单位4鸡的Prefab
    [SerializeField] private GameObject unitDogPrefab;       // 单位5狗的Prefab
    [SerializeField] private GameObject unitDuckPrefab;      // 单位6鸭的Prefab
    [SerializeField] private GameObject unitPandaPrefab;     // 单位7熊猫的Prefab
    [SerializeField] private GameObject unitMonkeyPrefab;    // 单位8猴子的Prefab
    [SerializeField] private GameObject unitDeerPrefab;      // 单位9鹿的Prefab
    [SerializeField] private GameObject unitBearPrefab;      // 单位10熊的Prefab
    [SerializeField] private GameObject unitMoosePrefab;     // 单位11麋鹿的Prefab
    [SerializeField] private GameObject unitCrocPrefab;      // 单位12鳄鱼的Prefab
    [SerializeField] private GameObject unitHippoPrefab;     // 单位13河马的Prefab
    [SerializeField] private GameObject unitRhinoPrefab;     // 单位14犀牛的Prefab
    #endregion

    #region 单位参数与选择

    // 用字典将单位ID与参数映射
    private Dictionary<int, UnitParameters> unitParameters;
    #endregion

    #region 每条兵线的当前选择兵种
    // 默认选择兵种ID为 1
    public NetworkVariable<int> topLaneSelectedUnit = new NetworkVariable<int>(1);
    public NetworkVariable<int> midLaneSelectedUnit = new NetworkVariable<int>(1);
    public NetworkVariable<int> botLaneSelectedUnit = new NetworkVariable<int>(1);
    #endregion

    #region 兵线资源相关（每条兵线独立）
    // 存储每条兵线的资源数据
    // 为了方便代码，可以用一个字典来管理各兵线的变量（字典本身不是同步的，但里面存放的是已绑定的 NetworkVariable）
    private Dictionary<Lane, NetworkVariable<LaneResourceData>> laneResources;
    // 存储各兵线出兵协程的句柄
    private Dictionary<Lane, Coroutine> laneSpawnCoroutines;
    #endregion

    // 保存玩家所有生成的建筑，便于销毁等操作
    private Dictionary<string, List<GameObject>> structures = new Dictionary<string, List<GameObject>>();

    #region 阵线推进和单位晋升系统
    // 阵线推进状态 - 每条兵线独立
    [SerializeField]
    private Dictionary<Lane, LanePromotionStatus> lanePromotionStatus = new Dictionary<Lane, LanePromotionStatus>();
    
    // 单位晋升状态 - 每条兵线每种单位独立
    [SerializeField]
    private Dictionary<Lane, Dictionary<int, UnitPromotionStatus>> unitPromotionStatus = new Dictionary<Lane, Dictionary<int, UnitPromotionStatus>>();
    
    // 阵线推进状态结构
    [System.Serializable]
    public struct LanePromotionStatus
    {
        public bool outerTowerDestroyed;  // 外塔是否被摧毁
        public bool innerTowerDestroyed;  // 内塔是否被摧毁
        public bool hilltopTowerDestroyed; // 高地塔是否被摧毁
        
        // 获取当前阵线推进带来的各种属性加成
        public float GetHealthMultiplier()
        {
            float multiplier = 1.0f;
            if (innerTowerDestroyed) multiplier *= 1.2f; // 内塔摧毁，生命值提升1.2倍
            return multiplier;
        }
        
        public float GetDamageMultiplier()
        {
            float multiplier = 1.0f;
            if (hilltopTowerDestroyed) multiplier *= 1.2f; // 高地塔摧毁，攻击力提升1.2倍
            return multiplier;
        }
        
        public float GetSpeedMultiplier()
        {
            float multiplier = 1.0f;
            if (outerTowerDestroyed) multiplier *= 1.2f; // 外塔摧毁，移动速度提升1.2倍
            return multiplier;
        }
    }
    
    // 单位晋升状态结构
    [System.Serializable]
    public struct UnitPromotionStatus
    {
        public int deployCount;  // 该兵线上该单位的部署次数
        public int level;        // 当前晋升等级 (1-4)
        
        // 获取当前晋升等级带来的属性加成
        public float GetAttributeMultiplier()
        {
            switch (level)
            {
                case 2: return 1.2f;  // 等级2：属性提升1.2倍
                case 3: return 1.44f; // 等级3：属性提升1.2^2倍
                case 4: return 1.728f; // 等级4：属性提升1.2^3倍
                default: return 1.0f; // 等级1：无加成
            }
        }
        
        // 获取当前晋升等级带来的资源消耗倍率
        public float GetResourceMultiplier()
        {
            switch (level)
            {
                case 2: return 1.5f;  // 等级2：资源消耗1.5倍
                case 3: return 2.25f; // 等级3：资源消耗1.5^2倍
                case 4: return 3.375f; // 等级4：资源消耗1.5^3倍
                default: return 1.0f; // 等级1：无加成
            }
        }
    }
    #endregion

    [Header("容器预制体")]
    [SerializeField] private GameObject lanesRootPrefab;
    [SerializeField] private GameObject laneContainerPrefab;
    [SerializeField] private GameObject unitsContainerPrefab;

    // 用于存储动态创建的容器的引用，方便后续查找
    private Transform lanesRootTransform;
    private Dictionary<Lane, Transform> laneContainerTransforms = new Dictionary<Lane, Transform>();
    private Dictionary<Lane, Transform> unitsContainerTransforms = new Dictionary<Lane, Transform>();

    // 映射Lane枚举到容器名称和显示名称
    private static readonly Dictionary<Lane, string> laneNames = new Dictionary<Lane, string>
    {
        { Lane.Top, "TopLane" },
        { Lane.Mid, "MidLane" },
        { Lane.Bot, "BotLane" }
    };

    private static readonly Dictionary<Lane, string> unitContainerNames = new Dictionary<Lane, string>
    {
        { Lane.Top, "TopUnits" },
        { Lane.Mid, "MidUnits" },
        { Lane.Bot, "BotUnits" }
    };

    private static readonly Dictionary<Lane, Color> laneColors = new Dictionary<Lane, Color>
    {
        { Lane.Top, new Color(0.8f, 0.4f, 0.4f) }, // 粉红色 - 上路
        { Lane.Mid, new Color(0.4f, 0.8f, 0.4f) }, // 淡绿色 - 中路
        { Lane.Bot, new Color(0.4f, 0.4f, 0.8f) }  // 淡蓝色 - 下路
    };

    // 用以通知playerUIController
    public static event Action<PlayerManager> OnLocalPlayerSpawned;

    #region Unity 生命周期

    void Awake()
    {
        // 根据各兵种脚本修改
        unitParameters = new Dictionary<int, UnitParameters>()
        {
            { 1, new UnitParameters { costPopulation = 1, costResource = 33, addProduction = 6, unitPrefab = unitChipmunkPrefab } },    // 花栗鼠
            { 2, new UnitParameters { costPopulation = 1, costResource = 30, addProduction = 6, unitPrefab = unitRabbitPrefab } },      // 兔子
            { 3, new UnitParameters { costPopulation = 1, costResource = 40, addProduction = 6, unitPrefab = unitCatPrefab } },         // 猫
            { 4, new UnitParameters { costPopulation = 1, costResource = 50, addProduction = 8, unitPrefab = unitChickenPrefab } },     // 鸡
            { 5, new UnitParameters { costPopulation = 1, costResource = 40, addProduction = 7, unitPrefab = unitDogPrefab } },         // 狗
            { 6, new UnitParameters { costPopulation = 1, costResource = 51, addProduction = 9, unitPrefab = unitDuckPrefab } },        // 鸭
            { 7, new UnitParameters { costPopulation = 3, costResource = 150, addProduction = 15, unitPrefab = unitPandaPrefab } },      // 熊猫
            { 8, new UnitParameters { costPopulation = 2, costResource = 78, addProduction = 12, unitPrefab = unitMonkeyPrefab } },      // 猴子
            { 9, new UnitParameters { costPopulation = 2, costResource = 150, addProduction = 19, unitPrefab = unitDeerPrefab } },        // 鹿
            { 10, new UnitParameters { costPopulation = 4, costResource = 260, addProduction = 31, unitPrefab = unitBearPrefab } },     // 熊
            { 11, new UnitParameters { costPopulation = 3, costResource = 280, addProduction = 24, unitPrefab = unitMoosePrefab } },    // 麋鹿
            { 12, new UnitParameters { costPopulation = 4, costResource = 560, addProduction = 58, unitPrefab = unitCrocPrefab } },     // 鳄鱼
            { 13, new UnitParameters { costPopulation = 5, costResource = 1080, addProduction = 98, unitPrefab = unitHippoPrefab } },    // 河马
            { 14, new UnitParameters { costPopulation = 5, costResource = 1500, addProduction = 137, unitPrefab = unitRhinoPrefab } }     // 犀牛
        };
        // 注意：不要在 Awake 中初始化 laneResources，因为此时 NetworkObject 还未 Spawn，
        // 修改 NetworkVariable 会引起警告或错误。
    }

    public override void OnNetworkSpawn()
    {
        // 通知PlayerUIController，player已生成
        if (IsOwner)
        {
            OnLocalPlayerSpawned?.Invoke(this);
        }

        // 只有拥有者或服务器才需要初始化和控制出兵逻辑
        if (!IsServer)
            return;

        // 初始化阵线推进和单位晋升系统
        InitializePromotionSystem();
        
        // 订阅建筑被摧毁事件
        Structure.OnStructureDestroyed += HandleStructureDestroyed;

        laneSpawnCoroutines = new Dictionary<Lane, Coroutine>();

        // 服务器（IsServer）初始化兵线资源
        InitializeLaneResources();
        // Debug.Log("LaneResources initialized in OnNetworkSpawn for Host.");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // 取消订阅事件
        if (IsServer)
        {
            Structure.OnStructureDestroyed -= HandleStructureDestroyed;
        }
    }
    #endregion

    /// <summary>
    /// 初始化阵线推进和单位晋升系统
    /// </summary>
    private void InitializePromotionSystem()
    {
        // 初始化每条兵线的阵线推进状态
        foreach (Lane lane in new[] { Lane.Top, Lane.Mid, Lane.Bot })
        {
            lanePromotionStatus[lane] = new LanePromotionStatus
            {
                outerTowerDestroyed = false,
                innerTowerDestroyed = false,
                hilltopTowerDestroyed = false
            };
            
            // 初始化每条兵线的每种单位晋升状态
            unitPromotionStatus[lane] = new Dictionary<int, UnitPromotionStatus>();
            foreach (int unitId in unitParameters.Keys)
            {
                unitPromotionStatus[lane][unitId] = new UnitPromotionStatus
                {
                    deployCount = 0,
                    level = 1
                };
            }
        }
        
        // Debug.Log("阵线推进和单位晋升系统已初始化");
    }

    /// <summary>
    /// 处理建筑被摧毁事件，更新阵线推进状态
    /// </summary>
    private void HandleStructureDestroyed(Structure structure)
    {
        // 只处理敌方建筑被摧毁的情况
        if (structure.ownerClientId == OwnerClientId)
            return;
            
        // 只处理塔类型的建筑
        if (structure.structureType == Structure.StructureType.Base)
            return;
            
        // 确保建筑有分配的兵线
        if (!structure.assignedLane.HasValue)
            return;
            
        Lane lane = structure.assignedLane.Value;
        
        // 更新阵线推进状态
        LanePromotionStatus status = lanePromotionStatus[lane];
        
        switch (structure.structureType)
        {
            case Structure.StructureType.OuterTower:
                status.outerTowerDestroyed = true;
                Debug.Log($"玩家 {OwnerClientId} 在 {lane} 路摧毁了敌方外塔，该路单位变为骑兵，移动速度提升至1.2倍");
                break;
            case Structure.StructureType.InnerTower:
                status.innerTowerDestroyed = true;
                Debug.Log($"玩家 {OwnerClientId} 在 {lane} 路摧毁了敌方内塔，该路单位变为堡垒守卫，生命值提升至1.2倍");
                break;
            case Structure.StructureType.HilltopTower:
                status.hilltopTowerDestroyed = true;
                Debug.Log($"玩家 {OwnerClientId} 在 {lane} 路摧毁了敌方高地塔，该路单位变为重装步兵，攻击力提升至1.2倍");
                break;
        }
        
        // 更新状态
        lanePromotionStatus[lane] = status;
    }

    /// <summary>
    /// 仅初始化兵线资源，不开始出兵
    /// </summary>
    public void InitializeResources()
    {
        if (IsServer)
        {
            if (laneResources == null)                                                  // 这个方法是否无效？？？？？？？？？？？？？？？？？
            {
                InitializeLaneResources();
                Debug.Log("兵线资源已初始化，等待开始出兵");
            }
            
            if (laneSpawnCoroutines == null)
            {
                laneSpawnCoroutines = new Dictionary<Lane, Coroutine>();
            }
        }
    }

    private void InitializeLaneResources()
    {
        laneResources = new Dictionary<Lane, NetworkVariable<LaneResourceData>>()
        {
            { Lane.Top, topLaneData },
            { Lane.Mid, midLaneData },
            { Lane.Bot, botLaneData }
        };
    }

    #region 结构初始化
    /// <summary>
    /// 初始化玩家建筑结构
    /// </summary>
    /// <param name="basePosition">基地位置</param>
    /// <param name="hilltopTowerPositions">高地塔位置</param>
    /// <param name="innerTowerPositions">内塔位置</param>
    /// <param name="outerTowerPositions">外塔位置</param>
    public void InitializeStructures(
        Vector3 basePosition,
        Vector3[] hilltopTowerPositions,
        Vector3[] innerTowerPositions,
        Vector3[] outerTowerPositions)
    {
        if (!IsServer) return;

        // 初始化 structures 字典
        structures.Clear();
        structures["Base"] = new List<GameObject>();
        structures["HilltopTower"] = new List<GameObject>();
        structures["InnerTower"] = new List<GameObject>();
        structures["OuterTower"] = new List<GameObject>();

        // 动态创建 Lanes 容器
        // 创建并 Spawn LanesRoot
        GameObject lanesRootObj = Instantiate(lanesRootPrefab, transform.position, transform.rotation);
        lanesRootObj.name = "Lanes"; // 改个名方便看
        var lanesNet = lanesRootObj.GetComponent<NetworkObject>();
        lanesNet.Spawn();
        lanesRootObj.transform.SetParent(transform, false);
        lanesRootTransform = lanesRootObj.transform;

        // 为每条路创建 LaneContainer 和 UnitsContainer
        foreach (Lane lane in new[] { Lane.Top, Lane.Mid, Lane.Bot })
        {
            // 创建 LaneContainer
            GameObject laneGO = Instantiate(laneContainerPrefab, lanesRootTransform.position, Quaternion.identity);
            laneGO.name = laneNames[lane];
            var laneNet = laneGO.GetComponent<NetworkObject>();
            laneNet.Spawn();
            laneGO.transform.SetParent(lanesRootTransform, false);
            laneContainerTransforms[lane] = laneGO.transform;

            // // 可选：设置一个特殊材质或颜色以视觉区分（如果容器有渲染器）
            // if (laneGO.TryGetComponent<Renderer>(out var renderer))
            // {
            //     Material newMat = new Material(renderer.material);
            //     newMat.color = laneColors[lane];
            //     renderer.material = newMat;
            // }

            // 创建 UnitsContainer
            GameObject unitsGO = Instantiate(unitsContainerPrefab, laneGO.transform.position, Quaternion.identity);
            unitsGO.name = unitContainerNames[lane];
            var unitsNet = unitsGO.GetComponent<NetworkObject>();
            unitsNet.Spawn();
            unitsGO.transform.SetParent(laneGO.transform, false);
            unitsContainerTransforms[lane] = unitsGO.transform;
        }

        // 生成基地（不属于任何特定Lane）
        SpawnStructure(basePrefab, basePosition, Structure.StructureType.Base, lane: null);

        // 生成三种塔，明确指定每个塔所属的Lane
        for (int i = 0; i < hilltopTowerPositions.Length; i++)
        {
            Lane lane = (Lane)i;
            SpawnStructure(hilltopTowerPrefab, hilltopTowerPositions[i], Structure.StructureType.HilltopTower, lane);
        }

        for (int i = 0; i < innerTowerPositions.Length; i++)
        {
            Lane lane = (Lane)i;
            SpawnStructure(innerTowerPrefab, innerTowerPositions[i], Structure.StructureType.InnerTower, lane);
        }

        for (int i = 0; i < outerTowerPositions.Length; i++)
        {
            Lane lane = (Lane)i;
            SpawnStructure(outerTowerPrefab, outerTowerPositions[i], Structure.StructureType.OuterTower, lane);
        }
        
        // Debug.Log($"为玩家 {OwnerClientId} 初始化建筑.");
    }

    /// <summary>
    /// 生成单个建筑，并记录在字典中
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="position">生成位置</param>
    /// <param name="structureType">建筑类型，用于字典分类</param>
    /// <returns>生成的建筑对象</returns>
    // 扩展：带上 laneIndex 参数，-1 表示 Base，不挂到路下
    private GameObject SpawnStructure(
        GameObject prefab,
        Vector3 position,
        Structure.StructureType type,
        Lane? lane = null)
    {
        GameObject obj = Instantiate(prefab, position, Quaternion.identity);

        // 立即设置 Structure 组件属性
        if (obj.TryGetComponent<Structure>(out var st))
        {
            st.ownerClientId = OwnerClientId;
            st.structureType = type;
            st.assignedLane = lane; // 设置塔所属的兵线
            
            // 添加调试日志
            // Debug.Log($"设置建筑 {obj.name} 的类型为 {type}，兵线为 {lane}");
        }

        // 然后再进行网络同步
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);

        // 设置父物体 - 基地放在Player下，塔放在对应Lane下
        if (lane.HasValue)
        {
            // 塔放在对应Lane下
            if (laneContainerTransforms.TryGetValue(lane.Value, out Transform laneTransform))
            {
                obj.transform.SetParent(laneTransform, true);
                
                // 可以给塔添加视觉标识
                if (obj.TryGetComponent<TowerDisplay>(out var display))
                {
                    // 阵营颜色 + 路的颜色作为色调调整
                    Color baseColor = (OwnerClientId == RoomManager.Instance.GetRoomHostClientId()) ? Color.red : Color.blue;
                    Color laneColor = laneColors[lane.Value];
                    // 混合颜色
                    Color mixedColor = new Color(
                        (baseColor.r + laneColor.r) / 2, 
                        (baseColor.g + laneColor.g) / 2, 
                        (baseColor.b + laneColor.b) / 2
                    );
                    
                    string displayName = $"{lane.Value} {type}";
                    display.SetColorAndNameClientRpc(mixedColor, displayName);
                }
            }
            else
            {
                Debug.LogError($"Lane容器 {lane.Value} 不存在，塔将直接挂在Player下");
                obj.transform.SetParent(transform, true);
            }
        }
        else
        {
            // 基地直接挂在Player下
            obj.transform.SetParent(transform, true);
            
            // 基地的颜色和名称
            if (obj.TryGetComponent<TowerDisplay>(out var display))
            {
                Color color = (OwnerClientId == RoomManager.Instance.GetRoomHostClientId()) ? Color.red : Color.blue;
                display.SetColorAndNameClientRpc(color, "Base");
            }
        }

        // 记录到字典
        string key = type.ToString();
        if (!structures.ContainsKey(key))
            structures[key] = new List<GameObject>();
        structures[key].Add(obj);

        // 如果该建筑是塔，则尝试调用 TowerDisplay 更新名称和颜色
        // 更新显示
        if (obj.TryGetComponent<TowerDisplay>(out var disp))
        {
            Color color = (OwnerClientId == NetworkManager.Singleton.LocalClientId) ? Color.red : Color.blue;
            disp.SetColorAndNameClientRpc(color, type.ToString());
        }

        return obj;
    }
    #endregion

    #region 对外接口：启动游戏逻辑

    /// <summary>
    /// 被GameManager调用，开始游戏逻辑
    /// </summary>
    /// <summary>
    /// 被GameManager调用，开始出兵逻辑
    /// </summary>
    public void StartSpawning()
    {
        // 在服务器上，为每个玩家启动出兵
        if (IsServer)
        {
            // 确保资源已初始化
            if (laneResources == null)
            {
                InitializeLaneResources();
            }
            
            // 启动游戏回合协程
            StartCoroutine(GameRoundCoroutine());
            Debug.Log($"玩家 {OwnerClientId} 开始出兵");
        }
    }

    #endregion

    #region 游戏回合与出兵逻辑

    /// <summary>
    /// 每20秒一个回合，重置人口、增加资源，并控制单位生成
    /// </summary>
    private IEnumerator GameRoundCoroutine()
    {
        // 确保 laneSpawnCoroutines 已初始化
        if (laneSpawnCoroutines == null)
        {
            laneSpawnCoroutines = new Dictionary<Lane, Coroutine>();
        }
        
        if (laneResources == null)
        {
            Debug.LogError("laneResources 还未初始化！");
            yield break; // 或者进行初始化后再继续
        }

        while (true)
        {
            // 对每条兵线执行回合初操作
            foreach (Lane lane in laneResources.Keys)
            {
                // 获取当前数据副本（注意：LaneResourceData 是值类型，修改后要重新赋值）
                LaneResourceData data = laneResources[lane].Value;
                // 重置人口
                data.availablePopulation = 40;
                // 增加资源：这里假设 production 已经设置
                data.availableResource += data.production;
                // 将更新后的数据重新赋值到 NetworkVariable 中
                laneResources[lane].Value = data;

                // 若该兵线已有出兵协程，则先停止
                if (laneSpawnCoroutines.ContainsKey(lane) && laneSpawnCoroutines[lane] != null)
                {
                    StopCoroutine(laneSpawnCoroutines[lane]);
                }
                // 启动该兵线的出兵协程
                laneSpawnCoroutines[lane] = StartCoroutine(SpawnUnitsForLane(lane));
            }

            // 回合持续时间，例如30秒。20秒太短了会卡
            yield return new WaitForSeconds(30f);

            // 出兵阶段结束，停止所有兵线的出兵协程
            foreach (Lane lane in laneResources.Keys)
            {
                if (laneSpawnCoroutines.ContainsKey(lane) && laneSpawnCoroutines[lane] != null)
                {
                    StopCoroutine(laneSpawnCoroutines[lane]);
                    laneSpawnCoroutines[lane] = null;
                }
            }

            // 可选：打印各兵线当前状态
            foreach (Lane lane in laneResources.Keys)
            {
                LaneResourceData data = laneResources[lane].Value;
                // Debug.Log($"Lane {lane}: remaining population: {data.availablePopulation}, " +
                //           $"resource: {data.availableResource}, production: {data.production}");
            }
        }
    }

    /// <summary>
    /// 出兵逻辑：每0.25秒尝试出一个兵种，条件是可用人口和资源足够
    /// </summary>
    private IEnumerator SpawnUnitsForLane(Lane lane)
    {
        // 等待直到 NetworkObject 被 Spawn 完毕
        // yield return new WaitUntil(() => IsSpawned);
        while (true)
        {
            int unitId = 0;
            // 根据传入兵线读取对应的选择兵种
            switch(lane)
            {
                case Lane.Top:
                    unitId = topLaneSelectedUnit.Value;
                    break;
                case Lane.Mid:
                    unitId = midLaneSelectedUnit.Value;
                    break;
                case Lane.Bot:
                    unitId = botLaneSelectedUnit.Value;
                    break;
            }
            if (!unitParameters.ContainsKey(unitId))
            {
                Debug.LogError($"Unit parameters not defined for unit id: {unitId}");
                yield break;
            }

            UnitParameters parameters = unitParameters[unitId];
            // 获取当前兵线数据
            LaneResourceData data = laneResources[lane].Value;

            // 获取资源消耗倍率
            float resourceMultiplier = unitPromotionStatus[lane][unitId].GetResourceMultiplier();
            int adjustedCostResource = Mathf.RoundToInt(parameters.costResource * resourceMultiplier);

            // 判断是否足够
            if (data.availablePopulation >= parameters.costPopulation &&
                data.availableResource >= adjustedCostResource)
            {
                // 扣除相应消耗
                data.availablePopulation -= parameters.costPopulation;
                data.availableResource -= adjustedCostResource;
                // 增加产量
                data.production += parameters.addProduction;
                // 重新赋值
                laneResources[lane].Value = data;

                // 生成单位
                SpawnUnitForLane(lane, parameters);

                // Debug.Log($"Player {OwnerClientId} spawned a Unit{unitId} on lane {lane}. " +
                //           $"Remaining population: {data.availablePopulation}, resource: {data.availableResource}, production: {data.production}");
            }

            yield return new WaitForSeconds(0.25f);
        }
    }
    #endregion

    #region 单位生成
    /// <summary>
    /// 根据单位ID和参数生成单位，服务器调用生成单位并同步到客户端
    /// </summary>
    /// <param name="unitId">兵种ID</param>
    /// <param name="parameters">对应的出兵参数</param>
    private void SpawnUnitForLane(Lane lane, UnitParameters parameters)
    {
        if (!structures.ContainsKey("Base") || structures["Base"].Count == 0)
        {
            Debug.LogError("还未生成基地");
            return;
        }

        // 获取当前选择的单位ID
        int unitId = 0;
        switch(lane)
        {
            case Lane.Top:
                unitId = topLaneSelectedUnit.Value;
                break;
            case Lane.Mid:
                unitId = midLaneSelectedUnit.Value;
                break;
            case Lane.Bot:
                unitId = botLaneSelectedUnit.Value;
                break;
        }

        // 生成单位
        Vector3 basePos = structures["Base"][0].transform.position; // 以玩家第一个基地为参考，生成单位
        Vector3 spawnPos = basePos + new Vector3(
            UnityEngine.Random.Range(-40, 40), 
            0, 
            UnityEngine.Random.Range(-40, 40)
        ); // 增加一定随机偏移（注意 y 坐标根据预制体需求调整）

        // 更新单位部署计数并检查晋升
        UpdateUnitDeploymentCount(lane, unitId);
        
        // 获取单位的属性加成
        float healthMultiplier = GetUnitHealthMultiplier(lane, unitId);
        float damageMultiplier = GetUnitDamageMultiplier(lane, unitId);
        float speedMultiplier = GetUnitSpeedMultiplier(lane, unitId);

        /// 实例化单位
        GameObject unitObj = Instantiate(parameters.unitPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);

        // 初始化单位
        InitializeUnit(unitObj, lane);

        // 应用属性加成
        UnitBase unitBase = unitObj.GetComponent<UnitBase>();
        if (unitBase != null)
        {
            // 应用生命值加成
            unitBase.currentHealth *= healthMultiplier;
            
            // 应用攻击力加成
            unitBase.damageALight *= damageMultiplier;
            unitBase.damageAHeavy *= damageMultiplier;
            
            // 应用移动速度加成
            unitBase.moveSpeed *= speedMultiplier;
            
            // 设置单位的晋升等级和阵线状态
            unitBase.SetPromotionStatus(
                unitPromotionStatus[lane][unitId].level,
                lanePromotionStatus[lane].outerTowerDestroyed,
                lanePromotionStatus[lane].innerTowerDestroyed,
                lanePromotionStatus[lane].hilltopTowerDestroyed
            );
        }

        // 可选：根据不同兵种类型，做额外初始化
        // 例如：if (unitId == 1) { ... } else if (unitId == 2) { ... }

        // // 可选：获取 unitChipmunk 组件，后续对单位执行其他操作
        // unitChipmunk unitData = unitObj.GetComponent<unitChipmunk>();
        // if (unitData != null)
        // {
        //     // 此处可做一些初始化、特效触发等
        // }

        // 附加到正确的Units容器
        if (unitsContainerTransforms.TryGetValue(lane, out Transform unitsContainer))
        {
            unitObj.transform.SetParent(unitsContainer, true);
            
            // 单位名称可以显示所属路
            if (unitObj.TryGetComponent<UnitDisplay>(out var display))
            {
                string unitType = unitObj.name.Replace("(Clone)", "").Trim();
                string displayName = $"{lane} {unitType}";
                
                // 单位颜色：结合阵营颜色和路的特性色
                Color teamColor = (OwnerClientId == RoomManager.Instance.GetRoomHostClientId()) ? Color.red : Color.blue;
                Color pathColor = laneColors[lane];
                Color finalColor = Color.Lerp(teamColor, pathColor, 0.3f); // 30%的路径色调
                
                display.SetColorAndNameClientRpc(finalColor, displayName);
            }
        }
        else
        {
            Debug.LogError($"Units容器 {lane} 不存在，单位将直接挂在Player下");
            unitObj.transform.SetParent(transform, true);
        }
    }

    private void InitializeUnit(GameObject unitObj, Lane lane)
    {
        // 获取LaneManager
        LaneManager laneManager = LaneManager.Instance;
        if (laneManager == null)
        {
            Debug.LogError("找不到LaneManager");
            return;
        }

        // 判断阵营
        bool isRedTeam = (OwnerClientId == RoomManager.Instance.GetRoomHostClientId());

        // 获取目标路线
        Lane targetLane = GetTargetLaneForLane(lane);

        // 获取路线
        List<Vector3> route = isRedTeam ? laneManager.redRoutes[targetLane] : laneManager.blueRoutes[targetLane];

        // 初始化单位
        if (unitObj.TryGetComponent<UnitBase>(out var unitBase))
        {
            // 注意：传递原始的lane作为单位所属路线，但使用targetLane的路线
            unitBase.InitializeUnit(lane, isRedTeam, route);
            
            // 可选：添加一个视觉提示，表明这个单位被转线了
            if (lane != targetLane && unitObj.TryGetComponent<UnitDisplay>(out var display))
            {
                string currentName = display.CurrentName;
                Color currentColor = display.CurrentColor;
                
                // 添加转线标记到名称
                string newName = $"{currentName} → {targetLane}";
                
                // 混合颜色以表示转线
                Color targetColor = laneColors[targetLane];
                Color mixedColor = Color.Lerp(currentColor, targetColor, 0.5f);
                
                display.SetColorAndNameClientRpc(mixedColor, newName);
            }
        }
    }
    #endregion

    /// <summary>
    /// 更新单位部署计数并检查晋升
    /// </summary>
    private void UpdateUnitDeploymentCount(Lane lane, int unitId)
    {
        // 获取当前状态
        UnitPromotionStatus status = unitPromotionStatus[lane][unitId];
        
        // 增加部署计数
        status.deployCount++;
        
        // 检查是否达到晋升条件
        if (status.level < 4) // 最高4级
        {
            int requiredCount = 0;
            switch (status.level)
            {
                case 1: requiredCount = 200; break; // 1级升2级需要200次
                case 2: requiredCount = 350; break; // 2级升3级需要350次
                case 3: requiredCount = 500; break; // 3级升4级需要500次
            }
            
            if (status.deployCount >= requiredCount)
            {
                status.level++;
                Debug.Log($"玩家 {OwnerClientId} 在 {lane} 路的单位ID {unitId} 晋升到等级 {status.level}，" +
                        $"属性提升至 {status.GetAttributeMultiplier():F2} 倍，资源消耗提升至 {status.GetResourceMultiplier():F2} 倍");
            }
        }
        
        // 更新状态
        unitPromotionStatus[lane][unitId] = status;
    }

    /// <summary>
    /// 获取单位的生命值加成倍率
    /// </summary>
    private float GetUnitHealthMultiplier(Lane lane, int unitId)
    {
        // 阵线推进带来的生命值加成
        float laneMultiplier = lanePromotionStatus[lane].GetHealthMultiplier();
        
        // 单位晋升带来的属性加成
        float unitMultiplier = unitPromotionStatus[lane][unitId].GetAttributeMultiplier();
        
        // 两种加成相乘
        return laneMultiplier * unitMultiplier;
    }

    /// <summary>
    /// 获取单位的攻击力加成倍率
    /// </summary>
    private float GetUnitDamageMultiplier(Lane lane, int unitId)
    {
        // 阵线推进带来的攻击力加成
        float laneMultiplier = lanePromotionStatus[lane].GetDamageMultiplier();
        
        // 单位晋升带来的属性加成
        float unitMultiplier = unitPromotionStatus[lane][unitId].GetAttributeMultiplier();
        
        // 两种加成相乘
        return laneMultiplier * unitMultiplier;
    }

    /// <summary>
    /// 获取单位的移动速度加成倍率
    /// </summary>
    private float GetUnitSpeedMultiplier(Lane lane, int unitId)
    {
        // 阵线推进带来的移动速度加成
        float laneMultiplier = lanePromotionStatus[lane].GetSpeedMultiplier();
        
        // 单位晋升不影响移动速度
        return laneMultiplier;
    }

    #region 对外的出兵请求接口

    /// <summary>
    /// 为外部提供的请求生成单位接口（例如UI按钮调用）
    /// 此接口调用后如果满足条件，则触发出兵逻辑
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnUnitForLaneServerRpc(Lane lane, ServerRpcParams rpcParams = default)
    {
        int unitId = 0;
        // 根据传入兵线读取对应的选择兵种
        switch(lane)
        {
            case Lane.Top:
                unitId = topLaneSelectedUnit.Value;
                break;
            case Lane.Mid:
                unitId = midLaneSelectedUnit.Value;
                break;
            case Lane.Bot:
                unitId = botLaneSelectedUnit.Value;
                break;
        }

        if (!unitParameters.ContainsKey(unitId))
        {
            Debug.LogError($"Unit parameters not defined for unit id: {unitId}");
            return;
        }
        UnitParameters parameters = unitParameters[unitId];

        // 从已绑定的 NetworkVariable 字典中获取该兵线的资源数据
        if (!laneResources.ContainsKey(lane))
        {
            Debug.LogError($"Lane resources for lane {lane} not found.");
            return;
        }
        
        // 获取当前兵线数据（注意：LaneResourceData 是值类型，必须在修改后重新赋值）
        LaneResourceData data = laneResources[lane].Value;

        if (data.availablePopulation >= parameters.costPopulation &&
            data.availableResource >= parameters.costResource)
        {
            data.availablePopulation -= parameters.costPopulation;
            data.availableResource -= parameters.costResource;
            data.production += parameters.addProduction;
            
            // 更新网络变量
            laneResources[lane].Value = data;
            
            SpawnUnitForLane(lane, parameters);
        }
        else
        {
            Debug.LogWarning($"Player {OwnerClientId} tried to spawn Unit{unitId} on lane {lane} but does not have sufficient resources/population.");
        }
    }
    #endregion

    #region 修改某条兵线选择兵种的接口
    /// <summary>
    /// UI 调用此接口修改指定兵线的选择兵种（例如切换到 unitRabbit）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetSelectedUnitForLaneServerRpc(Lane lane, int newUnitId, ServerRpcParams rpcParams = default)
    {
        // 可加入额外检查，比如 newUnitId 是否有效
        switch(lane)
        {
            case Lane.Top:
                topLaneSelectedUnit.Value = newUnitId;
                break;
            case Lane.Mid:
                midLaneSelectedUnit.Value = newUnitId;
                break;
            case Lane.Bot:
                botLaneSelectedUnit.Value = newUnitId;
                break;
        }
        // 使用 SessionManager 获取玩家名称
        string playerName = SessionManager.Instance.GetClientUsername(OwnerClientId);
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = $"Player {OwnerClientId}";
        }
        Debug.Log($"玩家 {playerName} 在 {lane} 路选择了单位 {newUnitId}");
    }
    #endregion

    #region 转线功能相关
    // 每条兵线的目标路线（默认为自己的路线）
    public NetworkVariable<Lane> topLaneTargetLane = new NetworkVariable<Lane>(Lane.Top);
    public NetworkVariable<Lane> midLaneTargetLane = new NetworkVariable<Lane>(Lane.Mid);
    public NetworkVariable<Lane> botLaneTargetLane = new NetworkVariable<Lane>(Lane.Bot);

    // 获取某条兵线当前的目标路线
    private Lane GetTargetLaneForLane(Lane sourceLane)
    {
        switch(sourceLane)
        {
            case Lane.Top:
                return topLaneTargetLane.Value;
            case Lane.Mid:
                return midLaneTargetLane.Value;
            case Lane.Bot:
                return botLaneTargetLane.Value;
            default:
                return sourceLane; // 默认返回自己
        }
    }

    // 设置某条兵线的目标路线
    [ServerRpc(RequireOwnership = false)]
    public void SetLaneTargetServerRpc(Lane sourceLane, Lane targetLane, ServerRpcParams rpcParams = default)
    {
        switch(sourceLane)
        {
            case Lane.Top:
                topLaneTargetLane.Value = targetLane;
                break;
            case Lane.Mid:
                midLaneTargetLane.Value = targetLane;
                break;
            case Lane.Bot:
                botLaneTargetLane.Value = targetLane;
                break;
        }
        
        // 使用 SessionManager 获取玩家名称
        string playerName = SessionManager.Instance.GetClientUsername(OwnerClientId);
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = $"Player {OwnerClientId}";
        }
        Debug.Log($"玩家 {playerName} 将 {sourceLane} 路的单位转至 {targetLane} 路");
    }
    #endregion

    // 不打野了
    // [ServerRpc(RequireOwnership = false)]
    // public void RequestSpawnJungleServerRpc(ServerRpcParams rpcParams = default)
    // {
    //     SpawnJungle(); // 服务器端生成单位
    // }

    // public void SpawnJungle()
    // {
    //     if (structures.ContainsKey("Base") && structures["Base"].Count > 0)
    //     {
    //         // 以玩家的第一个基地为参考位置生成单位
    //         Vector3 basePos = structures["Base"][0].transform.position;
    //         Vector3 junglePos = basePos + new Vector3(10, 0, 10);
    //         GameObject jungleObj = UnitSpawner.SpawnUnitFree(junglePrefab, junglePos, transform, OwnerClientId);

    //         Debug.Log($"Jungle spawned for Player {OwnerClientId} at position {junglePos}.");
    //     }
    //     else
    //     {
    //         Debug.LogError("Player base is not assigned.");
    //     }
    // }

    /// <summary>
    /// 销毁玩家的所有结构
    /// </summary>
    public void DestroyAllStructures()
    {
        foreach (var pair in structures)
        {
            foreach (GameObject obj in pair.Value)
            {
                if (obj != null)
                {
                    NetworkObject netObj = obj.GetComponent<NetworkObject>();
                    if (netObj.IsSpawned)
                    {
                        netObj.Despawn(true);
                    }
                }
            }
        }
        structures.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        // if (IsOwner) // 只有拥有此 Player 的客户端可以控制
        // {
        //     if (Input.GetKeyDown(KeyCode.U)) // 按下 U 键生成 Unit
        //     {
        //         RequestSpawnJungleServerRpc();
        //     }
        // }
    }
}