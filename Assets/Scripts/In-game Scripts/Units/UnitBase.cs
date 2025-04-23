using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 所有单位的基类，用于封装共有参数和行为
/// </summary>
public abstract class UnitBase : NetworkBehaviour
{
    [Header("基础参数（晋升后的不显示在这里）")]
    public int costPopulation;              // 消耗人口
    public float costResource;              // 消耗资源
    public int addProduction;               // 出兵后增加的产出

    [Header("移动参数")]
    public float moveSpeed;                 // 移动速度
    public float waypointThreshold = 1f;    // 达到航点的阈值
    public float rotationSpeed = 5f;        // 旋转平滑速度

    [Header("战斗参数")]
    public float maxHealth;                 // 生命值
    public float currentHealth;             // 当前生命值
    public int armorType;                   // 护甲类型，1 = 轻甲，2 = 重甲
    public float damageALight;              // 对轻甲伤害，Damage against Light Armor
    public float damageAHeavy;              // 对重甲伤害，Damage against Heavy Armor

    public int armorALight;                 // 对轻甲单位护甲，Armor against Light Unit
    public float dtrALight;                 // Damage Taken Rate against Light Unit，对轻受伤率
    

    public int armorAHeavy;                 // 对重甲单位护甲，Armor against Light Unit
    public float dtrAHeavy;                 // Damage Taken Rate against Heavy Unit，对重受伤率

    public float targetAcquisitionRange;    // 索敌范围
    public float attackRange;               // 攻击范围
    public float attackSpeed;               // 攻击速度
    public float timeBetweenAttacks;        // 攻击间隔（由 attackSpeed 计算得出：1 / attackSpeed）

    // 自然行进相关（路线与索引）移动路线和当前目标航点索引
    private List<Vector3> route;
    private int currentWaypointIndex = 0;

    // 记录单位所属兵线及阵营（红方为 true，蓝方为 false）
    private Lane assignedLane;
    private bool isRedTeam;

    private Rigidbody rb;

    // 添加静态事件用于通知单位生成
    public static event System.Action<GameObject, ulong> OnUnitSpawned;

    // 控制额外施加的加速度大小（用于下落）
    public float extraDownwardAcceleration = 20f;

    // 当前锁定的目标（可以是 UnitBase 或 Structure）
    private Component currentTarget;

    // // 攻击时间计时器
    // private float lastAttackTime = 0f;

    // 攻击协程句柄
    private Coroutine attackRoutine;

    // private void Awake() // 不知道为什么不会被调用
    // {
    //     rb = GetComponent<Rigidbody>();
    //     if(rb == null)
    //     {
    //         Debug.LogError("Rigidbody not found on unit prefab!");
    //     }
    //     else
    //     {
    //         Debug.Log("找到刚体" + rb);
    //     }
    // }

    #region 初始化与路线

    // 初始化路线
    /// <summary>
    /// 初始化单位：设置所属兵线、阵营以及行进路线
    /// </summary>
    public void InitializeUnit(Lane lane, bool isRed, List<Vector3> assignedRoute)
    {
        assignedLane = lane;
        isRedTeam = isRed;
        route = assignedRoute;
        currentWaypointIndex = 0;

        // 初始化当前生命值为最大生命值
        currentHealth = maxHealth;
    }

    #endregion

    #region Unity 生命周期

    // 在服务器端启动攻击逻辑
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 触发单位生成事件
        OnUnitSpawned?.Invoke(gameObject, OwnerClientId);

