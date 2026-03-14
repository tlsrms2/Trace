using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    [Header("Notification")]
    [SerializeField] private GameObject notificationObject;
    [SerializeField] private float moveUpDistance = 120f;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private bool testShowOnStart = true;
    [SerializeField] private bool testOpenOnKey = false;
    [SerializeField] private KeyCode testOpenKey = KeyCode.F1;

    private RectTransform notificationRect;
    private Vector3 startLocalPos;
    private Coroutine notifyCor;

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
        }
    }

    private void Start()
    {
        if (testShowOnStart)
            ShowNotification();
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

    public void AddMessage(ChatMessage msg)
    {
        if (msg == null) return;

        messages.Add(msg);
        OnMessageAdded?.Invoke(msg);
        ShowNotification();
    }

    public void ShowNotification()
    {
        if (notificationObject == null) return;

        if (notifyCor != null) StopCoroutine(notifyCor);
        notifyCor = StartCoroutine(NotifyRoutine());
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

    public void OpenChatScene()
    {
        Debug.Log("OpenChatScene called");
        if (!IsChatSceneLoaded())
        {
            Debug.Log("Loading ChatScene");
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
