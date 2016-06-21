using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System.IO;
using Decoder = SevenZip.Compression.LZMA.Decoder;
using Xxtea;

namespace newx
{
    public delegate void OneUpdating(UpdateInfo info);

    public delegate void OneUpdated(UpdateInfo info);

    public delegate void AllUpdated(UpdateInfo info);

    public delegate void OneFailed(UpdateInfo info);
    public class GameUpdater : MonoBehaviour
    {
        [SerializeField]
        private VersionConfig localVersionConfig;
        [SerializeField]
        private VersionConfig remoteVersionConfig;
        [SerializeField]
        private List<BundleInfo> updateList;
        private string localVersionFileRelativePath;
        private string remoteVersionFileURL;
        private string remoteBundleAssetURL_Prefix;
        public float refreshInterval = 0.2f;
        public string password = "";
        private static GameUpdater _instance;

        public event OneUpdating OnOneUpdating;
        public event OneUpdated OnOneUpdated;
        public event AllUpdated OnAllUpdated;
        public event OneFailed OnOneFailed;

        public int updatingIndex = 0;
        public string updatingName = "";
        public uint bundleSizeUpdated = 0;
        public uint bundleSize = 0;
        public uint totalSizeUpdated = 0;
        public uint totalSize = 0;
        public int numBundlesUpdated = 0;
        public int numBundles = 0;

        public static string suffix = ".bytes";

        Dictionary<string, AssetBundle> assetBundleCache = new Dictionary<string, AssetBundle>();
        private GameUpdater() {

        }

        public static GameUpdater instance {
            get {
                if (_instance == null) {
                    _instance = UnityEngine.Object.FindObjectOfType<GameUpdater>();
                    if (_instance != null)
                        return _instance;
                    GameObject go = new GameObject();
                    go.name = "GameUpdater";
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<GameUpdater>();
                }
                return _instance;
            }

        }

        public void JustLoadLocalVersionFile(string localVersionFilePath, string password = "") {
            this.password = password;
            LoadLocalVersionFile(localVersionFilePath);
        }

        public IEnumerator DoUpdate(string localVersionFilePath, string remoteVersionFilePath,string remoteBundleAssetURLPrefix, string password) {
            this.password = password;
            this.remoteBundleAssetURL_Prefix = remoteBundleAssetURLPrefix;
            yield return LoadLocalVersionFile(localVersionFilePath);

            yield return LoadRemoteVersionFile(remoteVersionFilePath);
        }

        IEnumerator LoadLocalVersionFile(string relativePath) {
            localVersionFileRelativePath = relativePath;
            if (File.Exists(Application.temporaryCachePath + "/" + relativePath + suffix)) {
                localVersionConfig =
                    JsonMapper.ToObject<VersionConfig>(
                        File.ReadAllText(Application.temporaryCachePath + "/" + relativePath + suffix));
            }
            else {
                //安卓平台使用www访问
                //TextAsset objInResources = Resources.Load(relativePath, typeof(TextAsset)) as TextAsset;
                //localVersionConfig =
                //    JsonMapper.ToObject<VersionConfig>(objInResources.text);
                //Resources.UnloadAsset(objInResources);

                WWW www = new WWW(GetStreamingAssetPath(relativePath + suffix));
                yield return www;

                if (!string.IsNullOrEmpty(www.error)) {
                    Debug.LogError("this local vc.bytes in streamAssets read error:" + www.error);
                    www.Dispose();
                    //return ;
                    yield break;
                }
                byte[] localVerBuf = www.bytes;
                using (MemoryStream ms = new MemoryStream(localVerBuf)) {
                    using (StreamReader sr = new StreamReader(ms, System.Text.Encoding.UTF8)) {
                        string verTxt = sr.ReadToEnd();
                        localVersionConfig = JsonMapper.ToObject<VersionConfig>(verTxt);
                    }
                }
                www.Dispose();
            }
            yield return null;
            if (!Directory.Exists(Application.temporaryCachePath + "/" + localVersionConfig.bundleRelativePath)) {
                Directory.CreateDirectory(Application.temporaryCachePath + "/" + localVersionConfig.bundleRelativePath);
            }
        }

