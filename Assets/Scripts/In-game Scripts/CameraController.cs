using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    [Header("阵营设置")]
    [Tooltip("房主红true，加入者蓝false")]
    public bool isRedTeam = true;

    [Header("摄像机初始位置设置")]
    // 红方默认
    public Vector3 redInitialPosition = new Vector3(-150f, 200f, -200f);
    public Quaternion redInitialRotation = Quaternion.Euler(60f, 0f, 0f);
    // 蓝方默认（蓝方以120°观察，从右上角看）
    public Vector3 blueInitialPosition = new Vector3(150f, 200f, 300f);
    public Quaternion blueInitialRotation = Quaternion.Euler(120f, 0f, 180f);

    [Header("摄像机平移设置")]
    public float panBorderThickness = 50f;        // 鼠标触发移动的边缘厚度
    public Vector2 redPanLimitX = new Vector2(-240f, 240f);  // 红方X轴移动范围
    public Vector2 redPanLimitZ = new Vector2(-300f, 250f);  // 红方Z轴移动范围
    public Vector2 bluePanLimitX = new Vector2(-240f, 240f);  // 蓝方X轴移动范围
    public Vector2 bluePanLimitZ = new Vector2(-250f, 300f);  // 蓝方Z轴移动范围

    [Header("摄像机缩放设置")]
    public float scrollSpeed = 30f;               // 缩放速度
    public float minY = 20f;                      // 摄像机最低高度
    public float maxY = 300f;                      // 摄像机最高高度

    [Header("动态平移速度设置")]
    public float panSpeedMin = 30f;   // 当 y 较低时的平移速度
    public float panSpeedMax = 200f;  // 当 y 较高时的平移速度

    // [Header("额外限制")]

    // 内部辅助：根据阵营决定移动方向因子
    // 红方：factor = 1，蓝方：factor = -1
    private float teamMultiplier;

    void Start()
    {
        // 从 RoomManager 获取房主的 ClientId，并与本地 ClientId 比较
        if (RoomManager.Instance != null)
        {
            isRedTeam = (NetworkManager.Singleton.LocalClientId == RoomManager.Instance.GetRoomHostClientId());
            Debug.Log($"是红方吗：{isRedTeam}");
        }
        else
        {
            // Debug.LogWarning("[CameraController]：找不到RoomManager，默认使用 isRedTeam = true");
            // Debug.Log("默认使用isRedTeam为真");
        }

        // 根据阵营选择初始位置和旋转
        if (isRedTeam)
        {
            transform.position = redInitialPosition;
            transform.rotation = redInitialRotation;
            teamMultiplier = 1f;
        }
        else
        {            
            transform.position = blueInitialPosition;
            transform.rotation = blueInitialRotation;
            teamMultiplier = -1f;
        }
    }

    void Update()
    {
        Vector3 pos = transform.position;

        // 根据当前 y 值计算平移速度，在 minY 时为 panSpeedMin，maxY 时为 panSpeedMax
        float t = (pos.y - minY) / (maxY - minY);
        float dynamicPanSpeed = Mathf.Lerp(panSpeedMin, panSpeedMax, t);

        // 鼠标在屏幕上边缘向前（正Z方向）移动
        if (Input.mousePosition.y >= Screen.height - panBorderThickness)
        {
            pos.z += teamMultiplier * dynamicPanSpeed * Time.deltaTime;
        }
        // 鼠标在屏幕下边缘向后（负Z方向）移动
        if (Input.mousePosition.y <= panBorderThickness)
        {
            pos.z -= teamMultiplier * dynamicPanSpeed * Time.deltaTime;
        }
        // 鼠标在屏幕右边缘向右（正X方向）移动
        if (Input.mousePosition.x >= Screen.width - panBorderThickness)
        {
            pos.x += teamMultiplier * dynamicPanSpeed * Time.deltaTime;
        }
        // 鼠标在屏幕左边缘向左（负X方向）移动
        if (Input.mousePosition.x <= panBorderThickness)
        {
            pos.x -= teamMultiplier * dynamicPanSpeed * Time.deltaTime;
        }

        // 限制摄像机在X和Z方向的范围
        if (isRedTeam)
        {
            pos.x = Mathf.Clamp(pos.x, redPanLimitX.x, redPanLimitX.y);
            pos.z = Mathf.Clamp(pos.z, redPanLimitZ.x, redPanLimitZ.y);
        }
        else
        {
            pos.x = Mathf.Clamp(pos.x, bluePanLimitX.x, bluePanLimitX.y);
            pos.z = Mathf.Clamp(pos.z, bluePanLimitZ.x, bluePanLimitZ.y);
        }
        

        // 使用鼠标滚轮缩放摄像机视野时，y和z同时变动：
        // 滚轮输入产生 zoomDelta，令 z 增加 zoomDelta，同时 y 减少 1.732 * zoomDelta，
        // 满足“z每增加1, y就减少1.732”的要求。
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float zoomDelta = scroll * scrollSpeed * 100f * Time.deltaTime; // 往前推则 > 0
        
        
        // 如果 y 已达到下限且试图进一步降低，或 y 已达到上限且试图进一步提高，则忽略滚轮输入
        if ((scroll > 0f && pos.y <= minY) || (scroll < 0f && pos.y >= maxY))
        {
            zoomDelta = 0f;
        }

        pos.y -= 1.732f * zoomDelta;                                    // 往前推时，红-，蓝-
        pos.z += teamMultiplier * zoomDelta;                            // 往前推时，红+，蓝-

        // 限制 y 在规定范围内
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        if (isRedTeam)
        {
            // 限制 1.732 * z + y 在指定区间内，只调整 z 坐标
            float lowerLimit = 1.732f * (-250f) + 20f - 10f;            //（带有余量）
            float upperLimit = 1.732f * (250f) + 20f - 20f;             //（带有余量）
            float value = 1.732f * teamMultiplier * pos.z + pos.y;

            if (value > upperLimit)
            {
                pos.z = (upperLimit - pos.y) / 1.732f * teamMultiplier; // 复原
            }
            else if (value < lowerLimit)
            {
                pos.z = (lowerLimit - pos.y) / 1.732f * teamMultiplier;
            }
        }
        else
        {
            // 限制 1.732 * (-z) + y 在指定区间内，只调整 z 坐标
            float lowerLimit = 1.732f * (-250f) + 20f - 10f;            //（带有余量）
            float upperLimit = 1.732f * (250f) + 20f - 20f;             //（带有余量）
            float value = 1.732f * teamMultiplier * pos.z + pos.y;

            if (value > upperLimit)
            {
                pos.z = (upperLimit - pos.y) / 1.732f * teamMultiplier;
            }
            else if (value < lowerLimit)
            {
                pos.z = (lowerLimit - pos.y) / 1.732f * teamMultiplier;
            }
        }

        // 最后确保 z 仍在规定范围内
        if (isRedTeam)
        {
            pos.z = Mathf.Clamp(pos.z, redPanLimitZ.x, redPanLimitZ.y);
        }
        else
        {
            pos.z = Mathf.Clamp(pos.z, bluePanLimitZ.x, bluePanLimitZ.y);
        }

        transform.position = pos;
    }
}
