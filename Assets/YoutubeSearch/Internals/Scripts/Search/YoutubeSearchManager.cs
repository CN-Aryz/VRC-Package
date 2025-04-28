using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.SDK3.StringLoading;
using UnityEngine.UI;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Data;
using VRC.SDK3.Image;




#if !COMPILER_UDONSHARP && UNITY_EDITOR // These using statements must be wrapped in this check to prevent issues on builds
using UnityEditor;
using System.Text.RegularExpressions;
using UdonSharpEditor;
using UnityEditor.Callbacks;
#endif


namespace Rinvo{
    // to avoid conflicts

    public class YoutubeSearchManager : UdonSharpBehaviour
    {
        private readonly string version = "1.12";

        [SerializeField] public VRCUrl[] vrcurls_pool = {};

        [Header("External Connections")]
        [SerializeField] public UdonBehaviour VideoPlayerUIController = null;
        [SerializeField] public VRCUrlInputField UrlInputField = null;
        [SerializeField] public InputField VideoNameInputField = null; // forward to result if specified 

        [SerializeField] public VRCUrlInputField UrlInputFieldQueue = null; // optional
        [SerializeField] public InputField VideoNameInputFieldQueue = null; // optional
        [SerializeField] private VideoPlayerType videoPlayerType = VideoPlayerType.USharpVideo;
        [SerializeField] private UdonBehaviour QueueUIController = null;
        [SerializeField] private string OtherVideoplayerRequredPlayEvents = "";
        [SerializeField] private string QueueRequiredPlayEvents = "";


        // [Header("Url Settings")]
        [SerializeField] public string ApiProviderUrl = "https://api.u2b.cx/";
        [SerializeField] public string poolName = "";
        [SerializeField] public int poolSize = 2000;
        [SerializeField] private VRCUrl defaultUrl = null;


        [Header("General Settings")]
        [SerializeField] public bool swapToAvproForLivestreams = true;
        [SerializeField] public bool latestOnTop = true;
        [SerializeField] public bool UsingQueue = false;
        [SerializeField] public bool UsingOnlyQueue = false;
        

        [Header("(Internal) UI Elements")]
        public Scrollbar ResultsScrollbar = null;
        public Text SearchRequestTextPlaceholder = null;
        public VRCUrlInputField UrlField = null;
        public GameObject searchResultTempalte = null;
        public Transform searchResultParent;
        public GameObject NextPageButtonTemplate = null;


        [Header("Placeholders UI")]
        public GameObject ResultsPlaceholder = null;
        public GameObject NoVideosFoundPlaceholder = null;
        public GameObject LoadingPlaceholder = null;


        [Header("Errors UI")]
        public GameObject ErrorPlaceholder = null;
        public Text ErrorPlaceholderTextInfo = null;


        [Header("Debug UI")]
        public Text DebugOutput = null;


    // caches
        private GameObject[] searchResultsCache = {};
        private SearchResult[] searchResultsReferencesCache = {};

        private string lastSearchRequestTrimmed = "";
        private VRCUrl lastSearchUrl = null;

        private VRCUrl currentSearchImagesheetVRCUrl = null;
        private VRCUrl nextPageVRCUrl = null;
        private GameObject DeployedNextPagePanel = null;
        private bool isPoolNameValid = false;
        private bool isPoolSizeValid = false;


    // image loader
        [Header("Image Loading stuff")]
        public Material dummyMaterialForImageLoading = null;
        private VRCImageDownloader _imageDownloader;
        private IUdonEventReceiver _udonEventReceiver;


        void Start(){
            if(!ValidateSetup()) return;

            UrlField.SetUrl(defaultUrl);
            ShowPlaceholder(true);
            
            // It's important to store the VRCImageDownloader as a variable, to stop it from being garbage collected!
            _imageDownloader = new VRCImageDownloader();
            // To receive Image and String loading events, 'this' is casted to the type needed
            _udonEventReceiver = (IUdonEventReceiver)this;
        }


