

namespace Rinvo{
    public enum VideoPlayerType
    {
        USharpVideo, // using OnURLInput 
        ProTV2, // using _EndEditUrlInput
        ProTV3, // using EndEditUrlInput
        IwaSync3, // using OnURLChanged
        VizVid, // using _OnURLEndEdit
        YAMA, // using PlayUrlTop
        Other // using whatever user provides or script autodetects
    }
}
