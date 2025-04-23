using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种5：狗
/// </summary>
public class UnitDog : UnitBase                                 // 重克重，剑士，被花栗鼠克
{
    private void Awake()
    {
        costPopulation = 1;                                     // 人口消耗较高
        costResource = 40;
        addProduction = 7;

        moveSpeed = 13f;                                        // 速度较快

        maxHealth = 100f;                                       // 生命值尚可
        armorType = 2;                                          // 重甲
        damageALight = 10;                                      // 伤害不错
        damageAHeavy = 20;

        armorALight = 30;                                       // 护甲尚可
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 10;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 18f;                           // 索敌范围
        attackRange = 6f;                                       // 攻击距离
        attackSpeed = 1.6f;                                     // 攻击速度一般
    }
}
