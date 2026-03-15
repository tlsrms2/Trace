using System.Collections.Generic;
using UnityEngine;

public class ChatUIController : MonoBehaviour
{
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject playerMessagePrefab;
    [SerializeField] private GameObject opponentMessagePrefab;
    [SerializeField] private int participantCount = 2;

    public void RenderAll(IReadOnlyList<ChatMessage> messages)
    {
        ClearAll();
        if (messages == null) return;

        for (int i = 0; i < messages.Count; i++)
        {
            AppendMessage(messages[i]);
        }
    }

    public void AppendMessage(ChatMessage msg)
    {
        if (msg == null) return;
        if (messageContainer == null) return;

        if (msg.UnreadCount < 0)
            msg.UnreadCount = Mathf.Max(0, participantCount - 1);

        var prefab = msg.IsPlayer ? playerMessagePrefab : opponentMessagePrefab;
        if (prefab == null) return;

        var go = Instantiate(prefab, messageContainer);
        var view = go.GetComponent<ChatMessageView>();
        if (view != null)
            view.Bind(msg);
    }

    private void ClearAll()
    {
        if (messageContainer == null) return;

        for (int i = messageContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(messageContainer.GetChild(i).gameObject);
        }
    }
}
