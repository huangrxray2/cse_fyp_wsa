using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种11：麋鹿
/// </summary>
public class UnitMoose : UnitBase                               // 轻克重，武僧
{
    private void Awake()
    {
        costPopulation = 3;
        costResource = 280;
        addProduction = 24;

        moveSpeed = 18f;                                        //

        maxHealth = 280;                                           // 生命值更高
        armorType = 1;                                          // 
        damageALight = 28;                                      //
        damageAHeavy = 48;

        armorALight = 60;                                       // 护甲更高
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 35;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 30f;                           // 索敌范围
        attackRange = 15f;                                      // 攻击距离
        attackSpeed = 1.5f;                                     //
    }
}