        IEnumerator LoadRemoteVersionFile(string url) {
            remoteVersionFileURL = url;//+suffix;
            WWW www = new WWW(remoteVersionFileURL + suffix);
            //StartCoroutine("LoadRemoteVersionFileHandler", www);
            yield return LoadRemoteVersionFileHandler(www);
        }

        IEnumerator LoadRemoteVersionFileHandler(WWW www) {
            yield return www;
            if (www.error == null) {
                remoteVersionConfig = JsonMapper.ToObject<VersionConfig>(www.text);
                if (NeedUpdate()) {
                    MakeUpdateList();
                    yield return UpdateFile();
                }
                else {
                    UpdateInfo info = new UpdateInfo(updatingName, bundleSizeUpdated, bundleSize, totalSizeUpdated,
                        totalSize, numBundlesUpdated, numBundles);
                    if (OnAllUpdated != null) {
                        OnAllUpdated(info);
                    }
                }
            }
            else {
                //ToDo:错误：没有找到
                //第一次上线肯定还没有更新包，所以查不到自动认为更新完成
                UpdateInfo info = new UpdateInfo(updatingName, bundleSizeUpdated, bundleSize, totalSizeUpdated,
                        totalSize, numBundlesUpdated, numBundles);
                if (OnAllUpdated != null) {
                    OnAllUpdated(info);
                }
            }
        }

        bool NeedUpdate() {
            return !localVersionConfig.versionNum.Equals(remoteVersionConfig.versionNum);
        }

        void MakeUpdateList() {
            updateList = new List<BundleInfo>();
            foreach (var remoteBundle in remoteVersionConfig.bundles) {
                bool isNew = true;
                foreach (var localBundle in localVersionConfig.bundles) {
                    if (remoteBundle.name == localBundle.name) {
                        isNew = false;
                        if (!remoteBundle.md5.Equals(localBundle.md5)) {
                            totalSize += remoteBundle.size;
                            updateList.Add(remoteBundle);
                            break;
                        }
                    }
                }
                if (isNew) {
                    totalSize += remoteBundle.size;
                    updateList.Add(remoteBundle);
                }
            }
            numBundles = updateList.Count;
        }

        IEnumerator UpdateFile() {
            if (updatingIndex < updateList.Count) {
                var updating = updateList[updatingIndex];
                updatingName = updating.name;
                bundleSize = updating.size;
                Debug.LogWarning("downloading bundleAsset:"+remoteBundleAssetURL_Prefix+"/" +updatingName);
                WWW www = new WWW(remoteBundleAssetURL_Prefix+"/" +updatingName);
                
                //StartCoroutine(DownloadUpdateFileHandler(www));
                yield return DownloadUpdateFileHandler(www);
            }
            else {
                Debug.LogWarning("update(download?) end: writeing to:"+Application.temporaryCachePath + "/" + localVersionFileRelativePath + suffix);
                yield return null;
                File.WriteAllText(Application.temporaryCachePath + "/" + localVersionFileRelativePath + suffix, JsonMapper.ToJson(remoteVersionConfig));
                UpdateInfo info = new UpdateInfo(updatingName, bundleSizeUpdated, bundleSize, totalSizeUpdated,
                        totalSize, numBundlesUpdated, numBundles);
               
                if (OnAllUpdated != null) {
                    OnAllUpdated(info);
                }
            }
        }

