using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("Input Field")]
    public TMP_InputField inputField;

    [Header("Output")]
    public string currentText;

    void Start()
    {
        if (inputField == null) return;

        inputField.onSubmit.AddListener(OnSubmit);
        StartCoroutine(FocusInputFieldNextFrame());
    }

    private IEnumerator FocusInputFieldNextFrame()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
    }

    private void OnSubmit(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        currentText = text;
        Debug.Log($"플레이어 입력: {currentText}");

        if (ChatManager.Instance != null)
        {
            var msg = new ChatMessage(
                senderId: "player",
                senderName: "Player",
                text: currentText,
                isPlayer: true,
                timestampUnix: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
            ChatManager.Instance.AddMessage(msg);
        }

        inputField.text = "";
        StartCoroutine(FocusInputFieldNextFrame());
    }
}