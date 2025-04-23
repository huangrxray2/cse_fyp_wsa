using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种10：熊
/// </summary>
public class UnitBear : UnitBase                                // 重均衡，木乃伊/铁甲
{
    private void Awake()
    {
        costPopulation = 4;                                     // 人口消耗高
        costResource = 260;
        addProduction = 31;

        moveSpeed = 10f;                                        // 速度慢

        maxHealth = 400f;                                          // 生命值非常高
        armorType = 2;                                          // 重甲
        damageALight = 50;                                      // 伤害高
        damageAHeavy = 40;                                      // 对重甲效果也不错

        armorALight = 50;                                       // 护甲高
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 30;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 18f;                           // 索敌范围
        attackRange = 8f;                                       // 攻击距离较远
        attackSpeed = 1.0f;                                     // 攻击速度非常慢
    }
}
