﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelekinesisAbility :  Ability
{
    private bool isInteractable = false;
    [SerializeField] private LayerMask mask;
    private GameObject objectToMove;
    private float angleZ;
    private Vector3 middlePosition;
    private Vector3 playerPosition;
    private bool isPressed;
    [SerializeField] private GameObject cam;
    [SerializeField] private float distance;
    private PlayerNetwork playerNetwork;
    private PhotonView view;
    private bool alreadyParent = false;

    private void Start()
    {
        playerNetwork = GetComponent<PlayerNetwork>();
        view = GetComponent<PhotonView>();
    }

    void FixedUpdate()
    {
        
        if (isInteractable && isPressed)
        {
            
            PerformMovement();
        }
    }

    private void PerformMovement()
    {
        float positionY = objectToMove.transform.position.y;
        float playerPosY = transform.position.y - 1;
        Debug.Log(playerPosY);
        //changer les valeurs magiques par des valeurs dynamiques; 1 = hauteur du cube a la base; 4 = une valeur max au dessus du joueur
        if (positionY <= playerPosY + 3.8f && positionY >= 1f)
        {
            objectToMove.transform.RotateAround(playerPosition, -cam.transform.right, angleZ);
        } else if (positionY >= playerPosY + 3.7f && angleZ < 0f)
        {
            objectToMove.transform.RotateAround(playerPosition, -cam.transform.right, angleZ);
        } else if (positionY <= playerPosY + 1.1f && angleZ > 0)
        {
            objectToMove.transform.RotateAround(playerPosition, -cam.transform.right, angleZ);
        }
    }

    public override void Interact()
    {
        //if (objectToMove != null) return;
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward),
            out hit, distance, mask))
        {
            if (hit.collider.CompareTag("InteractablePhysicsObject"))
            {
                view = GetComponent<PhotonView>();
                view.RPC("SetObjectToMove",PhotonTargets.All, hit.collider.gameObject.name);
                playerNetwork.ChangeOwner(hit.collider);
                Physics.IgnoreCollision(objectToMove.gameObject.GetComponent<Collider>(), transform.gameObject.GetComponent<Collider>());
                if (isPressed)
                {
                    view.RPC("ParentObject", PhotonTargets.All);
                }
                objectToMove.GetComponent<InteractableObject>().StartFlashing();
            }
        }
        else
        {
            if (objectToMove != null)
            {
                Release();
            }
        }
    }

    [PunRPC]
    public void ParentObject()
    {
        isInteractable = true;
        if (alreadyParent) return;
        objectToMove.transform.parent = transform;
        objectToMove.transform.position = cam.transform.position + cam.transform.forward * (3 + Vector3.Distance(transform.position, cam.transform.position)) ;
        alreadyParent = true;
    }
    
    [PunRPC]
    public void DeparentObject()
    {
        objectToMove.transform.parent = null;
        isInteractable = false;
        alreadyParent = false;
    }

    [PunRPC]
    public void SetObjectToMove(String str)
    {
        objectToMove = GameObject.Find(str);
    }
    
    [PunRPC]
    public void RemoveObjectToMove()
    {
        objectToMove = null;
    }

    public override void SetValue(float _angleZ, Vector3 _playerPosition)
    {
        if (objectToMove is null) return;

        angleZ = _angleZ;
        playerPosition = _playerPosition;
    }
    
    public override void Pressed()
    {
        isPressed = true;
        if (objectToMove == null)
        {
            return;
        }
        objectToMove.GetComponentInChildren<Rigidbody>().isKinematic = true;
    }

    public override void Release()
    {
        isPressed = false;
        if (objectToMove == null)
        {
            return;
        }
        Physics.IgnoreCollision(objectToMove.gameObject.GetComponent<Collider>(), transform.gameObject.GetComponent<Collider>(), false);
        objectToMove.GetComponentInChildren<Rigidbody>().isKinematic = false;
        objectToMove.gameObject.GetComponent<InteractableObject>().StopFlashing();
        view.RPC("DeparentObject", PhotonTargets.All);
        view.RPC("RemoveObjectToMove",PhotonTargets.All);
    }
}
