using System;
using System.Collections.Generic;
using UnityEngine;

public class ChatLoader : MonoBehaviour
{
    [SerializeField] private ChatUIController ui;
    [SerializeField] private TextAsset chatJson;
    [SerializeField] private bool loadFromJsonOnStart = true;

    private void Start()
    {
        if (ui == null || ChatManager.Instance == null) return;

        if (loadFromJsonOnStart && chatJson != null && ChatManager.Instance.GetMessages().Count == 0)
        {
            var loaded = ParseMessages(chatJson.text);
            if (loaded != null)
                ChatManager.Instance.SetMessages(loaded);
        }

        ui.RenderAll(ChatManager.Instance.GetMessages());
        ChatManager.Instance.OnMessageAdded += ui.AppendMessage;
    }

    private void OnDestroy()
    {
        if (ChatManager.Instance != null && ui != null)
            ChatManager.Instance.OnMessageAdded -= ui.AppendMessage;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && ChatManager.Instance != null)
        {
            ChatManager.Instance.CloseChatScene();
        }
    }

    private static List<ChatMessage> ParseMessages(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var wrapper = JsonUtility.FromJson<ChatMessageList>(json);
            return wrapper != null ? wrapper.Messages : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [Serializable]
    private class ChatMessageList
    {
        public List<ChatMessage> Messages;
    }
}
