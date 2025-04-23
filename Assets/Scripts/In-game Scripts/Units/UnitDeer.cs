using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种9：鹿
/// </summary>
public class UnitDeer : UnitBase                                // 轻均衡，骑士
{
    private void Awake()
    {
        costPopulation = 2;
        costResource = 150;
        addProduction = 19;

        moveSpeed = 20f;                                        // 非常快

        maxHealth = 200f;                                          // 生命值中等
        armorType = 1;
        damageALight = 28;
        damageAHeavy = 28;

        armorALight = 25;
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 5;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 25f;                           // 索敌范围
        attackRange = 10f;                                       // 攻击距离
        attackSpeed = 1.7f;                                     // 攻击速度
    }
}
