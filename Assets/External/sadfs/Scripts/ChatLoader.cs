using UnityEngine;

public class ChatLoader : MonoBehaviour
{
    [SerializeField] private ChatUIController ui;

    private void Start()
    {
        if (ui == null || ChatManager.Instance == null) return;

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
}
