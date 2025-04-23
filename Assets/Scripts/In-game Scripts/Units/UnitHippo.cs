using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种13：河马
/// </summary>
public class UnitHippo : UnitBase                               // 轻克重，武僧2
{
    private void Awake()
    {
        costPopulation = 5;                                     // 人口消耗非常高
        costResource = 1080;
        addProduction = 98;

        moveSpeed = 7f;                                         // 速度慢

        maxHealth = 1340f;                                          // 生命值极高
        armorType = 1;                                          // 轻甲，和猪一样
        damageALight = 104;                                      // 伤害高
        damageAHeavy = 170;                                      // 对重甲效果好

        armorALight = 70;                                       // 护甲非常高
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 50;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 25f;                           // 索敌范围
        attackRange = 11f;                                       // 攻击距离
        attackSpeed = 1.0f;                                     // 攻击速度慢
    }
}
