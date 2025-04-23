using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Jungle 单位类，允许玩家自由控制移动
/// </summary>
public class Jungle : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField]
    private float moveSpeed = 10;
    [SerializeField]
    private float turnSpeed = 100;
    [SerializeField]
    private TMP_Text nameLabel; // 使用 TextMeshPro - Text (UI)

    private NetworkVariable<Vector3> networkPlayerPos = new NetworkVariable<Vector3>(Vector3.zero);
    private NetworkVariable<Quaternion> networkPlayerRot = new NetworkVariable<Quaternion>(Quaternion.identity);

    private NetworkVariable<ulong> clientId = new NetworkVariable<ulong>();

    private Color[] playerColors = { Color.blue, Color.red, Color.green, Color.magenta }; // 需要根据加入者修改

    void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        if(this.IsServer)
        {
            // Debug.Log("Player OnNetworkSpawn as Server, id = " + OwnerClientId);
            clientId.Value = OwnerClientId;
        }
    }

    /// <summary>
    /// 自由移动逻辑，允许玩家通过输入控制单位移动
    /// </summary>
    /// <param name="v">垂直输入</param>
    /// <param name="h">水平输入</param>
    // Update is called once per frame
    void Update()
    {
        if (!IsClient || !IsOwner)
        {
            // Debug.Log($"Unit OwnerClientId: {this.OwnerClientId}, CallerClientId: {NetworkManager.Singleton.LocalClientId}");
            // Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is controlling Unit owned by {this.OwnerClientId}");
            return; // 只有拥有此单位的客户端可以控制
        }

        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        if (v != 0 || h != 0)
        {
            MoveServerRpc(v, h);
        }
    }

    [ServerRpc]
    void MoveServerRpc(float v, float h)
    {
        Vector3 delta = transform.forward * v * moveSpeed * Time.deltaTime;
        Vector3 pos = rb.position + delta;
        Quaternion rot = Quaternion.Euler(0, h * turnSpeed * Time.deltaTime, 0) * rb.rotation;
        rb.MovePosition(pos);
        rb.MoveRotation(rot);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("UnitTest"))
        {
            if(this.IsOwner)
            {
                // Debug.Log("This is UnitTest.");
                UnitTest ut = other.gameObject.GetComponent<UnitTest>();
                ut.SetActive(false);
            }
        }
    }
}