        // this will prevent it from not having url set if manager was initially turned off
        void OnEnable() => UrlField.SetUrl(defaultUrl);
        

        bool ValidateSetup(){
            if(Utilities.IsValid(DebugOutput)) DebugOutput.text += $"Prefab Version: {version}";

            if(defaultUrl.ToString() == "" || vrcurls_pool.Length == 0){
                ThrowError($"Editor Setup Error: Search settings are not setup correctly: Find object called Search Manager, click on it and resolve all errors in the editor, then click Apply Settings. Read Readme.txt if needed.", critical: true);
                return false;
            }

            if(VideoPlayerUIController == null ){
                ThrowError($"Editor Setup Error: Videoplayer Reference is not set correctly: Find object called Search Manager, click on it and resolve all errors in the editor, then click Apply Settings. Read Readme.txt if needed.", critical: true);
                return false;
            }

            return true;
        }


    // String loading and search
        public void OnUrlFieldEnter(){
            VRCUrl url = UrlField.GetUrl();
            
            if(defaultUrl.ToString() == "" || vrcurls_pool.Length == 0){
                ThrowError($"Editor Setup Error: Search settings are not setup correctly: Find object called Search Manager, click on it and resolve all errors in the editor, then click Apply Settings. Read Readme.txt if needed.", critical: true);
                return;
            }

            if(!url.ToString().Contains(defaultUrl.ToString().Trim())){
                ShowErrorPlaceholder(show: true, errorMessage: "Invalid URL, try again");
                ResetUrlField();
                return;
            }

            LoadJsonUrl(url);
        }

        public void LoadJsonUrl(VRCUrl url){
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
            lastSearchUrl = url;
            ShowLoadingPlaceholder(true);
            ResetUrlField();
            
            if(lastSearchUrl.ToString().Contains("→")){
                UpdateLastSearchRequestTrimmed();
            }
        }

        public void OnNextPageButtonPressed(){
            if(nextPageVRCUrl == null) return;
            ShowLoadingPlaceholder(true);

            LoadJsonUrl(nextPageVRCUrl);   
        }


