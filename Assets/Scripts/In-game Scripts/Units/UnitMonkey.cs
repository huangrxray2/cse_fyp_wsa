using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种8：猴子
/// </summary>
public class UnitMonkey : UnitBase                              // 轻克轻，忍者2，被猫克制
{
    private void Awake()
    {
        costPopulation = 2;
        costResource = 78;
        addProduction = 12;

        moveSpeed = 15f;                                        // 非常快/敏捷

        maxHealth = 120f;                                          // 生命值中等
        armorType = 1;
        damageALight = 35;
        damageAHeavy = 17;

        armorALight = 20;
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 10;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 15f;                           // 索敌范围
        attackRange = 5f;                                       // 攻击距离
        attackSpeed = 2.1f;                                     // 攻击速度快
    }
}
