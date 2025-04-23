using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoomListItemUI : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private string roomId;

    // 用于将房间加入操作的回调传递到 RoomListItemUI
    public System.Action<string> OnJoinRoomButtonClicked;

    public void Setup(RoomInfoNetwork room)
    {
        roomId = room.roomId.ToString();
        roomNameText.text = room.roomName.ToString();
        playerCountText.text = $"{room.currentPlayers}/{room.maxPlayers}";

        // Debug.Log($"Button listeners 添加前: {joinButton.onClick.GetPersistentEventCount()}");

        joinButton.onClick.RemoveAllListeners();
        // 添加加入房间按钮的回调
        joinButton.onClick.AddListener(() => OnJoinRoomButtonClicked?.Invoke(roomId));

        // Debug.Log($"Button listeners 添加后: {joinButton.onClick.GetPersistentEventCount()}");

        if (OnJoinRoomButtonClicked != null)
        {
            // Debug.Log($"OnJoinRoomButtonClicked callback已设置，房间号为{roomId}");
        }
    }

    // private void OnJoinClicked()
    // {
    //     RoomManager.Instance.JoinRoomServerRpc(roomId, "");
    //     SceneManager.LoadScene("GameRoomScene", LoadSceneMode.Additive);
    // }
}
