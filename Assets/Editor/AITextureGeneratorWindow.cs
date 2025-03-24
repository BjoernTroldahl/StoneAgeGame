using UnityEngine;
using UnityEditor;

public class AITextureGeneratorWindow : EditorWindow
{
    private Texture2D sourceTexture;
    private float noiseScale = 1f;
    private float persistence = 0.5f;
    private int octaves = 4;
    private Vector2 scrollPosition;

    [MenuItem("Window/AI Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<AITextureGeneratorWindow>("AI Texture Generator");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("AI Texture Generator", EditorStyles.boldLabel);
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
            if (sourceTexture != null && ValidateTexture())
            {
                GenerateTexture();
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