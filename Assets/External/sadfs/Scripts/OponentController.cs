using UnityEngine;
using System;

public class OponentController : MonoBehaviour
{
    public void ReceiveOpponentMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (ChatManager.Instance != null)
        {
            var msg = new ChatMessage(
                senderId: "opponent",
                senderName: "Opponent",
                text: text,
                isPlayer: false,
                timestampUnix: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
            ChatManager.Instance.AddMessage(msg);
        }
    }
}