using System;

[Serializable]
public class ChatMessage
{
    public string SenderId;
    public string SenderName;
    public string Text;
    public long TimestampUnix;
    public bool IsPlayer;
    public int UnreadCount = -1;

    public ChatMessage(string senderId, string senderName, string text, bool isPlayer, long timestampUnix)
    {
        SenderId = senderId;
        SenderName = senderName;
        Text = text;
        IsPlayer = isPlayer;
        TimestampUnix = timestampUnix;
        UnreadCount = -1;
    }
}
