using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectManager : MonoBehaviourPunCallbacks
{
  [SerializeField] private Dropdown GroupDropdown;
  [SerializeField] private Dropdown KindDropdown;

  private List<string> roomName = new List<string>
  {
    "305hr_1", "305hr_2", "306hr_1", "306hr_2"
  };

  public void OnPressedSubmit()
  {
    if (GroupDropdown.value == 0 || KindDropdown.value == 0) return;

    if (!PhotonNetwork.IsConnected)
    {
      PhotonNetwork.NickName = "User" + KindDropdown.value.ToString();
      PhotonNetwork.ConnectUsingSettings();
    }
    else
    {
      PhotonNetwork.JoinOrCreateRoom(roomName[GroupDropdown.value - 1], new RoomOptions { MaxPlayers = 3 }, TypedLobby.Default);
    }
  }

  public override void OnConnectedToMaster()
  {
    base.OnConnectedToMaster();

    PhotonNetwork.JoinLobby();
  }

  public override void OnJoinedLobby()
  {
    base.OnJoinedLobby();

    if (GroupDropdown.value == 0) return;

    PhotonNetwork.JoinOrCreateRoom(roomName[GroupDropdown.value - 1], new RoomOptions { MaxPlayers = 7 }, TypedLobby.Default);
  }

  public override void OnJoinedRoom()
  {
    base.OnJoinedRoom();

    SceneManager.LoadScene("MainScene");
  }
}
