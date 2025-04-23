using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种3：猫
/// </summary>
public class UnitCat : UnitBase                                 // 重克轻，武士，被狗克
{
    private void Awake()
    {
        costPopulation = 1;
        costResource = 40;
        addProduction = 6;

        moveSpeed = 12f;                                        // 敏捷

        maxHealth = 88f;                                        // 
        armorType = 2;                                          // 重甲
        damageALight = 18;                                      // 伤害稍高
        damageAHeavy = 6;

        armorALight = 25;                                       // 护甲稍好
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 5;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 20f;                           // 索敌范围
        attackRange = 8f;                                       // 攻击距离
        attackSpeed = 1.9f;                                     // 攻击速度稍快
    }
}
