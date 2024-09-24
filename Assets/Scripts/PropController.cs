using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PropController : NetworkBehaviour
{
    public float hitPoints = 50f;

    [ClientRpc]
    public void RpcTakeDamage(float amount)
    {
        hitPoints -= amount;
        if (hitPoints <= 0)
        {
            RpcDestroyProp();
        }
    }

    [ClientRpc]
    private void RpcDestroyProp()
    {
        // Handle destruction effects (like animations, sound)
        Debug.Log("Prop destroyed!");
        // Destroy the object locally and on the server
        NetworkServer.Destroy(gameObject);
    }
}
