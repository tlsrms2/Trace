using TMPro;
using UnityEngine;
using System.Globalization;
using System.Collections;

public class ChatMessageView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI unreadCountText;
    [SerializeField] private float unreadTickDelay = 0.25f;

    private Coroutine unreadCor;
    private ChatMessage boundMessage;

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

        if (unreadCor != null)
        {
            StopCoroutine(unreadCor);
            unreadCor = null;
        }
        ApplyUnreadCount(msg.UnreadCount);
        if (msg.UnreadCount > 0)
            unreadCor = StartCoroutine(DecreaseUnreadCount());
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

    private IEnumerator DecreaseUnreadCount()
    {
        while (boundMessage != null && boundMessage.UnreadCount > 0)
        {
            yield return new WaitForSeconds(unreadTickDelay);
            boundMessage.UnreadCount = Mathf.Max(0, boundMessage.UnreadCount - 1);
            ApplyUnreadCount(boundMessage.UnreadCount);
        }
        unreadCor = null;
    }
}
