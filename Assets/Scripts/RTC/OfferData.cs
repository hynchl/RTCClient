using Unity.WebRTC;

namespace RTC
{
    [System.Serializable]
    public class OfferData 
    {
        public string answerSocketId;
        public RTCSessionDescription offer;
        public string offerSocketId;
        public bool enableMediaStream;
        public bool enableDataChannel;
    }
}