using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // Add TextMeshPro namespace
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField playerNameInput; // Changed to TMP_InputField for TextMeshPro
    [SerializeField] private Button createRoomButton; // Button to create a room
    [SerializeField] private Button joinRoomButton; // Button to join a random room

    void Start()
    {
        // Connect to Photon
        PhotonNetwork.ConnectUsingSettings();
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRandomRoom);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        // Set player nickname from InputField, or use a default if empty
        string playerName = string.IsNullOrEmpty(playerNameInput.text) ? "Player" + Random.Range(1000, 9999) : playerNameInput.text;
        PhotonNetwork.NickName = playerName;
    }

    void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom("Room" + Random.Range(1000, 9999), roomOptions);
    }

    void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        // Load the game scene
        PhotonNetwork.LoadLevel("MainScene");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No random room available, creating one...");
        CreateRoom();
    }
}