using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class UnitDisplay : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText; // 可以在 Inspector 中拖放引用
    private Transform infoCanvas;

    // 当前的名称和颜色（转线后）
    public string CurrentName { get; private set; }
    public Color CurrentColor { get; private set; }

    private void Awake()
    {
        // 尝试查找层级结构中的组件
        if (nameText == null)
        {
            // 找到 InfoCanvas
            infoCanvas = transform.Find("InfoCanvas");
            if (infoCanvas != null)
            {
                // 找到 NamePanel
                Transform namePanel = infoCanvas.Find("NamePanel");
                if (namePanel != null)
                {
                    // 找到 NameText
                    nameText = namePanel.GetComponentInChildren<TMP_Text>();
                }
            }
        }
    }

    [ClientRpc]
    public void SetColorAndNameClientRpc(Color color, string name)
    {
        // 存储当前的名称和颜色
        CurrentName = name;
        CurrentColor = color;
        
        // 如果在 Awake 中未找到，再次尝试查找
        if (nameText == null)
        {
            if (infoCanvas == null)
            {
                infoCanvas = transform.Find("InfoCanvas");
            }
            
            if (infoCanvas != null)
            {
                Transform namePanel = infoCanvas.Find("NamePanel");
                if (namePanel != null)
                {
                    nameText = namePanel.GetComponentInChildren<TMP_Text>();
                }
            }
        }

        // 设置文本和颜色
        if (nameText != null)
        {
            nameText.text = name;  // 设置文本
            nameText.color = color; // 设置颜色
        }
        else
        {
            Debug.LogWarning($"在单位 {gameObject.name} 中找不到文本组件，路径应为: InfoCanvas/NamePanel/NameText");
        }
    }
}