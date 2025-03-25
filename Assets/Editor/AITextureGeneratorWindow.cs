using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Net.Http;

public class AITextureGeneratorWindow : EditorWindow
{
    private Texture2D sourceTexture;
    private float noiseScale = 1f;
    private float persistence = 0.5f;
    private int octaves = 4;
    private Vector2 scrollPosition;

    private string apiKey = "";
    private const string API_KEY_PREF = "AITextureGenerator_APIKey";
    private const string OPENAI_API_ENDPOINT = "https://api.openai.com/v1/images/variations";
    private const string OPENAI_KEY_PREFIX = "sk-";

    private enum APIProvider
    {
        OpenAI,
        DeepSeek
    }

    private APIProvider selectedProvider = APIProvider.OpenAI;
    private const string DEEPSEEK_API_ENDPOINT = "https://api.deepseek.com/v1/images/variations";
    private const string DEEPSEEK_KEY_PREFIX = "sk-";
    private const string SELECTED_PROVIDER_PREF = "AITextureGenerator_SelectedProvider";

    [MenuItem("Window/AI Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<AITextureGeneratorWindow>("AI Texture Generator");
    }

    void OnEnable()
    {
        // Load saved API key when window opens
        apiKey = EditorPrefs.GetString(API_KEY_PREF, "");
        selectedProvider = (APIProvider)EditorPrefs.GetInt(SELECTED_PROVIDER_PREF, 0);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("AI Texture Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Add provider selection
        EditorGUI.BeginChangeCheck();
        selectedProvider = (APIProvider)EditorGUILayout.EnumPopup("API Provider", selectedProvider);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetInt(SELECTED_PROVIDER_PREF, (int)selectedProvider);
        }

        // Add API Key field
        EditorGUI.BeginChangeCheck();
        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(API_KEY_PREF, apiKey);
        }

        EditorGUILayout.Space();

        // Source texture field
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", 
            sourceTexture, typeof(Texture2D), false);

        // Parameters
        noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 0.1f, 10f);
        persistence = EditorGUILayout.Slider("Persistence", persistence, 0f, 1f);
        octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 8);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Texture"))
        {
            if (!ValidateAPIKey())
            {
                return;
            }

            if (sourceTexture != null && ValidateTexture())
            {
                GenerateTextureUsingAPI();
            }
            else if (sourceTexture == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Please assign a source texture first!", "OK");
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private bool ValidateTexture()
    {
        if (sourceTexture == null)
            return false;

        string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (importer == null)
            return false;

        if (!importer.isReadable)
        {
            EditorUtility.DisplayDialog("Invalid Texture Settings", 
                "The texture must have 'Read/Write Enabled' checked in its import settings.", "OK");
            return false;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            EditorUtility.DisplayDialog("Invalid Texture Settings", 
                "The texture should use uncompressed format to avoid artifacts.", "OK");
            return false;
        }

        return true;
    }

    private bool ValidateAPIKey()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            EditorUtility.DisplayDialog("Invalid API Key", 
                "Please enter an API key.", "OK");
            return false;
        }

        switch (selectedProvider)
        {
            case APIProvider.OpenAI:
                if (!apiKey.StartsWith(OPENAI_KEY_PREFIX))
                {
                    EditorUtility.DisplayDialog("Invalid API Key", 
                        "Please enter a valid OpenAI API key starting with 'sk-'", "OK");
                    return false;
                }
                if (apiKey.Length < 51)
                {
                    EditorUtility.DisplayDialog("Invalid API Key", 
                        "The OpenAI API key appears to be too short.", "OK");
                    return false;
                }
                break;

            case APIProvider.DeepSeek:
                if (!apiKey.StartsWith(DEEPSEEK_KEY_PREFIX))
                {
                    EditorUtility.DisplayDialog("Invalid API Key", 
                        "Please enter a valid DeepSeek API key starting with 'dsk-'", "OK");
                    return false;
                }
                break;
        }

        return true;
    }

    private async void GenerateTextureUsingAPI()
    {
        try
        {
            // Show progress bar
            EditorUtility.DisplayProgressBar("Generating Texture", 
                $"Sending request to {selectedProvider} API...", 0.5f);

            // Convert source texture to base64
            byte[] textureBytes = sourceTexture.EncodeToPNG();
            string base64Texture = System.Convert.ToBase64String(textureBytes);

            // Create HTTP client
            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var requestContent = selectedProvider == APIProvider.OpenAI 
                    ? CreateOpenAIRequest(textureBytes)
                    : CreateDeepSeekRequest(textureBytes);

                string endpoint = selectedProvider == APIProvider.OpenAI 
                    ? OPENAI_API_ENDPOINT 
                    : DEEPSEEK_API_ENDPOINT;

                var response = await client.PostAsync(endpoint, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var resultBytes = await response.Content.ReadAsByteArrayAsync();
                    SaveGeneratedTexture(resultBytes);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    EditorUtility.DisplayDialog("API Error", 
                        $"Failed to generate texture: {response.StatusCode}\n{errorContent}", "OK");
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Failed to generate texture: {e.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private HttpContent CreateOpenAIRequest(byte[] textureBytes)
    {
        var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(textureBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "image", "source.png");
        return form;
    }

    private HttpContent CreateDeepSeekRequest(byte[] textureBytes)
    {
        var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(textureBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "input_image", "source.png");
        form.Add(new StringContent("1"), "num_images");
        form.Add(new StringContent("1024x1024"), "size");
        return form;
    }

    private void SaveGeneratedTexture(byte[] textureBytes)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Generated Texture",
            "GeneratedTexture.png",
            "png",
            "Please enter a file name to save the texture to"
        );

        if (path.Length != 0)
        {
            System.IO.File.WriteAllBytes(path, textureBytes);
            AssetDatabase.Refresh();
        }
    }

    private void GenerateTexture()
    {
        // Create a copy of the source texture
        Texture2D newTexture = new Texture2D(sourceTexture.width, sourceTexture.height);
        Color[] pixels = sourceTexture.GetPixels();
        
        // Apply Perlin noise to modify the pixels
        for (int y = 0; y < sourceTexture.height; y++)
        {
            for (int x = 0; x < sourceTexture.width; x++)
            {
                float noiseValue = GenerateNoiseValue(x, y);
                int index = y * sourceTexture.width + x;
                pixels[index] = ModifyPixelColor(pixels[index], noiseValue);
            }
        }

        newTexture.SetPixels(pixels);
        newTexture.Apply();

        // Save the generated texture
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Generated Texture",
            "GeneratedTexture.png",
            "png",
            "Please enter a file name to save the texture to"
        );

        if (path.Length != 0)
        {
            byte[] bytes = newTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }
    }

    private float GenerateNoiseValue(int x, int y)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = x * noiseScale * frequency;
            float sampleY = y * noiseScale * frequency;
            
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            noiseValue += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= 2;
        }

        return Mathf.Clamp01(noiseValue);
    }

    private Color ModifyPixelColor(Color originalColor, float noiseValue)
    {
        return new Color(
            originalColor.r * noiseValue,
            originalColor.g * noiseValue,
            originalColor.b * noiseValue,
            originalColor.a
        );
    }
}