﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityStandardAssets.Utility;

[Serializable]
public class AICarSyncSystem
{
    public enum Mode
    {
        None,
        Host,
        Client
    }

    struct SpawnAICarMsg : INetMessage
    {
        public int MessageId => (int)MsgId.S_SpawnAICar;
        public int PrefabIdx;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool SpawnDriver;
        public bool SpawnPassenger;

        public void Sync<T>(T synchronizer) where T : ISynchronizer
        {
            synchronizer.Sync(ref PrefabIdx);
            synchronizer.Sync(ref Position);
            synchronizer.Sync(ref Rotation);
        }
    }

    public AICar[] Prefabs;

    // Make sure to update the way these cars are logged if we
    // ever start supporting removal
    [NonSerialized]
    public List<PlayerAvatar> Cars = new List<PlayerAvatar>();

    public int CarsSpawnedSinceLastLog = 0;
    Mode _mode;
    UNetHost _host;
    public void InitHost(UNetHost host)
    {
        _mode = Mode.Host;
        _host = host;
    }

    public void InitClient(MessageDispatcher dispatcher)
    {
        _mode = Mode.Client;
        dispatcher.AddStaticHandler((int)MsgId.S_SpawnAICar, ClientHandleSpawnAICar);
        dispatcher.AddStaticHandler((int)MsgId.S_UpdateAICarPoses, ClientHandleUpdatePoses);
    }

    int FindPrefabIndex(AICar prefab)
    {
        for (int i = 0; i < Prefabs.Length; i++)
        {
            if (Prefabs[i] == prefab) return i;
        }
        return -1;
    }

    public AICar Spawn(CarSpawnParams parameters, bool yielding)
    {
        Assert.AreEqual(Mode.Host, _mode, "Only host can spawn synced objects");
        var prefabIdx = FindPrefabIndex(parameters.Car);
        Assert.AreNotEqual(-1, prefabIdx, $"The prefab {parameters.Car} was not added to NetworkingManager -> AICarSyncSystem -> Prefabs");
        var aiCar = GameObject.Instantiate(Prefabs[prefabIdx], parameters.SpawnPoint.position, parameters.SpawnPoint.rotation);
        aiCar.gameObject.layer = LayerMask.NameToLayer(yielding ? "Yielding" : "Car");
        aiCar.enabled = true;
        var waypointProgressTracker = aiCar.GetComponent<WaypointProgressTracker>();
        waypointProgressTracker.enabled = true;
        waypointProgressTracker.Init(parameters.Track);
        var avatar = aiCar.GetComponent<PlayerAvatar>();
        avatar.PassengerPuppet.SetActive(parameters.SpawnPassenger);
        avatar.DriverPuppet.SetActive(parameters.SpawnDriver);
        avatar.Initialize(false, PlayerSystem.InputMode.None, PlayerSystem.ControlMode.HostAI);
        var rb = aiCar.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.GetComponent<Rigidbody>().useGravity = true;
        Cars.Add(avatar);
        _host.BroadcastReliable(new SpawnAICarMsg()
        {
            PrefabIdx = prefabIdx,
            Position = parameters.SpawnPoint.position,
            Rotation = parameters.SpawnPoint.rotation,
            SpawnPassenger = parameters.SpawnPassenger,
            SpawnDriver = parameters.SpawnDriver
        });
        return aiCar;
    }

    private void ClientHandleSpawnAICar(ISynchronizer sync, int srcPlayerId)
    {
        var msg = NetMsg.Read<SpawnAICarMsg>(sync);
        var go = GameObject.Instantiate(Prefabs[msg.PrefabIdx], msg.Position, msg.Rotation);
        var avatar = go.GetComponent<PlayerAvatar>();
        avatar.PassengerPuppet.SetActive(msg.SpawnPassenger);
        avatar.DriverPuppet.SetActive(msg.SpawnDriver);
        avatar.Initialize(true, PlayerSystem.InputMode.None, PlayerSystem.ControlMode.HostAI);
        Cars.Add(avatar);
    }

    private void ClientHandleUpdatePoses(ISynchronizer sync, int srcPlayerId)
    {
        var msg = NetMsg.Read<UpdateAICarPosesMsg>(sync);
        for (int i = 0; i < msg.Poses.Count; i++)
        {
            Cars[i].ApplyPose(msg.Poses[i]);
        }
    }

    List<AvatarPose> _poses = new List<AvatarPose>();
    public List<AvatarPose> GatherPoses()
    {
        _poses.Clear();
        foreach (var car in Cars)
        {
            _poses.Add(car.GetPose());
        }
        return _poses;
    }

    struct UpdateAICarPosesMsg : INetMessage
    {
        public int MessageId => (int)MsgId.S_UpdateAICarPoses;

        public List<AvatarPose> Poses;
        public void Sync<T>(T synchronizer) where T : ISynchronizer
        {
            synchronizer.SyncListSubmessage(ref Poses);
        }
    }
    public void UpdateHost()
    {
        _host.BroadcastUnreliable(new UpdateAICarPosesMsg
        {
            Poses = GatherPoses(),
        });
    }
}
