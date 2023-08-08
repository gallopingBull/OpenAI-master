using AiKodexDeepVoice;
using Newtonsoft.Json;
using OpenAI;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpenAIController : MonoBehaviour
{

    [Header("OpenAI Settings")]
    public double temperature = 1;
    public int maxTokens = 250;
    private Model model = Model.ChatGPTTurbo;

    public TMP_Dropdown botPersonalityDropdown;
    [SerializeField] string systemPrompt;
    public TMP_InputField sytetemPromptTextField;

    private List<string> personalityOptions = new List<string>() { "chatgpt_dan", "chatgpt_mongo_tom" };

    private string promptPath = "Assets/Prompts/BotPromptData.json";
    private OpenAIAPI api;
    private List<OpenAI_API.Chat.ChatMessage> messages;

    [Header("UI Elements")]
    public TMP_Text textField;
    private string currentConversation;
    public TMP_InputField inputField;
    public Button sendButton;
    public GameObject scrollViewObject;

    public MessageController messageController;


    // Logger sends chatlog to google form. 
    private SendChatLog logger;

    //public CanvasController canvasController;

    void Start()
    {
        // This line gets your API key (and could be slightly different on Mac/Linux)
#if UNITY_EDITOR
        api = new OpenAIAPI(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));
#else
        // TODO: Change this to an api key that is stored in a secure location on your server or something. 

        
#endif
        Init();
        StartConversation();

    }

    private void Init()
    {

        logger = GetComponent<SendChatLog>();
        systemPrompt = GetPrompt("chatgpt_dan", promptPath);
        sytetemPromptTextField.text = FormatSystemPrompt(systemPrompt);

        GetComponent<Whisper>().OnEndRecording += GetResponse;

        sendButton.onClick.AddListener(() => GetResponse());
        botPersonalityDropdown.onValueChanged.AddListener(delegate
      {
          DropdownValueChanged(botPersonalityDropdown);
      });
    }
    public void ReInitialize(string systemPrompt)
    {

        //personalityPrompt = GetPrompt(systemPrompt, promptPath);
        messages = new List<OpenAI_API.Chat.ChatMessage> {
              new OpenAI_API.Chat.ChatMessage(ChatMessageRole.System, this.systemPrompt)
        };


    }
    private void StartConversation()
    {
        messages = new List<OpenAI_API.Chat.ChatMessage> {
              new OpenAI_API.Chat.ChatMessage(ChatMessageRole.System, systemPrompt)
        };

        inputField.text = "";
        string startString = "I am galloping_bull's AI bot. Ask me something...\n";
        textField.text = startString;
        //messageController.AddMessage(startString);

        Debug.Log(startString);
    }

    private async void GetResponse()
    {
        if (inputField.text.Length < 1)
        {
            return;
        }

        // Disable the Send button
        sendButton.interactable = false;

        // Fill the user message from the input field
        OpenAI_API.Chat.ChatMessage userMessage = new OpenAI_API.Chat.ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputField.text;
        if (userMessage.Content.Length > 100)
        {
            // Limit messages to 100 characters
            userMessage.Content = userMessage.Content.Substring(0, 100);
        }
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);
        //messageController.AddMessage(userMessage.Content);

        // update conversation string.
        currentConversation = textField.text;
        if (currentConversation != "")
            currentConversation += "\n";
        currentConversation = AppendToBuilder(currentConversation, string.Format("You: {0}\n", userMessage.Content));

        // Update the text field with the user message
        textField.text = currentConversation;
        // move scroll view to bottom.
        ResetScrollRectVerticalPosition();

        // Clear the input field
        inputField.text = "";

        // Send the entire chat to OpenAI to get the next message
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = model,
            Temperature = 1, // this controls the behavior/correctness of the bot 
            MaxTokens = 250,
            Messages = messages
        });

        // Get the response message
        OpenAI_API.Chat.ChatMessage responseMessage = new OpenAI_API.Chat.ChatMessage();
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        responseMessage.Content = chatResult.Choices[0].Message.Content;
        Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

        // Add the response to the list of messages
        messages.Add(responseMessage);
        var tmp = RemoveBefore(responseMessage.Content);

        //messageController.AddMessage(tmp);
#if UNITY_EDITOR
        SaveToFile($"You: {userMessage.Content}", $"{responseMessage.rawRole}: {tmp}");
#endif
        logger.Send($"You: {userMessage.Content}\n{responseMessage.rawRole}: {tmp}");
        // generate voice response
        //canvasController.BotResponse = tmp;
        //canvasController.Generate();
        //currentConversation = AppendToBuilder(currentConversation, $"\n{responseMessage.rawRole}: {tmp}\n");
        // Update the text field with the response

        textField.text = currentConversation;

        ResetScrollRectVerticalPosition();

        // Re-enable the Send button.
        sendButton.interactable = true;
    }

    private void ResetScrollRectVerticalPosition()
    {
        Canvas.ForceUpdateCanvases();
        scrollViewObject.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
    }

    // method that takes a string and returns a string that removes all characters before "DAN".
    public static string RemoveBefore(string s)
    {
        int index = s.IndexOf("DAN");
        return index < 0 ? s : s.Substring(index);
    }

    // method that takes in two strings and appends them to a sting builder.
    public static string AppendToBuilder(string curConversation, string newText)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(curConversation);
        sb.Append(newText);
        return sb.ToString();
    }

    // Generate method that takes in a string and saves it to a file in this project's root directory called GPTLOG-currentdate.txt
    public static void SaveToFile(string prompt, string reponse)
    {
        string time = GetDate();
        string path = "GPTLOG-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt";
        // This text is added only once to the file.
        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine($"\nUTC TIME: {time}- {prompt}\nUTC TIME: {time}-{reponse}");
            }
        }
        else
        {
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine($"\nUTC TIME: {time}- {prompt}\nUTC TIME: {time}-{reponse}");
            }
        }
    }

    // Generate method that gets current date and time in UTC format.
    public static string GetDate()
    {
        DateTime date = DateTime.UtcNow;
        return date.ToString("MM-dd-yyyy-HH:mm:ss");
    }

    // Returns a string that is made up of all "prompt" properties from a json file appended together.
    public static string GetPrompt(string botPersonality, string path)
    {
        string json = File.ReadAllText(path);
        var prompts = JsonConvert.DeserializeObject<List<ChatPrompt>>(json);

        StringBuilder sb = new StringBuilder();

        Debug.Log($"botPersonality: {botPersonality}");
        foreach (var prompt in prompts)
        {
            Debug.Log($"prompt type: {prompt.Type}");
            if (botPersonality == prompt.Type)
                sb.Append(prompt.Prompt);
        }

        // inject galloping_bull into the last prompt.
        sb.Append(prompts[prompts.Count - 1].Prompt);
        return sb.ToString();
    }


    private void DropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log($"Dropdown value changed! value: {change.value}");
        systemPrompt = GetPrompt(personalityOptions[change.value], promptPath);
        sytetemPromptTextField.text = FormatSystemPrompt(systemPrompt);
        ReInitialize(systemPrompt);
    }

    // format system prompt to be more readable.
    public static string FormatSystemPrompt(string s)
    {
        int index = s.IndexOf("In addition");
        return index < 0 ? s : s.Insert(index, "\n\n");
    }
}
public class ChatPrompt
{
    [JsonProperty("id")]
    public int ID { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("prompt")]
    public string Prompt { get; set; }

}