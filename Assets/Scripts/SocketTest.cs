
using System;
using System.Collections;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

[System.Serializable]
public class OfferData 
{
    public string answerSocketId;
    public RTCSessionDescription offer;
    public string offerSocketId;
    public bool enableMediaStream;
    public bool enableDataChannel;
}

[System.Serializable]
public class AnswerData
{
    public string answerSocketId;
    public RTCSessionDescription answer;
    public string offerSocketId;
}

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


[System.Serializable]
public class CandidateData
{
    public string destSocketId;
    public string fromSocketId;
    public ReceivedIceCandidateInfo candidate;
}

public class SocketTest : MonoBehaviour
{
    public SocketIOUnity socket;
    RTCPeerConnection pc;
    public string currentDest;
    public string currentOrigin;
    public RawImage image;
    private MediaStream receiveStream;
    private DelegateOnTrack onTrack; 
    public string monitor = "";
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WebRTC.Update());
        onTrack = e => {
            if (e.Track is VideoStreamTrack video)
            {
                video.OnVideoReceived += tex =>
                {
                    image.texture = tex;
                };
            }
            // if (e.Track is AudioStreamTrack audioTrack)
            // {
            //     receiveAudio.SetTrack(audioTrack);
            //     receiveAudio.loop = true;
            //     receiveAudio.Play();
            // }
        };
        
        
        var uri = new Uri("http://localhost:5001");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                { "token", "UNITY" }
            },
            EIO = 4,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        ///// reserved socketio events
        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connected!");
            Debug.Log($"My socket id is ... \n{socket.Id}");
            currentOrigin = socket.Id;
        };
        
        // 일단 살려둠
        socket.OnPing += (sender, e) => { Debug.Log("Ping"); };
        socket.OnPong += (sender, e) => { Debug.Log("Pong: " + e.TotalMilliseconds); };
        socket.OnDisconnected += (sender, e) => { Debug.Log("disconnect: " + e); };
        socket.OnReconnectAttempt += (sender, e) => { Debug.Log($"{DateTime.Now} Reconnecting: attempt = {e}"); };

        static RTCConfiguration GetSelectedSdpSemantics() {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };
            return config;
        }
        
        var configuration = GetSelectedSdpSemantics();
        pc = new RTCPeerConnection(ref configuration) {
            OnIceCandidate = candidate =>
            {
                // When the ICECandidate of PC is created, send the information of the ICECandidate.
                CandidateData c = new CandidateData();
                c.destSocketId = currentDest;
                c.fromSocketId = currentOrigin;
                c.candidate = ReceivedIceCandidateInfo.Extract(candidate);
                socket.Emit("candidate", c);
            },
            OnTrack = e => {
                if (e.Track is VideoStreamTrack video)
                {
                    video.OnVideoReceived += tex =>
                    {
                        image.texture = tex;
                    };
                }
                // if (e.Track is AudioStreamTrack audioTrack)
                // {
                //     receiveAudio.SetTrack(audioTrack);
                //     receiveAudio.loop = true;
                //     receiveAudio.Play();
                // }
            } 
        };

        // var transceiver = pc.AddTransceiver(TrackKind.Video);
        // transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
        //
        
        socket.OnUnityThread("offer", (response) =>
        {
            var offerData = response.GetValue<OfferData>();
            currentDest = offerData.offerSocketId;
            StartCoroutine(CreateAnswer(pc, offerData.offer));
        });
       
        socket.OnUnityThread("candidate", (response) =>
        {
            RTCIceCandidate cand = response.GetValue<CandidateData>().candidate.GetRTCIceCandidate();
            if (pc != null)
            {
                pc.AddIceCandidate(cand); 
            }
        }); 

        // 현재 상황에서 쓰이지 않음
        socket.On("answer", (response) =>
        {
            var str = response.GetValue<string>();
        });
        
        // Connect to socket
        Debug.Log("Connecting...");
        socket.Connect();


    }

    IEnumerator CreateAnswer(RTCPeerConnection pc, RTCSessionDescription desc) {
        
        var op2 = pc.SetRemoteDescription(ref desc);
        yield return op2;
        if (op2.IsError) {
            var error = op2.Error;
            OnSetSessionDescriptionError(ref error);
        }
        
        var op3 = pc.CreateAnswer();
        yield return op3;
        if (!op3.IsError) {
            yield return OnCreateAnswerSuccess(pc, op3.Desc);
        }
    }

    static void OnSetSessionDescriptionError(ref RTCError error) {
        Debug.LogError($"Error Detail Type: {error.message}");
    }

    IEnumerator OnCreateAnswerSuccess(RTCPeerConnection pc, RTCSessionDescription desc) {
        var op1 = pc.SetLocalDescription(ref desc);
        yield return op1;
        if (op1.IsError) {
            var error = op1.Error;
            OnSetSessionDescriptionError(ref error);
        } 
        var answer = new AnswerData();
        answer.offerSocketId = currentDest;
        answer.answerSocketId = currentOrigin;
        answer.answer = desc;
        socket.Emit("answer", answer);
        
        yield return null;
    }

    void Update()
    {
        monitor = $"{pc.SignalingState}/{pc.ConnectionState}";
    }
}
