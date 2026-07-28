using UnityEditor;

/// <summary>
/// Editor-only helper: any texture dropped into a <c>Resources/Icons/</c> folder is
/// automatically imported as a UI Sprite. This means teammates can just drag PNGs
/// into the folder without touching import settings by hand.
/// </summary>
public class IconAutoImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Icons/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
    }
}
