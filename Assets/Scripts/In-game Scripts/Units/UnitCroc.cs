using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 兵种12：鳄鱼
/// </summary>
public class UnitCroc : UnitBase                                // 重克重，不朽
{
    private void Awake()
    {
        costPopulation = 4;
        costResource = 560;
        addProduction = 58;

        moveSpeed = 6f;                                         // 陆地速度非常慢

        maxHealth = 610f;                                           // 生命值高
        armorType = 2;                                          // 重甲
        damageALight = 40;                                      //
        damageAHeavy = 92;

        armorALight = 45;                                       // 护甲不错
        dtrALight = 1f - armorALight / (armorALight + 100f);
        armorAHeavy = 25;
        dtrAHeavy = 1f - armorAHeavy / (armorAHeavy + 100f);

        targetAcquisitionRange = 20f;                           // 索敌范围
        attackRange = 6f;                                       // 攻击距离短但咬合力强
        attackSpeed = 0.9f;                                     // 攻击速度慢
    }
}
