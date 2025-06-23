using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

public class GPTManager : MonoBehaviour
{
    private string _apiKey;
    private string _apiUrl = "https://api.openai.com/v1/chat/completions";

    private void Start()
    {
        _apiKey = LoadApiKey();

        // Continue with your logic...
    }

    private string LoadApiKey()
    {
        // Path to the api_key.txt file in the Resources folder (or StreamingAssets if needed)
        string path = Path.Combine(Application.streamingAssetsPath, "api_key.txt");

        if (File.Exists(path))
        {
            try
            {
                // Read the API key from the file
                return File.ReadAllText(path).Trim(); // Trim to remove extra whitespace
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
    public IEnumerator SendRequest(string prompt, System.Action<string> callback)
    {
        var requestData = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are a game AI that asks and evaluates questions." },
                new { role = "user", content = prompt }
            },
            max_tokens = 100
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
