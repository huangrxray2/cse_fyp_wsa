using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UnitTest : NetworkBehaviour
{
    private NetworkVariable<bool> networkIsActive = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        networkIsActive.OnValueChanged += (preValue, newValue) =>
        {
            this.gameObject.SetActive(newValue);
        };
        this.gameObject.SetActive(networkIsActive.Value);
    }

    public void SetActive(bool active)
    {
        if(this.IsServer)
        {
            networkIsActive.Value = active;
        }
        else if(this.IsClient)
        {
            SetNetworkActiveServerRpc(active);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetNetworkActiveServerRpc(bool active)
    {
        networkIsActive.Value = active;
    }
}
