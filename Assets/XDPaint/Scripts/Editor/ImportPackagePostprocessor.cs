using System.Collections.Generic;
using UnityEditor;

namespace XDPaint.Editor
{
    [InitializeOnLoad]
    public class ImportPackagePostprocessor
    {
        private static readonly KeyValuePair<string, string>[] AssetsToRemove =
        {
            new KeyValuePair<string, string>("TexturesKeeper", "75ae33b80cae64c25be3d931c115a0fe"),
            new KeyValuePair<string, string>("RenderTarget", "43bbae924d5314cd6a3f1c441abe5f50")
        };

        static ImportPackagePostprocessor()
        {
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        }

        private static void OnImportPackageCompleted(string packageName)
        {
            if (packageName == "2D3D Paint" || packageName == "2D/3D Paint")
            {
                foreach (var assetData in AssetsToRemove)
                {
                    var assetName = assetData.Key;
                    var guid = assetData.Value;
                    TryToRemoveAsset(assetName, guid);
                }
                AssetDatabase.Refresh();
            }
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
        }

        private static void TryToRemoveAsset(string assetName, string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && path.Contains(assetName))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}