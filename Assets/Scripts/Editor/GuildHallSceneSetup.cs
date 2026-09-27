using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GuildHallSceneSetup
{
    private const string ScenePath = "Assets/Scenes/GuildHall.unity";
    private const string ArtPath = "Assets/Art/GuildHallPixelConcept.png";
    private const string RoomResourcePath = "Assets/Resources/GuildHallBackground-EmptyGuests.png";

    static GuildHallSceneSetup()
    {
        EditorApplication.delayCall += PrepareRuntimeArt;
        EditorApplication.delayCall += () => EnsureScene();
    }

    [MenuItem("Guild Hall/Prepare Art Imports")]
    public static void PrepareRuntimeArt()
    {
        ConfigureTexture(RoomResourcePath, true, false);
        ConfigureTexture("Assets/Resources/GuildHallGuestsAtlas.png", false, true);
        ConfigureTexture("Assets/Resources/GuildDayMap-v2.png", false, false);
    }

    private static void ConfigureTexture(string path, bool asSprite, bool readable)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        bool changed = importer.isReadable != readable || importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.npotScale != TextureImporterNPOTScale.None || importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp;
        if (asSprite && importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (asSprite && importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (!asSprite && importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
        importer.isReadable = readable;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        if (changed) importer.SaveAndReimport();
    }

    [MenuItem("Guild Hall/Create or Reset Prototype Scene")]
    public static void CreateScene()
    {
        EnsureScene(true);
    }

    private static void EnsureScene(bool force = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!force && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        if (AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath) == null)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (background == null)
        {
            Debug.LogError("Guild hall art not found at " + ArtPath);
            return;
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.08f, .07f, .1f);
        camera.transform.position = new Vector3(0, 0, -10);

        GameObject art = new GameObject("Guild Hall Pixel Art", typeof(SpriteRenderer));
        SpriteRenderer renderer = art.GetComponent<SpriteRenderer>();
        renderer.sprite = background;
        renderer.sortingOrder = -10;
        float targetHeight = camera.orthographicSize * 2f;
        art.transform.localScale = Vector3.one * (targetHeight / background.bounds.size.y);

        camera.gameObject.AddComponent<GuildHallRuntime>();

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Guild hall prototype scene created: " + ScenePath);
    }
}
