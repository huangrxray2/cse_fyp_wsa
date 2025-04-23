using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种1：继承自 UnitBase，配置专属于 UnitChipmunk 的参数
/// </summary>
public class UnitChipmunk : UnitBase                            // 轻克重，老兵，被鸡克
{
    private void Awake()
    {
        costPopulation = 1;
        costResource = 33;
        addProduction = 6;

        moveSpeed = 10f;                                        // 移动速度适中

        maxHealth = 75f;                                        // 基础生命值
        armorType = 1;                                          // 轻甲类型
        damageALight = 6;                                       // 对轻甲伤害
        damageAHeavy = 10;                                      // 对重甲伤害

        armorALight = 20;                                       // 轻甲护甲值
        dtrALight = 1f - armorALight / (armorALight + 100f);    // 计算轻甲伤害减免
        armorAHeavy = 5;        // 重甲护甲值 (较低)
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);    // 计算重甲伤害减免

        targetAcquisitionRange = 15f;                           // 索敌范围
        attackRange = 5f;                                       // 攻击距离
        attackSpeed = 1.8f;                                     // 攻击速度 (每秒攻击次数的倒数，值越小越快)
        // 攻击间隔由 UnitBase 的 OnNetworkSpawn 中设置（timeBetweenAttacks = 1 / attackSpeed）
    }

    // 可以添加 UnitChipmunk 特有的行为，例如特效、动画等
    // protected override void Start()
    // {
    //     base.Start(); // 调用基类的 Start 方法
    //     // 花栗鼠特有的初始化逻辑
    // }

    // protected override void Update()
    // {
    //     base.Update(); // 调用基类的 Update 方法
    //     // 花栗鼠特有的每帧逻辑
    // }
}

// 轻均衡，新兵、修女       ：2兔子、6鸭子、9鹿
// 轻克轻，忍者             ：4鸡、8猴子
// 轻克重                   ：1花栗鼠、11麋鹿、13河马
// 重均衡，木乃伊、铁甲、骑士：10熊、14犀牛
// 重克轻，武士             ：3猫
// 重克重，剑士、不朽       ：5狗、7熊猫、12鳄鱼

// 先出兔子攒钱，然后出猫或者鸡克制花栗鼠或兔子
// 等对面出猫，就出狗克制；对面出鸡，再用猫克制
// 对面出狗，我方花栗鼠和麋鹿直接混出
// 对面只要不出麋鹿河马，我方直接无脑熊犀牛
// 对方出重，我方鳄鱼河马犀牛随便出

// 简单来说，兔子，鸡，狗，猫，麋鹿，熊犀牛