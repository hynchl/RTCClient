namespace RTC
{
    [System.Serializable]
    public class CandidateData
    {
        public string destSocketId;
        public string fromSocketId;
        public ReceivedIceCandidateInfo candidate;
    }
}