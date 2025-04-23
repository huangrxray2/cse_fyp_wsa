using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种7：熊猫
/// </summary>
public class UnitPanda : UnitBase                               // 重克重，剑士2，被花栗鼠克
{
    private void Awake()
    {
        costPopulation = 3;                                     // 人口消耗较高
        costResource = 150;
        addProduction = 15;

        moveSpeed = 7f;                                         // 速度慢

        maxHealth = 220f;                                          // 生命值高
        armorType = 2;                                          // 重甲
        damageALight = 18;                                      // 伤害不错
        damageAHeavy = 42;

        armorALight = 40;                                       // 护甲不错
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 20;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 15f;                           // 索敌范围
        attackRange = 7f;                                       // 近战攻击距离稍长
        attackSpeed = 1.2f;                                     // 攻击速度慢
    }
}
