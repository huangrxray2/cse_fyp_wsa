using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种14：犀牛
/// </summary>
public class UnitRhino : UnitBase                               // 重均衡，米拉奇
{
    private void Awake()
    {
        costPopulation = 5;
        costResource = 1500;
        addProduction = 137;

        moveSpeed = 12f;                                        // 比河马快

        maxHealth = 1780f;                                         // 生命值非常高
        armorType = 2;                                          // 重甲类型
        damageALight = 220;                                     // 伤害非常高
        damageAHeavy = 200;                                     // 对重甲极佳，穿甲

        armorALight = 65;                                       // 护甲非常高
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 60;                                       // 重甲极高
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 30f;                           // 索敌范围
        attackRange = 14f;                                      // 攻击距离
        attackSpeed = 1.3f;                                     // 攻击速度较慢
    }
}
