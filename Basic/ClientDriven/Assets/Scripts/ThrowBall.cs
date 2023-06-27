using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ThrowBall : NetworkBehaviour
{

    public NetworkVariable<bool> isThrown = new NetworkVariable<bool>(false);
    void OnCollisionEnter(Collision collision)
    {
        if(!IsServer)
            return;
        
        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Hit Player"  );
            var ServerPlayerNetworkObj = collision.gameObject.GetComponent<ServerPlayerMove>();
            ServerPlayerNetworkObj.isObjectPickedUp.Value = true;
            ServerPlayerNetworkObj.pickedUpObjectID.Value = this.NetworkObjectId;
            this.TryGetComponent(out NetworkObject networkObject);
            networkObject.TrySetParent(collision.gameObject.transform);
            
            //ServerPlayerNetworkObj.PickupObjectServerRpc( this.NetworkObjectId); 
            
            
            var pickUpObjectRigidbody = GetComponent<Rigidbody>();
            pickUpObjectRigidbody.isKinematic = true;
            pickUpObjectRigidbody.interpolation = RigidbodyInterpolation.None;
            this.GetComponent<NetworkTransform>().InLocalSpace = true;
            this.transform.localPosition = new Vector3(0, 2.5f, 0);
            
        }
    }
}
