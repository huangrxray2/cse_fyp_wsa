using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    private NetworkVariable<ulong> targetNetObjectId = new NetworkVariable<ulong>();
    private NetworkVariable<int> bulletDamage = new NetworkVariable<int>();
    private NetworkVariable<ulong> bulletOwnerClientId = new NetworkVariable<ulong>();
    private NetworkVariable<Vector3> lastMoveDirection = new NetworkVariable<Vector3>();

    // 本地引用
    private Transform targetTransform;
    public float speed = 15f;
    private bool isInitialized = false;

    // 记录上一次有目标时的飞行方向
    private Vector3 lastDirection;
    // 失去目标后计时
    private float timeSinceNoTarget = 0f;
    private const float maxNoTargetTime = 5f;

    // 在服务端设置目标的 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(ulong targetObjectId, int damage, ulong ownerClientId)
    {
        // Debug.Log($"服务器收到设置目标请求：目标ID={targetObjectId}, 伤害={damage}");
        targetNetObjectId.Value = targetObjectId;
        bulletDamage.Value = damage;
        bulletOwnerClientId.Value = ownerClientId;
        
        // 尝试立即获取目标
        TryGetTargetReference();
        
        // 如果找到目标，设置初始方向
        if (targetTransform != null)
        {
            Vector3 dir = (targetTransform.position - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                lastMoveDirection.Value = dir;
                // Debug.Log($"设置初始方向: {dir}");
            }
        }
        
        isInitialized = true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Debug.Log($"子弹生成! IsServer: {IsServer}, IsClient: {IsClient}");

        // 监听 NetworkVariable 变化
        targetNetObjectId.OnValueChanged += OnTargetIdChanged;
        
        if (IsServer)
        {
            // 确保初始移动方向不为零
            if (lastMoveDirection.Value == Vector3.zero)
            {
                lastMoveDirection.Value = Vector3.forward;
                // Debug.Log("设置初始方向为前方");
            }
        }

        // 尝试获取目标引用
        TryGetTargetReference();

        isInitialized = true;
    }

    private void OnTargetIdChanged(ulong previousValue, ulong newValue)
    {
        // Debug.Log($"目标变化: {previousValue} -> {newValue}");
        TryGetTargetReference();
    }

    private void TryGetTargetReference()
    {
        // 检查 NetworkManager 是否已准备好
        if (!NetworkManager.Singleton.IsListening) return;

        // 通过 NetworkObjectId 查找目标
        if (targetNetObjectId.Value != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId.Value, out NetworkObject netObj))
            {
                targetTransform = netObj.transform;
                // Debug.Log($"成功获取目标: {targetTransform.name} (ID: {targetNetObjectId.Value})");
            }
            else
            {
                // Debug.Log($"无法找到ID为 {targetNetObjectId.Value} 的网络对象");
                targetTransform = null;
            }
        }
        else
        {
            // Debug.Log("目标ID为0，无法获取目标");
            targetTransform = null;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 服务器和客户端都需要更新位置
        Vector3 moveDir;

        // 检查目标是否有效
        if (targetTransform != null)
        {
            // 目标存在——计算新的方向
            moveDir = (targetTransform.position - transform.position).normalized;
            
            // 服务器更新最后方向并重置计时
            if (IsServer)
            {
                lastMoveDirection.Value = moveDir;
                timeSinceNoTarget = 0f;
            }
        }
        else
        {
            // 使用最后记录的方向
            moveDir = lastMoveDirection.Value;

            // 如果方向为零，使用默认方向
            if (moveDir == Vector3.zero)
            {
                moveDir = Vector3.forward;
                if (IsServer)
                {
                    lastMoveDirection.Value = moveDir;
                    // Debug.LogWarning("移动方向为零，使用默认前进方向");
                }
            }
            
            // 服务器处理失去目标后的计时和销毁
            if (IsServer)
            {
                timeSinceNoTarget += Time.deltaTime;
                
                // 尝试重新获取目标引用
                TryGetTargetReference();
                
                // 超过时间后销毁
                if (timeSinceNoTarget >= maxNoTargetTime)
                {
                    // Debug.Log($"子弹超过{maxNoTargetTime}秒没有目标，准备销毁");
                    DestroyBullet();
                    return;
                }
            }
        }

        transform.position += moveDir * speed * Time.deltaTime;

        // 命中检测
        if (IsServer && targetTransform != null)
        {
            float distance = Vector3.Distance(transform.position, targetTransform.position);
            if (distance < 0.5f)
            {
                // Debug.Log($"子弹命中目标 {targetTransform.name}, 距离: {distance}");
                var unit = targetTransform.GetComponent<UnitBase>();
                if (unit != null && unit.NetworkObject.OwnerClientId != bulletOwnerClientId.Value)
                {
                    unit.TakeDamage(bulletDamage.Value);
                }
                DestroyBullet();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized || !IsServer) return;

        // Debug.Log($"子弹碰撞: {collision.gameObject.name}");

        var unit = collision.gameObject.GetComponent<UnitBase>();
        if (unit != null && unit.NetworkObject.OwnerClientId != bulletOwnerClientId.Value)
        {
            unit.TakeDamage(bulletDamage.Value);
            DestroyBullet();
        }

        // 如果希望子弹撞到其他任何东西也销毁，可以在这里统一销毁
        // DestroyBullet();
    }

    private void DestroyBullet()
    {
        if (!IsServer) return;

        // Debug.Log("销毁子弹");
        isInitialized = false;
        var net = GetComponent<NetworkObject>();
        if (net != null && net.IsSpawned)
            net.Despawn();
    }

    // 清理事件监听
    public override void OnNetworkDespawn()
    {
        targetNetObjectId.OnValueChanged -= OnTargetIdChanged;
        // Debug.Log("子弹从网络中移除");
        base.OnNetworkDespawn();
    }
}
