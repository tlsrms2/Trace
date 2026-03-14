using TMPro;
using UnityEngine;
using System.Globalization;

public class ChatMessageView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI timeText;

    public void Bind(ChatMessage msg)
    {
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
    }
}
