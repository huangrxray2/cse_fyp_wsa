using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class FogOfWarManager : NetworkBehaviour
{
    [Header("视野设置")]
    [SerializeField] private float visionRange = 60f; // 视野范围
    [SerializeField] private float updateInterval = 0.5f; // 视野更新间隔
    
    [Header("迷雾设置")]
    [SerializeField] private GameObject fogPlanePrefab; // 迷雾平面预制体
    [SerializeField] private Material fogMaterial; // 迷雾材质
    [SerializeField] private float fogHeight = 5f; // 迷雾高度
    
    // 地图范围
    private float mapMinX = -250f;
    private float mapMaxX = 250f;
    private float mapMinZ = -250f;
    private float mapMaxZ = 250f;
    
    // 迷雾平面
    private GameObject fogPlane;
    private Texture2D fogTexture;
    private int textureSize = 16; // 纹理大小，根据地图大小调整（目前地图是500*500）
    
    // 我方单位和建筑列表
    private List<GameObject> myUnits = new List<GameObject>();
    private List<GameObject> myStructures = new List<GameObject>();
    
    // 敌方单位和建筑列表
    private List<GameObject> enemyUnits = new List<GameObject>();
    private List<GameObject> enemyStructures = new List<GameObject>();
    
    // 视野更新协程
    private Coroutine visionUpdateCoroutine;
    
    // 单例实例
    public static FogOfWarManager Instance { get; private set; }
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsClient)
        {
            // 创建迷雾平面
            CreateFogPlane();
            
            // 开始视野更新
            visionUpdateCoroutine = StartCoroutine(UpdateVisionCoroutine());
            
            // 订阅单位和建筑生成事件
            UnitBase.OnUnitSpawned += RegisterUnit;
            Structure.OnStructureSpawned += RegisterStructure;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsClient)
        {
            // 取消订阅事件
            UnitBase.OnUnitSpawned -= RegisterUnit;
            Structure.OnStructureSpawned -= RegisterStructure;
            
            // 停止协程
            if (visionUpdateCoroutine != null)
            {
                StopCoroutine(visionUpdateCoroutine);
            }
            
            // 清理迷雾平面
            if (fogPlane != null)
            {
                Destroy(fogPlane);
            }
        }
    }
    
    private void CreateFogPlane()
    {
        // 创建迷雾平面
        fogPlane = Instantiate(fogPlanePrefab, new Vector3(0, fogHeight, 0), Quaternion.Euler(90, 0, 0));
        fogPlane.transform.localScale = new Vector3(
            (mapMaxX - mapMinX) / 1f, // 原先10是cell size，这是对于plane的，因为其默认大小是10！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
            (mapMaxZ - mapMinZ) / 1f, 
            1f
        );
        
        // 创建迷雾纹理
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;
        
        // 初始化纹理为全黑（不可见）
        Color[] colors = new Color[textureSize * textureSize];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
        }
        fogTexture.SetPixels(colors);
        fogTexture.Apply();
        
        // 设置迷雾材质
        Renderer renderer = fogPlane.GetComponent<Renderer>();
        if (renderer != null && fogMaterial != null)
        {
            Material instanceMaterial = new Material(fogMaterial);
            instanceMaterial.SetTexture("_MainTex", fogTexture);
            renderer.material = instanceMaterial;
        }
    }
    
    private IEnumerator UpdateVisionCoroutine()
    {
        while (true)
        {
            UpdateVision();
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    private void UpdateVision()
    {
        // 获取我方视野范围内的点
        HashSet<Vector2Int> visibleCells = new HashSet<Vector2Int>();
        
        // 从我方单位获取视野
        foreach (GameObject unit in myUnits)
        {
            if (unit != null)
            {
                AddVisibleCellsAroundPoint(unit.transform.position, visionRange, visibleCells);
            }
        }
        
        // 从我方建筑获取视野
        foreach (GameObject structure in myStructures)
        {
            if (structure != null)
            {
                AddVisibleCellsAroundPoint(structure.transform.position, visionRange, visibleCells);
            }
        }
        
        // 更新敌方单位可见性
        foreach (GameObject unit in enemyUnits)
        {
            if (unit != null)
            {
                bool isVisible = IsPointVisible(unit.transform.position, visibleCells);
                SetObjectVisibility(unit, isVisible);
            }
        }
        
        // 更新迷雾
        UpdateFogTexture(visibleCells);
    }
    
    private void AddVisibleCellsAroundPoint(Vector3 position, float radius, HashSet<Vector2Int> visibleCells)
    {
        // 将世界坐标转换为网格坐标
        int cellSize = 10; // 每个网格单元的大小 原先10！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
        int centerX = Mathf.FloorToInt((position.x - mapMinX) / cellSize);
        int centerZ = Mathf.FloorToInt((position.z - mapMinZ) / cellSize);
        int radiusCells = Mathf.CeilToInt(radius / cellSize);
        
        // 添加视野范围内的所有单元格
        for (int x = centerX - radiusCells; x <= centerX + radiusCells; x++)
        {
            for (int z = centerZ - radiusCells; z <= centerZ + radiusCells; z++)
            {
                if (x >= 0 && z >= 0)
                {
                    float distance = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(x, z)) * cellSize;
                    if (distance <= radius)
                    {
                        visibleCells.Add(new Vector2Int(x, z));
                    }
                }
            }
        }
    }
    
    private bool IsPointVisible(Vector3 position, HashSet<Vector2Int> visibleCells)
    {
        // 将世界坐标转换为网格坐标
        int cellSize = 10; // 原先10！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
        int x = Mathf.FloorToInt((position.x - mapMinX) / cellSize);
        int z = Mathf.FloorToInt((position.z - mapMinZ) / cellSize);
        
        return visibleCells.Contains(new Vector2Int(x, z));
    }
    
    private void SetObjectVisibility(GameObject obj, bool isVisible)
    {
        if (obj != null)
        {
            // 设置对象及其所有子对象的可见性
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isVisible;
            }

            // 设置对象及其所有子对象的 Canvas 可见性
            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true); // 使用 true 获取包括非激活状态的组件
            foreach (Canvas canvas in canvases)
            {
                canvas.enabled = isVisible;
            }
        }
    }
    
    private void UpdateFogTexture(HashSet<Vector2Int> visibleCells)
    {
        // 将可见单元格转换为纹理像素
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                // 将纹理坐标映射到网格坐标
                int gridX = Mathf.FloorToInt(x * (mapMaxX - mapMinX) / (textureSize * 10f)); // 原先10！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
                int gridY = Mathf.FloorToInt(y * (mapMaxZ - mapMinZ) / (textureSize * 10f));
                
                // 检查该网格是否可见
                bool isVisible = visibleCells.Contains(new Vector2Int(gridX, gridY));
                
                // 更新纹理像素
                fogTexture.SetPixel(x, y, isVisible ? Color.white : Color.black);
            }
        }
        
        // 应用纹理更改
        fogTexture.Apply();
    }
    
    public void RegisterUnit(GameObject unit, ulong ownerClientId)
    {
        // 判断是我方还是敌方单位
        if (ownerClientId == NetworkManager.Singleton.LocalClientId)
        {
            myUnits.Add(unit);
        }
        else
        {
            enemyUnits.Add(unit);
            // 默认敌方单位不可见
            SetObjectVisibility(unit, false);
        }
    }
    
    public void RegisterStructure(GameObject structure, ulong ownerClientId)
    {
        // 判断是我方还是敌方建筑
        if (ownerClientId == NetworkManager.Singleton.LocalClientId)
        {
            myStructures.Add(structure);
        }
        else
        {
            enemyStructures.Add(structure);
            // 敌方建筑始终可见
        }
    }
}
