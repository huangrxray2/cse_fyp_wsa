using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class TowerDisplay : NetworkBehaviour
{
    /// <summary>
    /// 通过 ClientRpc 动态设置塔的颜色和名称
    /// </summary>
    /// <param name="color">要设置的颜色</param>
    /// <param name="name">要显示的名称</param>
    [ClientRpc]
    public void SetColorAndNameClientRpc(Color color, string name)
    {
        // 修改塔的材质颜色
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        // 在塔预制体上查找 Canvas（假定 Canvas 采用 WorldSpace 渲染模式）
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            TMP_Text nameLabel = canvas.GetComponentInChildren<TMP_Text>();
            if (nameLabel != null)
            {
                nameLabel.text = name;
            }
        }
    }
}
