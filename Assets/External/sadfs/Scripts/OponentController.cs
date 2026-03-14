using UnityEngine;
using System;

public class OponentController : MonoBehaviour
{
    [Header("Test Message")]
    [TextArea] public string opponentMessage = "æ»≥Á«œººø‰";
    public bool sendOnStart = false;

    void Start()
    {
        if (sendOnStart)
            ReceiveOpponentMessage(opponentMessage);
    }

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
