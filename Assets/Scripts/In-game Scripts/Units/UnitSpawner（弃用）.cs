using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// 单位生成器，将单位的生成逻辑封装到一个静态类中
/// </summary>
public static class UnitSpawner
{
    private static Color[] playerColors = { Color.blue, Color.red, Color.green, Color.magenta };

    /// <summary>
    /// 生成自由移动的单位（如 Jungle），不赋予路线
    /// </summary>
    /// <param name="unitPrefab">单位预制体</param>
    /// <param name="spawnPos">生成位置</param>
    /// <param name="parent">父对象</param>
    /// <param name="ownerClientId">所有者客户端ID</param>
    /// <returns>生成的单位对象</returns>

    /// 这些都废了

        // public static GameObject SpawnUnitFree(GameObject unitPrefab, Vector3 spawnPos, Transform parent, ulong ownerClientId)
        // {
        //     // 实例化单位
        //     GameObject unitObj = Object.Instantiate(unitPrefab, spawnPos, Quaternion.identity, parent);
        //     NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
        //     if (netObj != null)
        //     {
        //         netObj.SpawnWithOwnership(ownerClientId);
        //     }

        //     // 分配颜色和名称
        //     AssignColorAndName(unitObj, ownerClientId);

        //     return unitObj;
        // }

        // /// <summary>
        // /// 为指定对象分配颜色和名称。
        // /// </summary>
        // private static void AssignColorAndName(GameObject obj, ulong ownerClientId)
        // {
        //     // 获取房主的 ClientId，用于判断阵营（房主为红队，加入者为蓝队）
        //     ulong roomHostClientId = RoomManager.Instance.GetRoomHostClientId();
        //     bool isRedTeam = (ownerClientId == roomHostClientId);

        //     // 使用 SessionManager 获取玩家的用户名
        //     string playerName = SessionManager.Instance.GetClientUsername(ownerClientId);
        //     if (string.IsNullOrEmpty(playerName))
        //     {
        //         playerName = $"Player {ownerClientId}";  // 如果没有找到用户名，使用默认名称
        //     }

        //     // 根据阵营选择颜色
        //     Color assignedColor = isRedTeam ? Color.red : Color.blue;

        //     UnitDisplay display = obj.GetComponent<UnitDisplay>();
        //     if(display != null)
        //     {
        //         display.SetColorAndNameClientRpc(assignedColor, playerName);
        //     }
        // }
}