        public override void OnStringLoadSuccess(IVRCStringDownload downloadedData){
            // successfull loading doesn't mean data is fine yet

            // clear data from the last search;
            ClearScrollviewContent(); 
            ClearResultsCache();

            // Trying to deserealize data
            DataToken ReceivedData;
            if(VRCJson.TryDeserializeFromJson(downloadedData.Result, out DataToken deserializedData)){
                ReceivedData = deserializedData;
                if(Utilities.IsValid(DebugOutput)) DebugOutput.text = $"Prefab Version: {version}\n";
                if(Utilities.IsValid(DebugOutput)) DebugOutput.text += downloadedData.Result;
            }else{
                ThrowError($"JSON Deserialization error message: {deserializedData}", critical: false);
                ThrowError("Donwloaded String Deserialization Failed", critical: true);
                return;
            }

            // extracting data from deserealized data
            DataToken ResultsData = new DataToken();
            if (ReceivedData.DataDictionary.TryGetValue("results", TokenType.DataList, out DataToken ResultsDataToken)){
                ResultsData = ResultsDataToken;
            }else{
                // throw error if failed to find results in json
                ThrowError("JSON doesn't contain Results object", critical: true);
                return;
            }

            if(ResultsData.DataList.ToArray().Length == 0){
                ShowNoVideosFoundPlaceholder(true);
                ThrowError("JSON doesn't contain any videos (no videos found)", critical: false);
                return;
            }

            bool hasImagesheetProvided = false;

            // Load image data if needed (yeah, vrc doesn't treat int as an int for some reason so we need to dance around with converting double into int lol)
            if (ReceivedData.DataDictionary.TryGetValue("imagesheet_vrcurl", TokenType.Double, out DataToken imagesheet_vrcurlId)){
                currentSearchImagesheetVRCUrl = vrcurls_pool[(int)imagesheet_vrcurlId.Double];
                LoadImages(currentSearchImagesheetVRCUrl);
                hasImagesheetProvided = true;
            }else{
                ThrowError("Imagesheet_vrcurl not found in JSON");
            }


            // iterating through results and creating UI entries for them:
            foreach(DataToken token in ResultsData.DataList.ToArray()){
                int vrcurl_id = (int)token.DataDictionary["vrcurl"].Double;
                
                // Instantiate search result template:
                GameObject _newSearchRes = GameObject.Instantiate(searchResultTempalte, searchResultParent);
                SearchResult _res = _newSearchRes.GetComponent<SearchResult>();


                // Setup main result data
                _res.url = vrcurls_pool[vrcurl_id];
                _res.UpdateDataToken(token);
                _res.HasImagesheetProvided = hasImagesheetProvided;
                
                // setup videoplayer type
                _res.videoPlayerType = videoPlayerType;

                // Main Videoplayer
                _res.UiController = VideoPlayerUIController;
                _res.UrlInputField = UrlInputField;
                _res.VideoNameInputField = VideoNameInputField;
                _res.OtherVideoplayerRequredPlayEvents = OtherVideoplayerRequredPlayEvents;

                // Queue
                _res.UsingQueue = UsingQueue;
                _res.UsingOnlyQueue = UsingOnlyQueue;
                _res.UiControllerQueue = QueueUIController;
                _res.UrlInputFieldQueue = UrlInputFieldQueue;
                _res.VideoNameInputFieldQueue = VideoNameInputFieldQueue;
                _res.QueueRequiredPlayEvents = QueueRequiredPlayEvents;

                // Setup videoplayer type
                _res.videoPlayerType = videoPlayerType;
                
                // Other stuff
                _res.swapToAvproIfNeeded = swapToAvproForLivestreams;
                _res.SearchManager = this; 
        
                // Display it!
                _newSearchRes.SetActive(true);
                _res.UpdateUi();

                // Chache them so we have all the references we will need later
                searchResultsCache = searchResultsCache.Add(_newSearchRes);
                searchResultsReferencesCache = searchResultsReferencesCache.Add(_res);
            }


            if(ReceivedData.DataDictionary.TryGetValue("nextpage_vrcurl", TokenType.Double, out DataToken NextPageUrlValue)){
                nextPageVRCUrl = vrcurls_pool[(int)NextPageUrlValue.Double];
                // Make a button:
                if(ResultsData.DataList.ToArray().Length!=0 && DeployedNextPagePanel == null){
                    DeployedNextPagePanel = GameObject.Instantiate(NextPageButtonTemplate, searchResultParent);
                    DeployedNextPagePanel.SetActive(true);
                }
            }  
            else{
                ThrowError(errorText: "ERROR: Failed to extract next page Url");
                nextPageVRCUrl = null;
            }


            ShowPlaceholder(false);
            ShowErrorPlaceholder(false);
            ShowNoVideosFoundPlaceholder(false);
            ShowLoadingPlaceholder(false);

            // force update layout component
            SendCustomEventDelayedFrames(nameof(ForceResetLayout), 10);

            SearchRequestTextPlaceholder.text = lastSearchRequestTrimmed;
        }

        public override void OnStringLoadError(IVRCStringDownload result){
            ShowErrorPlaceholder(true); // todo: maybe the reason behind infinite loading 
            ShowNoVideosFoundPlaceholder(false);
            ShowLoadingPlaceholder(false);

            ThrowError($"String loading failed: {result.Error}", critical: true);
        }


    // loading images
        private void LoadImages(VRCUrl url){
            _imageDownloader.DownloadImage(url, dummyMaterialForImageLoading, _udonEventReceiver);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result){
            // send downloaded texture to the current results entries to deal with:
            Texture2D downloadedTexture = result.Result;
            foreach(SearchResult _res in searchResultsReferencesCache){
                _res.OnImageLoadCompleted(downloadedTexture);
            }
        }

