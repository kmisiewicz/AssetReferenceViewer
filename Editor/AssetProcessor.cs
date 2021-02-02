using UnityEditor;
using System.Collections.Generic;
using System;

namespace AssetReferenceViewer
{
    /// <summary>
    /// The purpose of this class is to try detecting asset changes to automatically update the ProjectCurator database.
    /// </summary>
    public class AssetProcessor : UnityEditor.AssetModificationProcessor
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// Some callbacks must be delayed on next frame
        /// </summary>
        private static void OnUpdate()
        {
            while (Actions.Count > 0) {
                Actions.Dequeue()?.Invoke();
            }
        }

        private static Queue<Action> Actions = new Queue<Action>();

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (Data.IsUpToDate) {
                Actions.Enqueue(() => {
                    foreach (string path in paths) {
                        AssetReferenceViewer.RemoveAssetFromDatabase(path);
                        AssetReferenceViewer.AddAssetToDatabase(path);
                    }
                    AssetReferenceViewer.SaveDatabase();
                });
            }
            return paths;
        }

        static void OnWillCreateAsset(string assetName)
        {
            if (Data.IsUpToDate) {
                Actions.Enqueue(() => {
                    AssetReferenceViewer.AddAssetToDatabase(assetName);
                    AssetReferenceViewer.SaveDatabase();
                });
            }
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetName, RemoveAssetOptions removeAssetOptions)
        {
            if (Data.IsUpToDate) {
                AssetReferenceViewer.RemoveAssetFromDatabase(assetName);
                AssetReferenceViewer.SaveDatabase();
            }
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (Data.IsUpToDate) {
                Actions.Enqueue(() => {
                    AssetReferenceViewer.RemoveAssetFromDatabase(sourcePath);
                    AssetReferenceViewer.AddAssetToDatabase(destinationPath);
                    AssetReferenceViewer.SaveDatabase();
                });
            }
            return AssetMoveResult.DidNotMove;
        }
    }
}