        IEnumerator DownloadUpdateFileHandler(WWW www) {
            //ToDo:错误处理呢？
            while (!www.isDone) {
                bundleSizeUpdated = (uint)www.bytesDownloaded;
                if (www.error == null) {
                    UpdateInfo info = new UpdateInfo(updatingName, bundleSizeUpdated, bundleSize, totalSizeUpdated + bundleSizeUpdated,
                        totalSize, numBundlesUpdated, numBundles);
                    if (OnOneUpdating != null) {
                        OnOneUpdating(info);
                    }
                    yield return new WaitForSeconds(refreshInterval);
                }
                else {
                    bundleSizeUpdated = (uint)www.bytesDownloaded;
                    UpdateInfo info = new UpdateInfo(updatingName, bundleSizeUpdated, bundleSize, totalSizeUpdated + bundleSizeUpdated,
                        totalSize, numBundlesUpdated, numBundles);
                    Debug.LogError("download bundleAsset error:"+updatingName+"   "+www.url);
                    if (OnOneFailed != null) {
                        OnOneFailed(info);
                    }

                    //下载出错
                    break;
                }
            }
            if (www.isDone) {
                totalSizeUpdated += bundleSize;
                //int childFolder = updatingName.LastIndexOf("/");
                //if (childFolder > 0) {
                //    string childFolderPath = Application.temporaryCachePath + "/" +
                //                             remoteVersionConfig.bundleRelativePath + "/" +
                //                             updatingName.Substring(0, childFolder);
                //    if (!Directory.Exists(childFolderPath)) {
                //        Directory.CreateDirectory(childFolderPath);
                //    }
                //}
                int childFolder = updatingName.LastIndexOf("/");
                string destPath = Application.temporaryCachePath + "/" + remoteVersionConfig.bundleRelativePath ;
                //string childFolderPath = destPath + "/" + updatingName;
                if (childFolder > 0) {
                   string childFolderPath = destPath + "/" + updatingName.Substring(0, childFolder);
                    //string dir = Path.GetDirectoryName(childFolderPath);
                    if (!Directory.Exists(childFolderPath)) {
                        Directory.CreateDirectory(childFolderPath);
                    }
                }

                File.WriteAllBytes( destPath+"/"+ updatingName + suffix, www.bytes);
                UpdateLocalVersionConfig();
                numBundlesUpdated++;
                UpdateInfo info = new UpdateInfo(updatingName, bundleSizeUpdated, bundleSize, totalSizeUpdated,
                        totalSize, numBundlesUpdated, numBundles);
                if (OnOneUpdated != null) {
                    OnOneUpdated(info);
                }
                updatingIndex++;
                yield return UpdateFile();
            }

        }

        void UpdateLocalVersionConfig() {
            BundleInfo downloadedBundle = updateList[updatingIndex];
            bool isNew = true;
            foreach (var localBundle in localVersionConfig.bundles) {
                if (localBundle.name == downloadedBundle.name) {
                    isNew = false;
                    localBundle.size = downloadedBundle.size;
                    localBundle.md5 = downloadedBundle.md5;
                    localBundle.include = downloadedBundle.include;
                    localBundle.dependency = downloadedBundle.dependency;
                    break;
                }
            }
            if (isNew) {
                localVersionConfig.bundles.Add(downloadedBundle);
            }
            Debug.LogWarning("UpdateLocalVersionConfig:"+Application.temporaryCachePath + "/" + localVersionFileRelativePath);
            string savePath = Application.temporaryCachePath + "/" + localVersionFileRelativePath;
            File.WriteAllText(savePath, JsonMapper.ToJson(localVersionConfig));
        }

        public AssetBundle LoadAssetBundle(string bundleName, bool cache = true) {
            if (assetBundleCache.ContainsKey(bundleName)) {
                return assetBundleCache[bundleName];
            }
            string path1 = Application.temporaryCachePath + "/" + localVersionConfig.bundleRelativePath + "/" + bundleName + suffix;
            string path2 = localVersionConfig.bundleRelativePath + "/" + bundleName;
            AssetBundle bundle = null;
            if (File.Exists(path1)) {
                using (var bundleStream = DeompressAndDecryptLZMA(path1)) {
                    if (bundleStream == null)
                        return bundle;
                    bundle = AssetBundle.LoadFromMemory(bundleStream.ToArray());
                    if (bundle == null)
                        return bundle;
                    if (cache)
                        assetBundleCache.Add(bundleName, bundle);
                }

            }
            else {
                using (var bundleStream = DeompressAndDecryptLZMA(path2, true)) {
                    if (bundleStream == null)
                        return bundle;
                    bundle = AssetBundle.LoadFromMemory(bundleStream.ToArray());
                    if (bundle == null)
                        return bundle;
                    if (cache)
                        assetBundleCache.Add(bundleName, bundle);
                }
            }
            return bundle;
        }

        public void DestroyAssetBundle(string bundleName) {
            if (assetBundleCache.ContainsKey(bundleName)) {
                assetBundleCache[bundleName].Unload(true);
                assetBundleCache.Remove(bundleName);
            }
        }

