using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Structure : NetworkBehaviour
{
    public enum StructureType { Base, HilltopTower, InnerTower, OuterTower }

    // 添加静态事件用于通知建筑生成
    public static event System.Action<GameObject, ulong> OnStructureSpawned;

    [Header("建筑设置")]
    public StructureType structureType;
    public ulong ownerClientId; // 记录建筑归属（由玩家创建时赋值）
    private string ownerName; // 将 ownerName 移到成员变量中

    public int maxHealth;  // 最大生命值，这个值将在 OnNetworkSpawn 方法中设置

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(); // 用 NetworkVariable 同步当前生命值（仅服务器写入，客户端只读）

    // 添加静态事件用于通知塔被摧毁
    public static event System.Action<Structure> OnStructureDestroyed;

    // 添加兵线属性
    public Lane? assignedLane;

    [Header("攻击设置")]
    [SerializeField] private GameObject bulletPrefab;
    private float attackRange;
    private float attackInterval;
    private int attackDamage;

    private UnitBase currentTarget;

    private Coroutine attackCoroutine;

    [ClientRpc]
    public void SyncStructureTypeClientRpc(StructureType type)
    {
        structureType = type;
        // Debug.Log($"客户端同步建筑 {gameObject.name} 的类型为 {type}");
    }

    public override void OnNetworkSpawn()
    {
        // if (!IsServer) return;

        // 触发建筑生成事件
        OnStructureSpawned?.Invoke(gameObject, ownerClientId);

        if (IsServer)
        {
            // Debug.Log($"建筑 {gameObject.name} 的类型为 {structureType}，初始化前的 maxHealth: {maxHealth}");

        // 根据建筑类型设置最大生命值
        switch (structureType)
        {
            case StructureType.Base:
                maxHealth = 300;
                attackRange = 100f;
                attackInterval = 0.5f;
                attackDamage = 100;
                break;
            case StructureType.HilltopTower:
                maxHealth = 200;
                attackRange = 75f;
                attackInterval = 0.75f;
                attackDamage = 50;
                break;
            case StructureType.InnerTower:
                maxHealth = 200;
                attackRange = 75f;
                attackInterval = 0.75f;
                attackDamage = 50;
                break;
            case StructureType.OuterTower:
                maxHealth = 200;
                attackRange = 75f;
                attackInterval = 0.75f;
                attackDamage = 50;
                break;
            default:
                Debug.LogError($"未知的建筑类型: {structureType}");
                maxHealth = 50;
                break;
        }
        currentHealth.Value = maxHealth;

        // Debug.Log($"建筑 {gameObject.name} 的类型为 {structureType}，初始化后的 maxHealth: {maxHealth}，currentHealth: {currentHealth.Value}");

        // 添加触发器SphereCollider检测进入范围
        SphereCollider sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = attackRange;

        // 启动攻击流程
        attackCoroutine = StartCoroutine(AttackRoutine());

        // 初始化 ownerName
        ownerName = SessionManager.Instance.GetClientUsername(ownerClientId);
        if (string.IsNullOrEmpty(ownerName))
        {
            ownerName = $"Player {ownerClientId}";
        }

        // 同步建筑类型到所有客户端
        SyncStructureTypeClientRpc(structureType);
        }
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            // 如果当前目标无效（死亡或脱离），取消锁定
            if (currentTarget == null || !currentTarget.IsSpawned || currentTarget.NetworkObject == null )
            {
                currentTarget = null;
                // 重新锁定最近的目标
                Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
                float closest = float.MaxValue;
                foreach (var hit in hits)
                {
                    UnitBase ub = hit.GetComponent<UnitBase>();
                    if (ub != null && ub.NetworkObject.OwnerClientId != ownerClientId)
                    {
                        float d = Vector3.Distance(transform.position, ub.transform.position);
                        if (d < closest)
                        {
                            closest = d;
                            currentTarget = ub;
                        }
                    }
                }
            }

            if (currentTarget != null)
            {
                FireBullet(currentTarget.transform);
                yield return new WaitForSeconds(attackInterval);
            }
            else
            {
                // 没有目标时，每帧检查
                yield return null;
            }
        }
    }

    // 攻击敌方单位
    private void FireBullet(Transform target)
    {
        if (target == null || target.GetComponent<NetworkObject>() == null)
        {
            // Debug.LogError("无法发射炮弹：目标为空或没有 NetworkObject 组件");
            return;
        }

        // 以防御塔的 x/z 坐标和固定 y=50 生成子弹
        Vector3 spawnPosition = new Vector3(transform.position.x, 50f, transform.position.z);
        GameObject b = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        
        // 获取子弹组件
        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null)
        {
            // Debug.LogError("生成的子弹预制体没有 Bullet 组件");
            Destroy(b);
            return;
        }
        
        // 获取网络对象
        NetworkObject netObj = b.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("生成的子弹预制体没有 NetworkObject 组件");
            Destroy(b);
            return;
        }

        // 先同步生成子弹
        netObj.Spawn();
        
        // 然后在服务器上初始化它
        bullet.SetTargetServerRpc(target.GetComponent<NetworkObject>().NetworkObjectId, attackDamage, ownerClientId);
        
        // Debug.Log($"已发射子弹，目标为 {target.name}，ID: {target.GetComponent<NetworkObject>().NetworkObjectId}");
    }

    /// <summary>
    /// 让建筑受到伤害
    /// </summary>
    /// <param name="damage">伤害数值</param>
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        // Debug.Log($"属于 {ownerName} 的建筑 [{structureType}] 承受 {damage} 点伤害. 剩余血量: {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            DestroyStructure();
        }
    }

    private void DestroyStructure()
    {
        // 触发塔被摧毁事件
        OnStructureDestroyed?.Invoke(this);

        // 播放销毁动画或特效
        Debug.Log($"属于 {ownerName} 的建筑 [{structureType}] 被摧毁.");
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        var net = GetComponent<NetworkObject>();
        if (net.IsSpawned)
            net.Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (currentTarget != null) return;
        UnitBase ub = other.GetComponent<UnitBase>();
        if (ub != null && ub.NetworkObject.OwnerClientId != ownerClientId)
        {
            currentTarget = ub;
        }
    }
}
