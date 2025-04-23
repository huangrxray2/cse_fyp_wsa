using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种6：鸭
/// </summary>
public class UnitDuck : UnitBase                                // 轻均衡，修女，被猫、鸡克
{
    private void Awake()
    {
        costPopulation = 1;
        costResource = 51;
        addProduction = 9;

        moveSpeed = 8f;                                         // 速度较慢

        maxHealth = 100f;                                          //
        armorType = 1;
        damageALight = 12;
        damageAHeavy = 12;

        armorALight = 15;                                       // 护甲较低
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 5;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 20f;                           // 索敌范围
        attackRange = 10f;                                      // 攻击距离
        attackSpeed = 1.7f;                                     // 攻击速度
    }
}
