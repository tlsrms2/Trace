using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject playerMessagePrefab;
    [SerializeField] private GameObject opponentMessagePrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private bool autoScrollToBottom = true;
    private readonly List<ChatMessageView> views = new List<ChatMessageView>();

    public void RenderAll(IReadOnlyList<ChatMessage> messages)
    {
        ClearAll();
        if (messages == null) return;

        for (int i = 0; i < messages.Count; i++)
        {
            AppendMessage(messages[i]);
        }

        if (autoScrollToBottom)
            ScrollToBottom();
    }

    public void AppendMessage(ChatMessage msg)
    {
        if (msg == null) return;
        if (messageContainer == null) return;

        msg.UnreadCount = msg.IsPlayer ? 1 : 0;

        var prefab = msg.IsPlayer ? playerMessagePrefab : opponentMessagePrefab;
        if (prefab == null) return;

        var go = Instantiate(prefab, messageContainer);
        var view = go.GetComponent<ChatMessageView>();
        if (view != null)
        {
            view.Bind(msg);
            views.Add(view);
        }

        if (!msg.IsPlayer)
            MarkPlayerMessagesRead();

        if (autoScrollToBottom)
            ScrollToBottom();
    }

    private void ClearAll()
    {
        if (messageContainer == null) return;

        for (int i = messageContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(messageContainer.GetChild(i).gameObject);
        }
        views.Clear();
    }

    private void MarkPlayerMessagesRead()
    {
        for (int i = 0; i < views.Count; i++)
        {
            var view = views[i];
            if (view == null) continue;

            var msg = view.BoundMessage;
            if (msg == null || !msg.IsPlayer) continue;

            msg.UnreadCount = 0;
            view.RefreshUnread();
        }
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;

        StopAllCoroutines();
        StartCoroutine(ScrollToBottomRoutine());
    }

    private System.Collections.IEnumerator ScrollToBottomRoutine()
    {
        // wait for layout to settle
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        scrollRect.verticalNormalizedPosition = 0f;

        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        scrollRect.verticalNormalizedPosition = 0f;

        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
