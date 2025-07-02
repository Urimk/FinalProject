using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages communication with the OpenAI GPT API for in-game AI interactions.
/// </summary>
public class GPTManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string ModelName = "gpt-3.5-turbo";
    private const int MaxTokens = 100;
    private const string SystemRole = "system";
    private const string UserRole = "user";

    // ==================== Private Fields ====================
    private string _apiKey;
    private string _apiUrl = ApiUrl;

    /// <summary>
    /// Loads the API key at startup.
    /// </summary>
    private void Start()
    {
        _apiKey = LoadApiKey();
        // Continue with your logic...
    }

    /// <summary>
    /// Loads the API key from the StreamingAssets folder.
    /// </summary>
    private string LoadApiKey()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "api_key.txt");
        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch (IOException ex)
            {
                Debug.LogError("Error reading API key file: " + ex.Message);
                return string.Empty;
            }
        }
        else
        {
            Debug.LogError("API key file not found at path: " + path);
            return string.Empty;
        }
    }

    /// <summary>
    /// Sends a prompt to the OpenAI API and invokes the callback with the response.
    /// </summary>
    public IEnumerator SendRequest(string prompt, System.Action<string> callback)
    {
        var requestData = new
        {
            model = ModelName,
            messages = new[]
            {
                new { role = SystemRole, content = "You are a game AI that asks and evaluates questions." },
                new { role = UserRole, content = prompt }
            },
            max_tokens = MaxTokens
        };

        string json = JsonConvert.SerializeObject(requestData);
        byte[] postData = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(postData),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + request.error);
            callback("Error: Could not reach OpenAI.");
        }
        else
        {
            try
            {
                string responseJson = request.downloadHandler.text;
                Debug.Log("Raw API Response: " + responseJson);

                // Use structured deserialization
                ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(responseJson);

                if (response != null && response.choices != null && response.choices.Count > 0)
                {
                    Debug.Log("Choices count: " + response.choices.Count);
                    if (response.choices[0].message != null && !string.IsNullOrEmpty(response.choices[0].message.content))
                    {
                        string reply = response.choices[0].message.content;
                        Debug.Log("Successfully parsed content: " + reply);
                        callback(reply);
                    }
                    else
                    {
                        Debug.LogError("Message or content is null in the first choice");
                        callback("Error: No content in response message.");
                    }
                }
                else
                {
                    Debug.LogError("No choices in response: " + (response?.choices == null ? "choices is null" : "choices is empty"));
                    callback("Error: No valid choices in the response.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON Parsing Error: " + e.Message + "\nStack trace: " + e.StackTrace);
                callback("Error: Unable to parse response. " + e.Message);
            }
        }
    }

    // Define classes for structured parsing
    [System.Serializable]
    private class MessageContent
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class Choice
    {
        public int index;
        public MessageContent message;
        public string finish_reason;
    }

    [System.Serializable]
    private class ApiResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public List<Choice> choices;
        public Dictionary<string, object> usage;
    }
}
