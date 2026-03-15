using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    [Header("Notification")]
    [SerializeField] private GameObject notificationObject;
    [SerializeField] private TextMeshProUGUI notificationNameText;
    [SerializeField] private TextMeshProUGUI notificationBodyText;
    [SerializeField] private float moveUpDistance = 120f;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private bool testShowOnStart = true;
    [SerializeField] private bool testOpenOnKey = false;
    [SerializeField] private KeyCode testOpenKey = KeyCode.F1;

    [Header("Auto Opponent Message")]
    [SerializeField] private bool autoOpponentMessageEnabled = true;
    [SerializeField] private float autoOpponentMessageInterval = 10f;
    [SerializeField] private string autoOpponentSenderId = "npc_auto";
    [SerializeField] private string autoOpponentSenderName = "Operator";
    [SerializeField] private string autoOpponentText = "Hi";

    private RectTransform notificationRect;
    private Vector3 startLocalPos;
    private Coroutine notifyCor;
    private Coroutine autoOpponentCor;

    private readonly List<ChatMessage> messages = new List<ChatMessage>();

    public event Action<ChatMessage> OnMessageAdded;
    public event Action OnChatSceneOpened;
    public event Action OnChatSceneClosed;

    private const string ChatSceneName = "ChatScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (notificationObject != null)
        {
            notificationRect = notificationObject.GetComponent<RectTransform>();
            startLocalPos = notificationRect != null
                ? (Vector3)notificationRect.anchoredPosition
                : notificationObject.transform.localPosition;

            CacheNotificationTextTargets();
        }
    }

    private void Start()
    {
        if (testShowOnStart)
            ShowNotification();

        if (autoOpponentMessageEnabled)
            autoOpponentCor = StartCoroutine(AutoOpponentMessageLoop());
    }

    private void Update()
    {
        if (testOpenOnKey && Input.GetKeyDown(testOpenKey))
            OpenChatScene();
    }

    public IReadOnlyList<ChatMessage> GetMessages()
    {
        return messages;
    }

    public void SetMessages(IReadOnlyList<ChatMessage> newMessages)
    {
        messages.Clear();

        if (newMessages == null) return;

        for (int i = 0; i < newMessages.Count; i++)
            messages.Add(newMessages[i]);
    }

    public void AddMessage(ChatMessage msg)
    {
        if (msg == null) return;

        messages.Add(msg);
        OnMessageAdded?.Invoke(msg);

        // 채팅창이 열려있지 않을 때만 알림
        if (!msg.IsPlayer && !IsChatSceneLoaded())
            ShowNotification();
    }

    public void ShowNotification()
    {
        if (notificationObject == null) return;

        notificationObject.SetActive(true);

        UpdateNotificationText(GetLastMessage());

        if (notifyCor != null)
            StopCoroutine(notifyCor);

        notifyCor = StartCoroutine(NotifyRoutine());
    }

    public void HideNotification()
    {
        if (notificationObject == null) return;

        if (notifyCor != null)
        {
            StopCoroutine(notifyCor);
            notifyCor = null;
        }

        SetLocalPos(startLocalPos);
        notificationObject.SetActive(false);
    }

    private IEnumerator NotifyRoutine()
    {
        Vector3 endLocalPos = startLocalPos + Vector3.up * moveUpDistance;

        yield return MoveLocal(startLocalPos, endLocalPos, moveDuration);

        yield return new WaitForSeconds(holdDuration);

        yield return MoveLocal(endLocalPos, startLocalPos, moveDuration);

        notifyCor = null;
    }

    private IEnumerator AutoOpponentMessageLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.1f, autoOpponentMessageInterval));

        while (true)
        {
            yield return wait;

            AddMessage(new ChatMessage(
                senderId: autoOpponentSenderId,
                senderName: autoOpponentSenderName,
                text: autoOpponentText,
                isPlayer: false,
                timestampUnix: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            ));
        }
    }

    private IEnumerator MoveLocal(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            SetLocalPos(to);
            yield break;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            SetLocalPos(Vector3.Lerp(from, to, Mathf.Clamp01(t)));

            yield return null;
        }
    }

    private void SetLocalPos(Vector3 pos)
    {
        if (notificationRect != null)
            notificationRect.anchoredPosition = pos;
        else if (notificationObject != null)
            notificationObject.transform.localPosition = pos; // 오케이
    }

    private ChatMessage GetLastMessage()
    {
        if (messages.Count == 0) return null;

        return messages[messages.Count - 1];
    }

    private void UpdateNotificationText(ChatMessage msg)
    {
        if (msg == null) return;

        if (notificationNameText != null)
            notificationNameText.text = msg.SenderName ?? "";

        if (notificationBodyText != null)
            notificationBodyText.text = msg.Text ?? "";
    }

    private void CacheNotificationTextTargets()
    {
        if (notificationObject == null) return;

        if (notificationNameText != null && notificationBodyText != null)
            return;

        var texts = notificationObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];

            if (notificationNameText == null && t.gameObject.name == "Name")
                notificationNameText = t;
            else if (notificationBodyText == null && t.gameObject.name == "Message")
                notificationBodyText = t;
        }
    }

    public void OpenChatScene()
    {
        Debug.Log("OpenChatScene called");

        // 채팅방 열릴 때 알림 제거
        HideNotification();

        if (!IsChatSceneLoaded())
        {
            if (GameManager.Instance != null)
                GameManager.Instance.PauseGame();

            SceneManager.LoadScene(ChatSceneName, LoadSceneMode.Additive);

            OnChatSceneOpened?.Invoke();
        }
    }

    public void CloseChatScene()
    {
        if (IsChatSceneLoaded())
        {
            SceneManager.UnloadSceneAsync(ChatSceneName);

            OnChatSceneClosed?.Invoke();
        }
    }

    public bool IsChatSceneLoaded()
    {
        return SceneManager.GetSceneByName(ChatSceneName).isLoaded;
    }
}