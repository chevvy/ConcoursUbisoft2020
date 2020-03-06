﻿using ExitGames.Demos.DemoAnimator;
using UnityEngine;

namespace Script
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPointP1;
        [SerializeField] private Transform spawnPointP2;
        [SerializeField] private Transform spawnPointNotconnected;

        [SerializeField] private BigTree tree;
        [SerializeField] private PhotonView[] doorViews;
        private int nbOfPuzzleSuceeed = 0;
        private void Awake()
        {
            if (PhotonNetwork.connected)
            {
                if (PlayerManager.LocalPlayerInstance.CompareTag("Player1"))
                {
                    PhotonNetwork.Instantiate(PlayerManager.LocalPlayerInstance.name, spawnPointP1.position,
                        Quaternion.identity, 0);
                }
                else if (PlayerManager.LocalPlayerInstance.CompareTag("Player2"))
                {
                    PhotonNetwork.Instantiate(PlayerManager.LocalPlayerInstance.name, spawnPointP2.position,
                        Quaternion.identity, 0);
                }
            }
            else
            {
                Instantiate(playerPrefab, spawnPointNotconnected.position, Quaternion.identity);
            }
        }

        public virtual void OnJoinedLobby()
        {
            Debug.Log("Join lobby");
            RoomOptions room = new RoomOptions();
            room.maxPlayers = 2;
            PhotonNetwork.JoinOrCreateRoom("test", room, TypedLobby.Default);
        }

        [PunRPC]
        public void EndedPuzzle()
        {
            tree.Grow();
            PlayerManager.LocalPlayerInstance.GetComponent<PlayerNetwork>().EndedPuzzle();
            OpenNextDoor();
        }

        private void OpenNextDoor()
        {
            doorViews[nbOfPuzzleSuceeed].RPC("OpenDoorRPC", PhotonTargets.All);
            nbOfPuzzleSuceeed += 1;
        }
    }
}