        public override void OnImageLoadError(IVRCImageDownload result){
            foreach(SearchResult _res in searchResultsReferencesCache){
                _res.OnImageLoadError();
            }
        }


    // Utility methods:
        public void ResetUrlField(){
            UrlField.SetUrl(defaultUrl);
        }

        public void ShowPlaceholder(bool show){
            if(show){
                ShowErrorPlaceholder(false);
                ShowNoVideosFoundPlaceholder(false);
                ShowLoadingPlaceholder(false);
            }

            if(Utilities.IsValid(ResultsPlaceholder)) ResultsPlaceholder.SetActive(show);
        }

        public void ShowLoadingPlaceholder(bool show){
            if(show){
                ShowErrorPlaceholder(false);
                ShowNoVideosFoundPlaceholder(false);
                ShowPlaceholder(false);
            }
            if(Utilities.IsValid(LoadingPlaceholder)) LoadingPlaceholder.SetActive(show);
        }

        private void ShowNoVideosFoundPlaceholder(bool show){
            if(show){
                ShowPlaceholder(false);
                ShowErrorPlaceholder(false);
                ShowLoadingPlaceholder(false);
            }
            if(Utilities.IsValid(NoVideosFoundPlaceholder)) NoVideosFoundPlaceholder.SetActive(show);
        }

        private void ShowErrorPlaceholder(bool show, string errorMessage=""){
            if(show){
                ShowPlaceholder(false);
                ShowNoVideosFoundPlaceholder(false);
                ShowLoadingPlaceholder(false);
            }
            if(Utilities.IsValid(ErrorPlaceholder)) ErrorPlaceholder.SetActive(show);
            if(show && errorMessage!=""){
                ErrorPlaceholderTextInfo.text = errorMessage; 
            }
        }

        public void ClearResultsCache(){
            searchResultsCache = searchResultsCache.Resize(0);
            searchResultsReferencesCache = searchResultsReferencesCache.Resize(0);
        }

        public void ClearScrollviewContent(){
            // just make sure to call it before ClearResultsCache lol
            foreach(GameObject obj in searchResultsCache){
                GameObject.Destroy(obj);
            }
            if(DeployedNextPagePanel!=null) GameObject.Destroy(DeployedNextPagePanel);
            DeployedNextPagePanel = null;
            ResetScroll();
        }
        public int GetPoolSize() => vrcurls_pool.Length;

        DataToken DeserializeFromJson(string json){
            if(VRCJson.TryDeserializeFromJson(json, out DataToken result)){
                // Deserialization succeeded! Let's figure out what we've got.
                if (result.TokenType == TokenType.DataDictionary){
                    Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                }
                else if (result.TokenType == TokenType.DataList){
                    Debug.Log($"Successfully deserialized as a list with {result.DataList.Count} items.");
                }
                else{
                    // pretty much impossible lol
                    ThrowError(errorText: "How did we get here?", critical: true);
                }
            }else{
                // Deserialization failed. Let's see what the error was.
                ThrowError($"Failed to Deserialize json:\n {result}");
            }
            return result;
        }

        private void UpdateLastSearchRequestTrimmed(){
            // special case for playlists
            if(lastSearchUrl.ToString().Contains("list=")){
                lastSearchRequestTrimmed = "custom playlist";
            }


            if(lastSearchUrl.ToString().Contains("→")){
                // if → is there - trim and save

                if(lastSearchUrl.ToString().Contains("list=")) lastSearchRequestTrimmed = "custom playlist";
                else lastSearchRequestTrimmed = lastSearchUrl.ToString().Substring(lastSearchUrl.ToString().IndexOf('→')).Replace("→", "").Trim();
            }else{
                // otherwise leave empty
                lastSearchRequestTrimmed = "";
            }
        }

        private void ResetScroll(){
            ResultsScrollbar.value = 1;
        }

        public void ForceResetLayout(){
            searchResultParent.gameObject.SetActive(false);
            searchResultParent.gameObject.SetActive(true);
        }

