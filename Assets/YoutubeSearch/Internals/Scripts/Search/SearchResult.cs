    
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace Rinvo{
    public class SearchResult : UdonSharpBehaviour
    {
        [Header("External Connections")]
        public YoutubeSearchManager SearchManager = null;
        public UdonBehaviour UiController = null;
        public UdonBehaviour UiControllerQueue = null;
        public VRCUrlInputField UrlInputField = null;
        public InputField VideoNameInputField = null;
        public VRCUrlInputField UrlInputFieldQueue = null;
        public InputField VideoNameInputFieldQueue = null;
        public VideoPlayerType videoPlayerType = VideoPlayerType.USharpVideo;
        public string OtherVideoplayerRequredPlayEvents = "";
        public string QueueRequiredPlayEvents = "";
        public bool UsingQueue = false;
        public bool UsingOnlyQueue = false;
        public bool HasImagesheetProvided = true;

  
        [Header("Error Messages")]
        public string ErrorPreviewLoadingFailedDefault = "Can't load previews";
        public string ErorrNoPreviewsProvided = "Can't load previews due to high demand";
        public string ErrorPreviewProcessingFailed = "Can't process previews";


        [Header("UI Elements")]
        public Text uiTitle = null;
        public Text uiDuration = null;
        public Text uiChannelName = null; 
        public Text uiViews = null;
        public Text uiUploaded = null;
        public Text uiDescription = null;
        public Image uiPreviewImage = null;
        public GameObject ImageLoadingFailedPlaceholder = null;
        public Text uiImageLoadingFailedText = null;
        public GameObject ImageLoadingInProcessPlaceholder = null;

        public GameObject uiBtnAddQueue = null;
        public GameObject uiBtnPlayVideo = null;

        [Header("Settings (will set automatically)")]
        public bool swapToAvproIfNeeded = true;

        private bool isLive = false;
        private DataToken dataToken;
        private DataToken channelData;
        [HideInInspector] public VRCUrl url = null;

        void Start(){
            // ImageLoadingInProcessPlaceholder.SetActive(true);
            // ImageLoadingFailedPlaceholder.SetActive(false);
            // if(uiImageLoadingFailedText) uiImageLoadingFailedText.text = ErrorPreviewLoadingFailedDefault;
        }

        void PlayVideo(VRCUrl url){
            if(UrlInputField != null) UrlInputField.SetUrl(url);
            else ForwardError("No Url Input Field has been set up by the user, can't play", isCritical: true);

            // this one isn't required at all so no errors
            if(VideoNameInputField != null) VideoNameInputField.text = uiTitle.text; 


            if(videoPlayerType == VideoPlayerType.USharpVideo) UiController.SendCustomEvent("OnURLInput");
            else if(videoPlayerType == VideoPlayerType.ProTV2) UiController.SendCustomEvent("_EndEditUrlInput");
            else if(videoPlayerType == VideoPlayerType.ProTV3) UiController.SendCustomEvent("EndEditUrlInput");
            else if(videoPlayerType == VideoPlayerType.VizVid){
                UiController.SendCustomEvent("_OnURLEndEdit");
                UiController.SendCustomEvent("_InputConfirmClick");
            }
            else if(videoPlayerType == VideoPlayerType.YAMA) UiController.SendCustomEvent("PlayUrlTop");
            
            else if(videoPlayerType == VideoPlayerType.IwaSync3) {
                // select if its live or not, apparently easier to do that than not
                if(isLive) UiController.SendCustomEvent("ModeLive");
                else UiController.SendCustomEvent("ModeVideo");
                // fill the field again because IwaSync does it differently
                UrlInputField.SetUrl(url);
                // send event
                UiController.SendCustomEvent("OnURLChanged");
                // it sends more events so we do that too ig
                UiController.SendCustomEvent("ClearURL");
                UiController.SendCustomEvent("Close");
            }

            // in case videoplayer used is not supported directly - it still can be used:
            else if(videoPlayerType == VideoPlayerType.Other){
                if(OtherVideoplayerRequredPlayEvents == "") ForwardError("Editor Setup Error: Other videoplayer used but no events set up", isCritical: true);

                foreach(string eventName in OtherVideoplayerRequredPlayEvents.Split(',')){
                    UiController.SendCustomEvent(eventName.Trim());
                }
            }
        }
        
        void QueueVideo(VRCUrl url){
            if(UrlInputFieldQueue != null) UrlInputFieldQueue.SetUrl(url);
            else ForwardError("Editor Setup Error: Queue is enabled but no Queue Input Field has been set up by the user", isCritical: true);
            
            if(VideoNameInputFieldQueue != null) VideoNameInputFieldQueue.text = uiTitle.text; 
            else ForwardError("Editor Setup Error: Queue is enabled but no Video Name input field has been set up by the user", isCritical: false);
            
            if(QueueRequiredPlayEvents == "") ForwardError("Editor Setup Error: Queue is enabled but no Queue Input Events has been set up by the user", isCritical: true);

            foreach(string eventName in QueueRequiredPlayEvents.Split(',')){
                if(UiControllerQueue == null) UiController.SendCustomEvent(eventName.Trim());
                else UiControllerQueue.SendCustomEvent(eventName.Trim());
            }
        }

        public void OnPlayButtonPressed(){
            SwapBackendIfNeeded();
            PlayVideo(url);
        }

        public void OnQueueButtonPressed(){
            QueueVideo(url);
        }

        public void UpdateDataToken(DataToken searchResultEntryData){
            // save dataToken just in case we need it elsewhere
            dataToken = searchResultEntryData;

            // check if its a livestream
            if (searchResultEntryData.DataDictionary.TryGetValue("live", TokenType.Boolean, out DataToken isLiveToken)){
                isLive = isLiveToken.Boolean;
            }

            // set common stuff between streams and videos
            SetUiTextValueFromDataToken(uiTitle, "title", searchResultEntryData, TokenType.String, defaultString: "No Title");
            
            // force update parent layout group to recalculate its size
            uiTitle.gameObject.SetActive(false);
            uiTitle.gameObject.SetActive(true); 


            // with viewcount its a bit more bothersome - playlists dont have viewCountText
            // and live streams dont have shortViewCountText, so we have ot check whats available
            // so we check if viewCountText is available first
            if(GetStringValueFromDataToken("viewCountText", searchResultEntryData) != "")
                SetUiTextValueFromDataToken(uiViews, "viewCountText", searchResultEntryData, TokenType.String, defaultString: "No Viewcount"); 
            else 
                SetUiTextValueFromDataToken(uiViews, "shortViewCountText", searchResultEntryData, TokenType.String, defaultString: "No Viewcount"); 


            SetUiTextValueFromDataToken(uiDescription, "description", searchResultEntryData, TokenType.String, defaultString: "No Description");

            if(isLive){
                uiDuration.text = "live";
                uiUploaded.text = "live";
            }else{
                SetUiTextValueFromDataToken(uiDuration, "lengthText", searchResultEntryData, TokenType.String, defaultString: "No Length");
                SetUiTextValueFromDataToken(uiUploaded, "uploaded", searchResultEntryData, TokenType.String, defaultString: "No Upload Date");
            }

            // eeh since its inside of the other one we need to unpack it first:
            if(searchResultEntryData.DataDictionary.TryGetValue("channel", TokenType.DataDictionary, out DataToken channelDataToken)){
                channelData = channelDataToken;
                // and set it if we found it, otherwise No Channel Name
                SetUiTextValueFromDataToken(uiChannelName, "name", channelData, TokenType.String, defaultString: "No Channel Name");
            }

            // if(HasImagesheetProvided){
            //     ImageLoadingInProcessPlaceholder.SetActive(true);
            //     ImageLoadingFailedPlaceholder.SetActive(false);
            // }else{
            //     ImageLoadingInProcessPlaceholder.SetActive(false);
            //     ImageLoadingFailedPlaceholder.SetActive(true);
            //     if(uiImageLoadingFailedText) uiImageLoadingFailedText.text = ErorrNoPreviewsProvided;
            // }
        }

        public void UpdateUi(){
            uiBtnAddQueue.SetActive(false);
            uiBtnPlayVideo.SetActive(true);

            if(UsingQueue) uiBtnAddQueue.SetActive(true);
            if(UsingOnlyQueue) uiBtnPlayVideo.SetActive(false);

            if(HasImagesheetProvided){
                ImageLoadingInProcessPlaceholder.SetActive(true);
                ImageLoadingFailedPlaceholder.SetActive(false);
            }else{
                ImageLoadingInProcessPlaceholder.SetActive(false);
                ImageLoadingFailedPlaceholder.SetActive(true);
                if(uiImageLoadingFailedText) uiImageLoadingFailedText.text = ErorrNoPreviewsProvided;
            }
        }

        public void OnImageLoadCompleted(Texture2D _loadedImage){
            if(!Utilities.IsValid(uiPreviewImage)) return; // yeet if no valid ui element to host the image

            // get cropping data from dataToken
            if (dataToken.DataDictionary.TryGetValue("thumbnail", TokenType.DataDictionary, out DataToken ThumbnailData)){
                int _left = (int)ThumbnailData.DataDictionary["x"].Double;
                int _top = _loadedImage.height-(int)ThumbnailData.DataDictionary["y"].Double - (int)ThumbnailData.DataDictionary["height"].Double;;
                int _width =  (int)ThumbnailData.DataDictionary["width"].Double;
                int _height =  (int)ThumbnailData.DataDictionary["height"].Double;

                Rect rect = new Rect(_left, _top, _width, _height);
                uiPreviewImage.sprite = Sprite.Create(_loadedImage, rect, new Vector2(1f, 0f));

                ImageLoadingInProcessPlaceholder.SetActive(false);
                ImageLoadingFailedPlaceholder.SetActive(false);
            }
            else{
                ForwardError("Failed to cut preview image, no cropping data found", isCritical: false);
                ImageLoadingInProcessPlaceholder.SetActive(false);
                ImageLoadingFailedPlaceholder.SetActive(true);
                if(uiImageLoadingFailedText) uiImageLoadingFailedText.text = ErrorPreviewProcessingFailed;
            }
        }

        public void OnImageLoadError(){
            ImageLoadingInProcessPlaceholder.SetActive(false);
            ImageLoadingFailedPlaceholder.SetActive(true);
            if(uiImageLoadingFailedText) uiImageLoadingFailedText.text = ErrorPreviewLoadingFailedDefault;
        }

        private void SetUiTextValueFromDataToken(Text uiText, string valueName, DataToken dataToken, TokenType type = TokenType.String, string defaultString = "NoData"){
            if(!Utilities.IsValid(uiText)) return;
            if (dataToken.DataDictionary.TryGetValue(valueName, type, out DataToken value)){
                if(Utilities.IsValid(uiText)) uiText.text = value.ToString();
            }
            else{
                if(Utilities.IsValid(uiText)) uiText.text = defaultString;
            }
        }

        private string GetStringValueFromDataToken(string valueName, DataToken dataToken){
            if(dataToken.DataDictionary.TryGetValue(valueName, TokenType.String, out DataToken value)) return value.ToString();
            else return "";
        }

        private void ForwardError(string errorMessage, bool isCritical = false){
            SearchManager.ThrowError(errorMessage, isCritical);
        }
        
        // prob deprecated
        private void SwapBackendIfNeeded(){
            if(!swapToAvproIfNeeded) return; // yeet if disabled
            if(videoPlayerType != VideoPlayerType.USharpVideo) return; // not working on protv for now

            if(isLive){ 
                UiController.SendCustomEvent("OnStreamPlayerModeButtonPressed");
            };
        }

        // private float GetOverflowAmount(Text text)
        // {
        //     float _permissibleHeight = text.gameObject.GetComponent<RectTransform>().rect.height;
        //     if(_permissibleHeight < LayoutUtility.GetPreferredHeight(text.rectTransform)){
        //         return LayoutUtility.GetPreferredHeight(text.rectTransform) - _permissibleHeight;
        //     }else{
        //         return 0;
        //     }
            
        // }

        // private void adjustTextHeightForOverflow(Text targetText){
        //     if(!Utilities.IsValid(targetText)) return;
        //     targetText.rectTransform.sizeDelta = new Vector2(targetText.rectTransform.sizeDelta.x, LayoutUtility.GetPreferredHeight(targetText.rectTransform)+24);

        // }
    }
}