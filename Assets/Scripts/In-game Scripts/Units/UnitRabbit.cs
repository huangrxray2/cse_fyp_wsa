using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // 如果 UnitBase 或特定逻辑需要，请包含

/// <summary>
/// 兵种2：兔子
/// </summary>
public class UnitRabbit : UnitBase                              // 轻均衡，新兵2，被猫、鸡克
{
    private void Awake()
    {
        costPopulation = 1;
        costResource = 30;
        addProduction = 6;

        moveSpeed = 14f;                                        // 速度快

        maxHealth = 100f;                                        // 生命值较低
        armorType = 1;                                          // 轻甲类型
        damageALight = 8;                                       // 伤害较低
        damageAHeavy = 8;

        armorALight = 10;                                       // 护甲低
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 0;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 15f;                           //
        attackRange = 4f;                                       // 攻击距离
        attackSpeed = 2.0f;                                     // 攻击速度较快
    }
}
