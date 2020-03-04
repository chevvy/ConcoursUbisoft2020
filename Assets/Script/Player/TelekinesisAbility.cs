﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelekinesisAbility :  Ability
{
   private static float MAX_HEIGHT = 3.8F;
   private static float TOLERENCE = 0.1F;
    
    [SerializeField] private GameObject cam;
    [SerializeField] private float distance;
    [SerializeField] private LayerMask mask;
    private bool isInteractable = false;
    private bool isPressed;
    private bool alreadyParent = false;
    private float angleZ;
    private float sensitivity = 3f;
    private static bool canRotate = false;
    private GameObject objectToMove;
    private Vector3 playerPosition;
    private Collider playerCollider;
    private PlayerNetwork playerNetwork;
    private PhotonView view;

    private void Start()
    {
        playerNetwork = GetComponent<PlayerNetwork>();
        view = GetComponent<PhotonView>();
        playerCollider = transform.gameObject.GetComponent<Collider>();
    }

    void FixedUpdate()
    {
        
        if (isInteractable && isPressed)
        {
            float rotationY = 0;
            float rotationX = 0;
            if (Input.GetAxis("TelekinesisRotate") != 0 && canRotate)
            {
                rotationY = -Input.GetAxis("Mouse X") * sensitivity;
                rotationX = -Input.GetAxis("Mouse Y") * sensitivity;
            }
            else
            {
                PerformRotationAroundPlayer();
            }
            PerformRotationAroundItself(rotationX, rotationY);
        }
    }

    private void PerformRotationAroundItself(float rotationX, float rotationY)
    {
        Transform objectToMoveTransform = objectToMove.transform;
        objectToMoveTransform.RotateAround(objectToMoveTransform.position, cam.transform.right, rotationX);
        objectToMoveTransform.RotateAround(objectToMoveTransform.position, cam.transform.up, rotationY);
    }

    private void PerformRotationAroundPlayer()
    {
        float objectToMovePosY = objectToMove.transform.position.y;
        
        // 1 = hauteur du personnage, a changer quand le personnage va etre le bon (pas une capsule)L
        float playerPosY = transform.position.y - 1;
        float objectToMoveScaleY = objectToMove.transform.lossyScale.y / 2;
        
        if (objectToMovePosY <= playerPosY + MAX_HEIGHT && objectToMovePosY >=  playerPosY + objectToMoveScaleY)
        {
            objectToMove.transform.RotateAround(playerPosition, -cam.transform.right, angleZ);
        } else if (objectToMovePosY >= playerPosY + MAX_HEIGHT - TOLERENCE && angleZ < 0f)
        {
            objectToMove.transform.RotateAround(playerPosition, -cam.transform.right, angleZ);
        } else if (objectToMovePosY <= playerPosY + objectToMoveScaleY + TOLERENCE && angleZ > 0)
        {
            objectToMove.transform.RotateAround(playerPosition, -cam.transform.right, angleZ);
        }
    }

    public override void Interact()
    {
        if (objectToMove != null) return;
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward),
            out hit, distance, mask))
        {
            if (hit.collider.CompareTag("InteractablePhysicsObject"))
            {
                //view = GetComponent<PhotonView>();
                view.RPC("SetObjectToMove",PhotonTargets.All, hit.collider.gameObject.name);
                playerNetwork.ChangeOwner(hit.collider);
                
                Physics.IgnoreCollision(objectToMove.gameObject.GetComponent<Collider>(), playerCollider);
                
                if (isPressed)
                {
                    view.RPC("ParentObject", PhotonTargets.All);
                }
                
                objectToMove.GetComponent<InteractableObject>().StartFlashing();
            }
        }
    }

    [PunRPC]
    public void ParentObject()
    {
        isInteractable = true;
        if (alreadyParent) return;
        
        objectToMove.transform.parent = transform;
        objectToMove.transform.position = cam.transform.position + cam.transform.forward * (3 + Vector3.Distance(transform.position, cam.transform.position));
        
        if ( objectToMove.transform.position.y < 0)
        {
            objectToMove.transform.position = new Vector3(objectToMove.transform.position.x, 1 , objectToMove.transform.position.z );
        }
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

    public override void IncreaseAbility()
    {
        canRotate = true;
    }
}