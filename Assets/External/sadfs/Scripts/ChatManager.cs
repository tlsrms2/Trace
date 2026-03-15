using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

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

    private RectTransform notificationRect;
    private Button notificationButton;
    private Vector3 startLocalPos;
    private Coroutine notifyCor;

    private readonly List<ChatMessage> messages = new List<ChatMessage>();

    public event Action<ChatMessage> OnMessageAdded;
    public event Action OnChatSceneOpened;
    public event Action OnChatSceneClosed;

    private const string ChatSceneName = "ChatScene";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

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
            CacheNotificationButton();
            BindNotificationButton();
        }
    }

    private void Start()
    {
        if (testShowOnStart)
            ShowNotification();
    }

    private void Update()
    {
        if (IsChatSceneLoaded())
        {
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Escape))
                CloseChatScene();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F1))
                OpenChatScene();
        }
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
            notificationObject.transform.localPosition = pos;
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;
        RebindNotification();
    }

    private void RebindNotification()
    {
        if (notificationObject == null)
            notificationObject = GameObject.Find("kraft");

        if (notificationObject == null) return;

        notificationRect = notificationObject.GetComponent<RectTransform>();
        startLocalPos = notificationRect != null
            ? (Vector3)notificationRect.anchoredPosition
            : notificationObject.transform.localPosition;

        notificationNameText = null;
        notificationBodyText = null;
        notificationButton = null;

        CacheNotificationTextTargets();
        CacheNotificationButton();
        BindNotificationButton();
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

    private void CacheNotificationButton()
    {
        if (notificationObject == null) return;
        if (notificationButton != null) return;

        notificationButton = notificationObject.GetComponent<Button>();
        if (notificationButton == null)
            notificationButton = notificationObject.GetComponentInChildren<Button>(true);
    }

    private void BindNotificationButton()
    {
        if (notificationButton == null) return;

        notificationButton.onClick.RemoveListener(OpenChatScene);
        notificationButton.onClick.AddListener(OpenChatScene);
    }

    public void OpenChatScene()
    {
        Debug.Log("OpenChatScene called");

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