        if (IsServer)
        {
            // 根据攻击速度计算攻击间隔
            timeBetweenAttacks = 1f / attackSpeed;
            // 启动攻击协程
            attackRoutine = StartCoroutine(CombatRoutine());
        }
    }

    public override void OnDestroy()
    {
        // 原有的 OnDestroy 代码
        if (IsServer)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody not found in FixedUpdate!");
                return;
            }
        }
        // 持续施加向下的加速度
        rb.AddForce(Vector3.down * extraDownwardAcceleration, ForceMode.Acceleration);

        // // 如果当前没有锁定目标，则让单位自然行进
        // if (IsServer && currentTarget == null && route != null && currentWaypointIndex < route.Count)
        // {
        //     // 没有索敌到目标时，恢复自然行进
        //     if (currentWaypointIndex == 0)
        //     {
        //         TrySkipInitialWaypoint();
        //     }
        //     MoveHorizontally();
        // }
        if (IsServer && route != null && currentWaypointIndex < route.Count)
        {
            if (currentTarget != null)
            {
                // 有目标就朝目标移动
                MoveTowardsTarget(currentTarget.transform.position);
            }
            else
            {
                // 没有目标就正常行进
                if (currentWaypointIndex == 0)
                {
                    TrySkipInitialWaypoint();
                }
                MoveHorizontally();
            }
        }

        // 检测是否接近地面，并冻结 Y 轴运动
        FreezeYAxisWhenGrounded();
    }

    #endregion

    #region 攻击协程逻辑

    /// <summary>
    /// 周期性检测目标并进行攻击的协程
    /// </summary>
    private IEnumerator CombatRoutine()
    {
        while (true)
        {
            // 检测索敌范围内的目标（优先兵种，其次建筑）
            currentTarget = DetectTarget();

            if (currentTarget != null)
            {
                float targetDistance = Vector3.Distance(transform.position, currentTarget.transform.position);

                // 如果目标是敌方兵种，则按原有逻辑：到达攻击范围后进行攻击
                if (currentTarget is UnitBase enemyUnit)
                {
                    // if (targetDistance > attackRange)
                    // {
                    //     MoveTowardsTarget(currentTarget.transform.position);
                    // }
                    // else
                    if (targetDistance <= attackRange)
                    {
                        Attack(enemyUnit);
                        yield return new WaitForSeconds(timeBetweenAttacks);
                        continue; // 继续下一轮检测
                    }
                }
                // // 如果目标是敌方建筑，则始终朝目标行进，不在远处发起攻击
                // else if (currentTarget is Structure enemyStructure)
                // {
                //     // 始终移动至建筑方向，等待碰撞发生时触发伤害和自我销毁
                //     MoveTowardsTarget(currentTarget.transform.position);
                // }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
                yield return null;
            }
            else
            {
                // 没有目标时，等待较短时间后再检测
                yield return new WaitForSeconds(0.1f);
            }

            // 每帧检测一次（或者稍微等待一小段时间以减小性能开销）
            yield return null;
        }
    }

    #endregion

    #region 攻击逻辑

    /// <summary>
    /// 根据 targetAcquisitionRange 检测目标：优先返回敌方兵种，其次返回敌方建筑
    /// </summary>
    /// <returns>返回检测到的目标（UnitBase 或 Structure），若无则返回 null</returns>
    private Component DetectTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetAcquisitionRange);
        UnitBase closestSoldier = null;
        float closestSoldierDistance = float.MaxValue;
        Structure closestStructure = null;
        float closestStructureDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            // 排除自身
            if (hit.gameObject == gameObject)
                continue;

            // 尝试检测敌方兵种
            UnitBase enemyUnit = hit.GetComponent<UnitBase>();
            if (enemyUnit != null && enemyUnit.NetworkObject.OwnerClientId != NetworkObject.OwnerClientId)
            {
                float dist = Vector3.Distance(transform.position, enemyUnit.transform.position);
                if (dist < closestSoldierDistance)
                {
                    closestSoldierDistance = dist;
                    closestSoldier = enemyUnit;
                }
            }
            else
            {
                // 如果不是兵种，则尝试检测敌方建筑
                Structure enemyStructure = hit.GetComponent<Structure>();
                if (enemyStructure != null && enemyStructure.ownerClientId != OwnerClientId)
                {
                    float dist = Vector3.Distance(transform.position, enemyStructure.transform.position);
                    if (dist < closestStructureDistance)
                    {
                        closestStructureDistance = dist;
                        closestStructure = enemyStructure;
                    }
                }
            }
        }
        // 优先返回敌方兵种
        if (closestSoldier != null)
            return closestSoldier;
        else
            return closestStructure;
    }

    /// <summary>
    /// 向指定目标位置移动（仅水平移动，保持当前 y 值）
    /// </summary>
    /// <param name="targetPos">目标位置</param>
    private void MoveTowardsTarget(Vector3 targetPos)
    {
        Vector3 currentPos = rb.position;
        Vector3 targetHorizontal = new Vector3(targetPos.x, currentPos.y, targetPos.z);
        Vector3 direction = targetHorizontal - currentPos;
        float distanceThisFrame = moveSpeed * Time.fixedDeltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
        Vector3 move = direction.normalized * distanceThisFrame;
        rb.MovePosition(currentPos + move);
    }

    #endregion

    #region 自然行进

    private void MoveHorizontally()
    {
        Vector3 target = route[currentWaypointIndex];
        if (rb == null)
        {
            Debug.LogError("Rigidbody is null, can't move the unit.");
            return;
        }
        Vector3 currentPos = rb.position;
        // 保留当前 y，目标只考虑 x 和 z
        Vector3 horizontalTarget = new Vector3(target.x, currentPos.y, target.z);
        Vector3 direction = horizontalTarget - currentPos;
        float distanceThisFrame = moveSpeed * Time.fixedDeltaTime;

        // 若足够接近当前目标，则认为到达（仅更新水平位置）
        if (direction.magnitude <= waypointThreshold)
        {
            rb.MovePosition(new Vector3(target.x, currentPos.y, target.z));
            currentWaypointIndex++;
        }
        else
        {
            // 平滑旋转：只根据水平方向计算旋转
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            }
            // 水平移动：使用 MovePosition 更新 x 和 z，保持当前 y 由重力计算
            Vector3 move = direction.normalized * distanceThisFrame;
            rb.MovePosition(currentPos + move);
        }
    }

    private void FreezeYAxisWhenGrounded()
    {
        // 使用射线检测单位底部离地面的距离（假设射线从单位中心向下发射）
        RaycastHit hit;
        float rayDistance = 1f; // 根据单位大小调整
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance + 0.1f))
        {
            // 假设 hit.point.y 就是地面高度，此处可以留一定余量
            if (transform.position.y - hit.point.y < 0.5f)
            {
                // 当单位距离地面很近时，冻结 Y 轴（同时保留 XZ 的移动）
                RigidbodyConstraints currentConstraints = rb.constraints;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                // 如果你希望单位保持当前高度，可以在这里强制设置 Y 坐标
                Vector3 pos = rb.position;
                pos.y = hit.point.y; // 或加上一个偏移量
                rb.position = pos;
            }
        }
        else
        {
            // 未检测到地面时，可以恢复部分约束（注意不要影响水平运动）
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    #endregion

    #region 攻击逻辑

    /// <summary>
    /// 对敌方兵种单位进行一次攻击
    /// </summary>
    /// <param name="enemy">敌方兵种</param>
    private void Attack(UnitBase enemy)
    {
        if (enemy == null)
            return;

        float actualDamage = 0f;
        // 根据双方的护甲类型计算实际伤害：
        // 攻击者和被攻击者均为轻甲
        if (this.armorType == 1 && enemy.armorType == 1)
        {
            actualDamage = damageALight * enemy.dtrALight;
        }
        // 攻击者和被攻击者均为重甲
        else if (this.armorType == 2 && enemy.armorType == 2)
        {
            actualDamage = damageAHeavy * enemy.dtrAHeavy;
        }
        // 轻甲单位攻击重甲单位
        else if (this.armorType == 1 && enemy.armorType == 2)
        {
            actualDamage = damageAHeavy * enemy.dtrALight;
        }
        // 重甲单位攻击轻甲单位
        else if (this.armorType == 2 && enemy.armorType == 1)
        {
            actualDamage = damageALight * enemy.dtrAHeavy;
        }

        enemy.TakeDamage(actualDamage);
        // Debug.Log($"{gameObject.name} attacked {enemy.gameObject.name} for {actualDamage:F3} damage. Enemy health: {enemy.currentHealth:F3}");
    }

    /// <summary>
    /// 对敌方建筑进行一次攻击
    /// </summary>
    /// <param name="structure">敌方建筑</param>
    private void AttackStructure(Structure structure)
    {
        if (structure == null)
            return;
        // 这里采用单位消耗的人口作为伤害值（也可以根据需要调整）
        int damage = costPopulation;
        structure.TakeDamage(damage);
        Debug.Log($"{gameObject.name} attacked structure {structure.structureType} for {damage} damage.");
    }

    /// <summary>
    /// 处理单位受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        // Debug.Log($"{gameObject.name} takes {damage:F3} damage, remaining health: {currentHealth:F3}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 单位死亡逻辑（仅服务器端处理）
    /// </summary>
    protected virtual void Die()
    {
        if (IsServer)
        {
            // Debug.Log($"{gameObject.name} is destroyed.");
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    #endregion

    #region 碰撞攻击（保留原有建筑碰撞检测，可根据需求选择保留或移除）

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // 检查是否与敌方建筑碰撞（兵种间攻击由上面的检测与追击处理）
        Structure structure = collision.gameObject.GetComponent<Structure>();
        if (structure != null)
        {
            if (structure.ownerClientId != OwnerClientId)
            {
                int damage = costPopulation;
                structure.TakeDamage(damage);
                // Debug.Log($"单位 [{gameObject.name}] 碰撞并攻入敌方建筑 {structure.structureType}, 造成 {damage} 点伤害.");
                // 单位在攻击后销毁
                Die();
            }
        }
    }

    #endregion

    /// <summary>
    /// 根据单位所属兵线和阵营判断是否满足提前出发的条件，
    /// 若满足则直接跳过第一个航点。
    /// </summary>
    private void TrySkipInitialWaypoint()
    {
        Vector3 pos = transform.position;
        if (assignedLane == Lane.Top)
        {
            if (isRedTeam)
            {
                if (pos.x >= -250f && pos.x <= -150f)
                {
                    currentWaypointIndex = 1;
                }
            }
            else
            {
                if (pos.z >= 150f && pos.z <= 250f)
                {
                    currentWaypointIndex = 1;
                }
            }
        }
        else if (assignedLane == Lane.Mid)
        {
            if (pos.z >= pos.x - 50f && pos.z <= pos.x + 50f)
            {
                currentWaypointIndex = 1;
            }
        }
        else if (assignedLane == Lane.Bot)
        {
            if (isRedTeam)
            {
                if (pos.z >= -250f && pos.z <= -150f)
                {
                    currentWaypointIndex = 1;
                }
            }
            else
            {
                if (pos.x >= 150f && pos.x <= 250f)
                {
                    currentWaypointIndex = 1;
                }
            }
        }
    }

    // 添加晋升状态属性
    private int promotionLevel = 1;
    private bool isCavalry = false;
    private bool isFortressGuard = false;
    private bool isHeavyInfantry = false;

    // 设置晋升状态的方法
    public void SetPromotionStatus(int level, bool cavalry, bool fortressGuard, bool heavyInfantry)
    {
        promotionLevel = level;
        isCavalry = cavalry;
        isFortressGuard = fortressGuard;
        isHeavyInfantry = heavyInfantry;
        
        // 更新单位显示名称以反映晋升状态
        UpdateUnitDisplayName();
    }

    // 更新单位显示名称
    private void UpdateUnitDisplayName()
    {
        if (!IsServer) return;
        
        UnitDisplay display = GetComponent<UnitDisplay>();
        if (display == null) return;
        
        string typeName = gameObject.name.Replace("(Clone)", "").Trim();
        string laneName = "";
        
        // 根据assignedLane获取兵线名称
        if (assignedLane == Lane.Top) laneName = "Top";
        else if (assignedLane == Lane.Mid) laneName = "Mid";
        else if (assignedLane == Lane.Bot) laneName = "Bot";
        
        // 构建晋升状态描述
        string promotionDesc = "";
        if (promotionLevel > 1) promotionDesc += $"Lv{promotionLevel} ";
        if (isCavalry) promotionDesc += "骑兵 ";
        if (isFortressGuard) promotionDesc += "守卫 ";
        if (isHeavyInfantry) promotionDesc += "重装 ";
        
        // 设置最终显示名称
        string displayName = $"{laneName} {promotionDesc}{typeName}";
        
        // 获取当前颜色
        Color currentColor = display.CurrentColor;
        
        // 更新显示
        display.SetColorAndNameClientRpc(currentColor, displayName);
    }
}