using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种4：鸡
/// </summary>
public class UnitChicken : UnitBase                             // 轻克轻，忍者，被猫克
{
    private void Awake()
    {
        costPopulation = 1;
        costResource = 50;
        addProduction = 8;                                      //

        moveSpeed = 9f;                                         // 速度一般
        
        maxHealth = 80f;                                        //
        armorType = 1;
        damageALight = 18;                                      //
        damageAHeavy = 6;

        armorALight = 5;                                        // 护甲很低
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 0;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 15f;                           // 
        attackRange = 10f;                                      // 攻击距离
        attackSpeed = 1.7f;                                     // 攻击速度较慢
    }
}