        MemoryStream DeompressAndDecryptLZMA(string path, bool fromStreamResourcesPath = false) {
            MemoryStream output = new MemoryStream();
            byte[] inputBytes = null;
            TextAsset objInResources = null;
            if (fromStreamResourcesPath) {
                //objInResources = Resources.Load(path, typeof(TextAsset)) as TextAsset;
                //if (objInResources == null)
                //    return null;
                //inputBytes = objInResources.bytes;
                WWW www = new WWW(GetStreamingAssetPath(path));
                while (!www.isDone) {
                }
                if (!string.IsNullOrEmpty(www.error)) {
                    www.Dispose();
                    return null;
                }
                inputBytes = www.bytes;
                www.Dispose();
            }
            else {
                inputBytes = File.ReadAllBytes(path);
            }
            Decoder coder = new Decoder();
            byte[] decryptedBytes = string.IsNullOrEmpty(password) ? inputBytes : XXTEA.Decrypt(inputBytes, password);
            using (MemoryStream mem = new MemoryStream()) {
                using (BinaryWriter binWriter = new BinaryWriter(mem)) {
                    binWriter.Write(decryptedBytes);
                    mem.Position = 0;
                    using (BinaryReader binReader = new BinaryReader(mem)) {
                        byte[] properties = new byte[5];
                        binReader.Read(properties, 0, 5);
                        byte[] fileLengthBytes = new byte[8];
                        binReader.Read(fileLengthBytes, 0, 8);
                        long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                        coder.SetDecoderProperties(properties);
                        coder.Code(mem, output, inputBytes.Length, fileLength, null);
                    }
                }

            }
            if (objInResources != null) {
                Resources.UnloadAsset(objInResources);
            }
            return output;
        }

        void Awake() {
            _instance = this;
        }
        /// <summary>
        /// 额外文件存储目录,凡是需要只读的文件都以这个路径为根目录
        /// 后面没有加'/',要静态存储的数据库/配置等一些内容都存入当前目录下
        /// 使用WWW加载
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static string RootStreamingAssetPath {
            get {

                string path = "";

                switch (Application.platform) {

                    case RuntimePlatform.Android:
                        // Android
                        path = "jar:file://" + Application.dataPath + "!/assets";
                        //jar:file://" + Application.dataPath + "!/assets/"
                        break;

                    case RuntimePlatform.IPhonePlayer:
                        // iOS
                        path = "file://" + Application.dataPath + "/Raw";
                        //Application.dataPath + "/Raw/"
                        break;
                    case RuntimePlatform.WindowsWebPlayer:
                        path = Application.dataPath + "/StreamingAssets/";
                        break;
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.OSXEditor:
                        // Windows Editor or Mac OS X Editor
                        path = "file://" + Application.dataPath + "/StreamingAssets";
                        break;
                    default:
                        path = "file://" + Application.streamingAssetsPath;
                        break;
                }

                return path;
            }
        }
        /// <summary>
        /// 获取存储文件的具体路径,加上了 RootStreamingAssetPath
        /// 安卓端需要www访问,移动端只读文件夹属性
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static string GetStreamingAssetPath(string assetPath) {
            string rootdir = RootStreamingAssetPath;
            return Path.Combine(rootdir, assetPath);
        }
    }

    public class UpdateInfo
    {
        //The assetbundle name of current assetbundle
        public string bundleName;
        //The downloaded file size of current assetbundle
        public uint bundleSizeUpdated;
        //The file size of current assetbundle
        public uint bundleSize;
        //The total file size of all assetbundles already update completed.
        public uint totalSizeUpdated;
        //The total file size of all assetbundles need to update.
        public uint totalSize;
        //The number of assetbundles already update completed.
        public int numBundleUpdated;
        //The total number of assetbundles  need to update.
        public int numBundle;

        public UpdateInfo(string bundleName, uint bundleSizeUpdated, uint bundleSize, uint totalSizeUpdated, uint totalSize, int numBundleUpdated, int numBundle) {
            this.bundleName = bundleName;
            this.bundleSizeUpdated = bundleSizeUpdated;
            this.bundleSize = bundleSize;
            this.totalSizeUpdated = totalSizeUpdated;
            this.totalSize = totalSize;
            this.numBundleUpdated = numBundleUpdated;
            this.numBundle = numBundle;
        }
    }
}
