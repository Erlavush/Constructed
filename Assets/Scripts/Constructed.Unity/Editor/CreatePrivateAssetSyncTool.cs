#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Constructed.Unity
{
    public static class CreatePrivateAssetSyncTool
    {
        [MenuItem("Constructed/Sync Private Assets")]
        public static void Sync()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var result = CreatePrivateAssetSyncService.Sync(CreateFirstSlicePrivateAssetManifest.Manifest, projectRoot);
            
            Debug.Log($"Sync complete! Copied: {result.CopiedPaths.Count}, Unchanged: {result.UnchangedPaths.Count}, Missing: {result.MissingPaths.Count}");
            if (result.MissingPaths.Count > 0)
            {
                Debug.LogWarning("Missing files:\n" + string.Join("\n", result.MissingPaths));
            }
            
            AssetDatabase.Refresh();
        }
    }
}
#endif
