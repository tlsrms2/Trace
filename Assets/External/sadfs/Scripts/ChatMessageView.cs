using TMPro;
using UnityEngine;
using System.Globalization;

public class ChatMessageView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI unreadCountText;

    private ChatMessage boundMessage;
    public ChatMessage BoundMessage => boundMessage;

    public void Bind(ChatMessage msg)
    {
        boundMessage = msg;
        if (nameText != null)
        {
            nameText.text = msg.IsPlayer ? "" : msg.SenderName;
            nameText.gameObject.SetActive(!msg.IsPlayer);
        }
        if (bodyText != null) bodyText.text = msg.Text;
        if (timeText != null)
        {
            var dt = System.DateTimeOffset.FromUnixTimeSeconds(msg.TimestampUnix).ToLocalTime();
            timeText.text = dt.ToString("tt hh:mm", new CultureInfo("ko-KR"));
        }
        ApplyUnreadCount(msg.UnreadCount);
    }

    public void RefreshUnread()
    {
        if (boundMessage == null) return;
        ApplyUnreadCount(boundMessage.UnreadCount);
    }

    private void ApplyUnreadCount(int count)
    {
        if (unreadCountText == null) return;

        if (count <= 0)
        {
            unreadCountText.text = "";
            unreadCountText.gameObject.SetActive(false);
            return;
        }

        unreadCountText.gameObject.SetActive(true);
        unreadCountText.text = count.ToString();
    }
}
