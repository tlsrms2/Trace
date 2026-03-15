using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAI;
using UnityEngine;

public class ChatGPTManager : MonoBehaviour
{
    private OpenAIApi openAI;
    private List<OpenAI.ChatMessage> messages = new List<OpenAI.ChatMessage>();
    [SerializeField] private string apiKey;

    public async void ASKChatGPT(string newText)
    {
        if (openAI == null)
        {
            Debug.LogError("OpenAI client not initialized. Set apiKey in Inspector.");
            return;
        }

        OpenAI.ChatMessage newMessage = new OpenAI.ChatMessage();
        newMessage.Content = newText;
        newMessage.Role = "user";

        messages.Add(newMessage);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo";

        var response = await openAI.CreateChatCompletion(request);

        if (response.Choices != null && response.Choices.Count > 0)
        {
            var chatResponse = response.Choices[0].Message;
            messages.Add(chatResponse);
            Debug.Log($"ChatGPT Response: {chatResponse.Content}");
            
        }

    }

    void Start()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("apiKey is empty. Set it in Inspector.");
            return;
        }

        openAI = new OpenAIApi(apiKey);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
