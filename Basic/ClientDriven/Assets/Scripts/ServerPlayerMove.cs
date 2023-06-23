using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Server side script to do some movements that can only be done server side with Netcode. In charge of spawning (which happens server side in Netcode)
/// and picking up objects
/// </summary>
[DefaultExecutionOrder(0)] // before client component
public class ServerPlayerMove : NetworkBehaviour
{
    public NetworkVariable<bool> isObjectPickedUp = new NetworkVariable<bool>();
    public NetworkVariable<ulong> pickedUpObjectID;

    NetworkObject m_PickedUpObject;
    NetworkObject PickedUpObject
    {
        get => m_PickedUpObject;
        set
        {
            m_PickedUpObject = value;
            if (m_PickedUpObject == null)
            {
                isObjectPickedUp.Value = false;
                pickedUpObjectID.Value = 999999;
                return;
            }
            isObjectPickedUp.Value = true;
            pickedUpObjectID.Value = m_PickedUpObject.NetworkObjectId;
        }
    }

    [SerializeField]
    Vector3 m_LocalHeldPosition;

    // DOC START HERE
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        OnServerSpawnPlayer();
        
        base.OnNetworkSpawn();
    }

    void OnServerSpawnPlayer()
    {
        // this is done server side, so we have a single source of truth for our spawn point list
        var spawnPoint = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
        var spawnPosition = spawnPoint ? spawnPoint.transform.position : Vector3.zero;
        transform.position = spawnPosition;
        
        // A note specific to owner authority:
        // Side Note:  Specific to Owner Authoritative
        // Setting the position works as and can be set in OnNetworkSpawn server-side unless there is a
        // CharacterController that is enabled by default on the authoritative side. With CharacterController, it
        // needs to be disabled by default (i.e. in Awake), the server applies the position (OnNetworkSpawn), and then
        // the owner of the NetworkObject should enable CharacterController during OnNetworkSpawn. Otherwise,
        // CharacterController will initialize itself with the initial position (before synchronization) and updates the
        // transform after synchronization with the initial position, thus overwriting the synchronized position.
    }

    [ServerRpc]
    public void PickupObjectServerRpc(ulong objToPickupID)
    {        
        Debug.Log("Execute on Server" +IsServer);
        
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup);
        if (objectToPickup == null || objectToPickup.transform.parent != null) return; // object already picked up, server authority says no

        if (objectToPickup.TryGetComponent(out NetworkObject networkObject) && networkObject.TrySetParent(transform))
        {
            var pickUpObjectRigidbody = objectToPickup.GetComponent<Rigidbody>();
            pickUpObjectRigidbody.isKinematic = true;
            pickUpObjectRigidbody.interpolation = RigidbodyInterpolation.None;
            objectToPickup.GetComponent<NetworkTransform>().InLocalSpace = true;
            objectToPickup.transform.localPosition = m_LocalHeldPosition;
            objectToPickup.GetComponent<ServerIngredient>().ingredientDespawned += IngredientDespawned;
            PickedUpObject = objectToPickup;
        }
    }

    
    [ServerRpc]
    public void PassObjectServerRpc(ulong objToPickupID, ulong closestPlayerID)
    {        
        Debug.Log("Execute on Server" +IsServer);
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPass);
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(closestPlayerID, out var closestPlayer);
        
        
        if (objectToPass.TryGetComponent(out NetworkObject networkObject) 
            && closestPlayer.TryGetComponent(out Transform ClosestplayerTransform)
            && networkObject.TrySetParent(ClosestplayerTransform))
        {
            var pickUpObjectRigidbody = objectToPass.GetComponent<Rigidbody>();
            pickUpObjectRigidbody.isKinematic = true;
            pickUpObjectRigidbody.interpolation = RigidbodyInterpolation.None;
            objectToPass.GetComponent<NetworkTransform>().InLocalSpace = true;
            objectToPass.transform.localPosition = m_LocalHeldPosition;
            //objectToPickup.GetComponent<ServerIngredient>().ingredientDespawned += IngredientDespawned;
            PickedUpObject = null;

            closestPlayer.GetComponent<ServerPlayerMove>().isObjectPickedUp.Value = true;
            closestPlayer.GetComponent<ServerPlayerMove>().PickedUpObject = objectToPass; 
            closestPlayer.GetComponent<ServerPlayerMove>().pickedUpObjectID.Value = objectToPass.NetworkObjectId; 
            //
        }
    }
    void IngredientDespawned()
    {
        PickedUpObject = null;
        isObjectPickedUp.Value = false;
    }
    
    [ServerRpc]
    public void DropObjectServerRpc()
    {
        Debug.Log("Execute on Server" +IsServer);
        if (PickedUpObject != null)
        {
            // can be null if enter drop zone while carrying
            PickedUpObject.transform.parent = null;
            var pickedUpObjectRigidbody = PickedUpObject.GetComponent<Rigidbody>();
            pickedUpObjectRigidbody.isKinematic = false;
            pickedUpObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            PickedUpObject.GetComponent<NetworkTransform>().InLocalSpace = false;
            PickedUpObject = null;
        }
    }
    // DOC END HERE

}
