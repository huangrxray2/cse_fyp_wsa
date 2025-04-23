using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public struct LaneResourceData : INetworkSerializable
{
    public int availablePopulation;
    public int availableResource;
    public int production;

    public LaneResourceData(int pop, int resource, int prod)
    {
        availablePopulation = pop;
        availableResource = resource;
        production = prod;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref availablePopulation);
        serializer.SerializeValue(ref availableResource);
        serializer.SerializeValue(ref production);
    }
}