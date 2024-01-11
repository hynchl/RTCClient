using Unity.WebRTC;

namespace RTC
{
    [System.Serializable]
    public class ReceivedIceCandidateInfo
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public string usernameFragment;
    
        public RTCIceCandidate GetRTCIceCandidate()
        {
            RTCIceCandidateInit info = new RTCIceCandidateInit();
            info.candidate = candidate;
            info.sdpMid = sdpMid;
            info.sdpMLineIndex = sdpMLineIndex;
            return new RTCIceCandidate(info);
        }

        public static ReceivedIceCandidateInfo Extract (RTCIceCandidate rtcIceCandidate)
        {
            ReceivedIceCandidateInfo info = new ReceivedIceCandidateInfo();
            info.candidate = rtcIceCandidate.Candidate;
            info.sdpMid = rtcIceCandidate.SdpMid;
            info.sdpMLineIndex = rtcIceCandidate.SdpMLineIndex.HasValue ? rtcIceCandidate.SdpMLineIndex.Value : -1;
            info.usernameFragment = rtcIceCandidate.UserNameFragment;
            return info;
        }
    }
}