        public void ThrowError(string errorText, bool critical=false){
            if(Utilities.IsValid(DebugOutput)) DebugOutput.text += $"\n[{this.name}] ERROR: {errorText}";
            Debug.Log($"[{this.name}] ERROR: {errorText}");
            if(critical){
                ShowErrorPlaceholder(show: true, errorMessage: errorText);
            }
        }



#if !COMPILER_UDONSHARP && UNITY_EDITOR
[CustomEditor(typeof(YoutubeSearchManager))]
public class CustomInspectorEditor : Editor{

    private AppliedSettings previousSettings;

    private struct AppliedSettings
    {
        public string ApiProviderUrl;
        public string poolName;
        public int poolSize;
    }
    private bool usingQueue = false;
    private bool showDebugInfo = false;
    private bool showContent = false;



    private Regex regex = new Regex("[^a-z_0-9]+");

    public override void OnInspectorGUI(){
        
        YoutubeSearchManager target = (YoutubeSearchManager)this.target;

        EditorGUILayout.Space(30);
        GUILayout.Label("Youtube Search Prefab by Rinvo & Lamp", EditorStyles.whiteLargeLabel);
        GUILayout.Label("Personal version 1.12", EditorStyles.miniLabel);
        EditorGUILayout.Space(30);  
    
        GUILayout.Label("References:", EditorStyles.boldLabel);

        target.VideoPlayerUIController = (UdonBehaviour)EditorGUILayout.ObjectField("Video Player UI Handler:", target.VideoPlayerUIController, typeof(UdonBehaviour), true);
        if (target.VideoPlayerUIController == null) EditorGUILayout.HelpBox("To play the video you need to specity UI Handler - script which videoplayer uses to process URL Inputs by user", MessageType.Error);
        
        target.UrlInputField = (VRCUrlInputField)EditorGUILayout.ObjectField("Url InputField:", target.UrlInputField, typeof(VRCUrlInputField), true);
        if (target.UrlInputField == null) EditorGUILayout.HelpBox("For videoplayer to know what URL, you need to specify VRCUrlInputField that user should input video's URL Into", MessageType.Error);
        
        target.VideoNameInputField = (InputField)EditorGUILayout.ObjectField("Video Name InputField (optional):", target.VideoNameInputField, typeof(InputField), true);

        target.videoPlayerType = (VideoPlayerType)EditorGUILayout.EnumPopup("Video Player Type:", target.videoPlayerType);
        
        // in case we select other videoplayer type we will need to specify some more requirements
        if(target.videoPlayerType == VideoPlayerType.Other) {
            EditorGUI.indentLevel++;
            target.OtherVideoplayerRequredPlayEvents = (string)EditorGUILayout.TextField("Other videoplayer events:", target.OtherVideoplayerRequredPlayEvents);
            if(target.OtherVideoplayerRequredPlayEvents == "") EditorGUILayout.HelpBox("In order for Other videoplayers to work, you need to specify events that will be sent to UI Handler in order to play the video, those events can be found in the VRCUrlInputField it uses and listed here separated by the , (comma) if multiple is required", MessageType.Error);
            EditorGUI.indentLevel--;
        }

        usingQueue = EditorGUILayout.Foldout(usingQueue, "Queue settings", EditorStyles.foldout);
        if (usingQueue) {
            EditorGUI.indentLevel++;
            target.UsingQueue = EditorGUILayout.Toggle("Using Queue Plugin", target.UsingQueue);

            target.UsingOnlyQueue = EditorGUILayout.Toggle("Only Queue", target.UsingOnlyQueue);
            if(target.UsingQueue && target.UsingOnlyQueue) EditorGUILayout.HelpBox("This will disable the default Play button, some Queue plugins do not support bypassing the queue (like default ProTV3 Queue plugin)", MessageType.Info);
            
            target.QueueUIController = (UdonBehaviour)EditorGUILayout.ObjectField("Queue UI Handler:", target.QueueUIController, typeof(UdonBehaviour), true);
            if(target.UsingQueue && target.QueueUIController == null) EditorGUILayout.HelpBox("For Search to know where to send URL for the Queue you need to specify UI Handler of the Queue plugin. If left empty - UI Handler of the main videoplayer will be used", MessageType.Warning);
            
            target.UrlInputFieldQueue = (VRCUrlInputField)EditorGUILayout.ObjectField("Queue Url Input Field:", target.UrlInputFieldQueue, typeof(VRCUrlInputField), true);
            if(target.UsingQueue && target.UrlInputFieldQueue == null) EditorGUILayout.HelpBox("For Queue to know what URL to save you need to specify VRCUrlInputField that user should input video's URL Into", MessageType.Error);

            target.VideoNameInputFieldQueue = (InputField)EditorGUILayout.ObjectField("Queue Video Name Input Field:", target.VideoNameInputFieldQueue, typeof(InputField), true);
            if(target.UsingQueue && target.VideoNameInputFieldQueue == null) EditorGUILayout.HelpBox("For Queue to know what to call the saved video you need to specify InputField that user should input name of the video, but its not required.", MessageType.Warning);
            
            target.QueueRequiredPlayEvents = (string)EditorGUILayout.TextField("Queue Required Input Events:", target.QueueRequiredPlayEvents);
            if(target.UsingQueue && target.QueueRequiredPlayEvents == "") EditorGUILayout.HelpBox("In order for Queue to to save the video you need to specify events that will be sent to UI Handler in order to play the video, those events can be found in the VRCUrlInputField it uses and listed here separated by the , (comma) if multiple is required", MessageType.Error);
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.Separator();
        GUILayout.Label("Search Settings", EditorStyles.boldLabel);
        target.ApiProviderUrl = EditorGUILayout.TextField("API Provider Url:", target.ApiProviderUrl);
        target.poolName = EditorGUILayout.TextField("URLs Pool Name", target.poolName);
        target.latestOnTop = EditorGUILayout.Toggle("Preoretize Latest Videos", target.latestOnTop);
        ValidatePoolName(target);

        target.poolSize = EditorGUILayout.IntField("URLs Pool Size", target.poolSize);

        ValidatePoolSize(target);
        // need to put stuff here
        if (target.isPoolNameValid && target.isPoolSizeValid && target.VideoPlayerUIController != null && target.UrlInputField != null){
            
            if (GUILayout.Button("Apply Settings")) ApplySettings(target);
            

            if(target.poolSize != previousSettings.poolSize ||
                    target.poolName != previousSettings.poolName ||
                    target.ApiProviderUrl != previousSettings.ApiProviderUrl){
                EditorGUILayout.HelpBox("Current settings are not applied yet, please click Apply Settings", MessageType.Warning);
            } else{
                EditorGUILayout.HelpBox("Settings are applied, good to go!", MessageType.Info);
            }
        }else{
            EditorGUILayout.HelpBox("Please fix any errors before applying the settings.", MessageType.Error);
        }
        
        EditorGUILayout.Space(20);
        GUILayout.Label("Advanced stuff:", EditorStyles.boldLabel);

        showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug information", EditorStyles.foldout);
        if (showDebugInfo) {
            DisplayDebugInfo(target);
            EditorGUILayout.Space(20);
        }

        showContent = EditorGUILayout.Foldout(showContent, "Internal Settings", EditorStyles.foldout);
        if (showContent){
            EditorGUILayout.Separator();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            DrawDefaultInspector();
            EditorGUILayout.Separator();
            EditorGUILayout.Space(20);
        }

        EditorGUILayout.Space(20);
        GUILayout.Label("Copyright Rinvo 2024", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label("discord: rinvo", EditorStyles.centeredGreyMiniLabel);
    }

    private void ValidatePoolName(YoutubeSearchManager target)
    {
        bool isPoolNameValid = true;

        if (string.IsNullOrEmpty(target.poolName)){
            isPoolNameValid = false;
            EditorGUILayout.HelpBox("Name of the Pool can't be empty, you can use something like username_worldname", MessageType.Error);
        }
        else if (target.poolName.Length < 5){
            isPoolNameValid = false;
            EditorGUILayout.HelpBox("Pool name can't be shorter than 5 symbols", MessageType.Error);
        }
        else if (target.poolName.Length > 50){
            isPoolNameValid = false;
            EditorGUILayout.HelpBox("Pool name can't be longer than 50 symbols", MessageType.Error);
        }
        else if (target.poolName != target.poolName.ToLower()){
            isPoolNameValid = false;
            EditorGUILayout.HelpBox("Pool name can't have uppercase symbols in it", MessageType.Error);
        }
        else if (regex.IsMatch(target.poolName)){
            isPoolNameValid = false;
            EditorGUILayout.HelpBox("Pool name should contain only lowercase letters, numbers or symbol _ (underscore)", MessageType.Error);
        }
        else if (int.TryParse(target.poolName[target.poolName.Length - 1].ToString(), out _)){
            isPoolNameValid = false;
            EditorGUILayout.HelpBox("Last symbol cannot be a number", MessageType.Error);
        }

        target.isPoolNameValid = isPoolNameValid;
    }

    private void ValidatePoolSize(YoutubeSearchManager target)
    {
        if (target.poolSize < 100){
            EditorGUILayout.HelpBox("Pool Size less than 100 is really not recommended", MessageType.Error);
            target.isPoolSizeValid = false;
        }
        else if (target.poolSize > 100000){
            EditorGUILayout.HelpBox("Pool Size cannot be more than 100_000", MessageType.Error);
            target.isPoolSizeValid = false;
        }
        else{
            target.isPoolSizeValid = true;
        }
    }

    private void ApplySettings(YoutubeSearchManager target){

        if(target.latestOnTop){
            target.defaultUrl = new VRCUrl($"{target.ApiProviderUrl}search?pool={target.poolName}{target.poolSize}&mode=latestontop&thumbnails=yes&input=                                                                Type search here →                               ​");
        }else{
            target.defaultUrl = new VRCUrl($"{target.ApiProviderUrl}search?pool={target.poolName}{target.poolSize}&thumbnails=yes&input=                                                                Type search here →                               ​");
        }
        // save settings to compare to
        previousSettings = new AppliedSettings{
            ApiProviderUrl = target.ApiProviderUrl,
            poolName = target.poolName,
            poolSize = target.poolSize,
        };
        EditorUtility.SetDirty(target);
    }


    // Play / Upload callback hackery
    [PostProcessScene(-100)]
    public static void OnPostprocessScene(){
        YoutubeSearchManager[] managers = FindObjectsOfType<YoutubeSearchManager>();
        
        foreach (YoutubeSearchManager manager in managers){
            ProcessManager(manager);
        }
    }

    private static void ProcessManager(YoutubeSearchManager manager){
        if (manager == null) return;
        
        if(manager.vrcurls_pool.Length == manager.poolSize && manager.vrcurls_pool[0].ToString().Contains($"https://api.u2b.cx/vrcurl/{manager.poolName}{manager.poolSize}/")) return;

        manager.vrcurls_pool = new VRCUrl[manager.poolSize];
        string poolUrl = $"https://api.u2b.cx/vrcurl/{manager.poolName}{manager.poolSize}/";

        for (var i = 0; i < manager.vrcurls_pool.Length; i++){
            manager.vrcurls_pool[i] = new VRCUrl(poolUrl + i);
        }
    }

    

    private void DisplayDebugInfo(YoutubeSearchManager target){
        EditorGUI.indentLevel++;
        GUILayout.Label($"Current Pool Size: {target.poolSize}");
        GUILayout.Label("Current default URL:");
        EditorGUILayout.SelectableLabel(target.defaultUrl.ToString(), EditorStyles.textField);
        EditorGUI.indentLevel--;
    }
    }
#endif
    }
}