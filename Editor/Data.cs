using System;
using System.IO;
using UnityEngine;

namespace AssetReferenceViewer
{
    [Serializable]
    public class Data
    {
        private const string JSON_PATH = "UserSettings/AssetReferenceViewerSettings.json";

        [SerializeField]
        private bool isUpToDate = false;
        public static bool IsUpToDate { get { return Instance.isUpToDate; } set { Instance.isUpToDate = value; } }

        [SerializeField]
        private AssetInfo[] assetInfos;
        public static AssetInfo[] AssetInfos {
            get => Instance.assetInfos ?? (Instance.assetInfos = new AssetInfo[0]);
            set => Instance.assetInfos = value;
        }

        private static Data instance;
        public static Data Instance {
            get {
                if (instance == null) {
                    if (File.Exists(JSON_PATH)) {
                        instance = JsonUtility.FromJson<Data>(File.ReadAllText(JSON_PATH));
                    } else {
                        instance = new Data();
                        File.WriteAllText(JSON_PATH, JsonUtility.ToJson(instance));
                    }
                }
                return instance;
            }
        }

        public static void Save()
        {
            File.WriteAllText(JSON_PATH, JsonUtility.ToJson(Instance));
        }
    }
}