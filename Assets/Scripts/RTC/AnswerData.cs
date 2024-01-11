using Unity.WebRTC;

namespace RTC
{
    [System.Serializable]
    public class AnswerData
    {
        public string answerSocketId;
        public RTCSessionDescription answer;
        public string offerSocketId;
    }
}