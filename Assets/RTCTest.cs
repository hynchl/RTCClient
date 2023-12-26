using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.WebRTC;

public class RTCTest : MonoBehaviour
{
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioSource outputAudioSource;
    private RTCPeerConnection _pc1, _pc2;
    private MediaStream _sendStream;
    private MediaStream _receiveStream;
    int m_samplingFrequency = 48000;
    int m_lengthSeconds = 1;
    private AudioStreamTrack m_audioTrack;
    

    private bool toggleEnableMicrophone;
    private List<RTCRtpCodecCapability> availableCodecs = new List<RTCRtpCodecCapability>();
    private Dictionary<string, int> dspBufferSizeOptions = new Dictionary<string, int>()
    {
        { "Best Latency",  256 },
        { "Good Latency", 512 },
        { "Best Performance", 1024 },
    };
    
    // Start is called before the first frame update
    void Start()
    {   
        // 뭐하는지 잘 모름...
        StartCoroutine(WebRTC.Update());
        StartCoroutine(LoopStatsCoroutine());
        
        // setting
        // DSP BufferSize
        // Codec
        // Bandwidth

        OnDSPBufferSizeChanged(dspBufferSizeOptions["Good Latency"]);

        var audioCodecs = new List<string> { "Default" };
        var codecs = RTCRtpSender.GetCapabilities(TrackKind.Audio).codecs;
        var excludeCodecTypes = new[] { "audio/CN", "audio/telephone-event" };
        foreach (var codec in codecs)
        {
            if (excludeCodecTypes.Count(type => codec.mimeType.Contains(type)) > 0)
                continue;
            availableCodecs.Add(codec);
        }
        // dropdownAudioCodecs.AddOptions(availableCodecs.Select(codec =>
        //     new Dropdown.OptionData(CodecToOptionName(codec))).ToList());
        // 할일 : 어디선가 코덱을 값을 넣어줘야함 .. oncall에서 뭔가 넣어주는 부분이 있음
        
        // option : 320k, 160k, 80k, 40k, 20k
        OnBandwidthChanged(320);
    }

    
    private AudioClip m_clipInput;
    [SerializeField] private AudioClip testAudioClip;
    private string m_micName;
    void OnStartRTC()
    {
        // 필요하면 밖에서 지정할 필요 있음
        m_micName = Microphone.devices[0];

        if (toggleEnableMicrophone)
        {
            m_clipInput = Microphone.Start(m_micName, true, m_lengthSeconds, m_samplingFrequency);
            while (!(Microphone.GetPosition(m_micName) > 0)) { }
        }
        else
        {
            m_clipInput = testAudioClip;
        }

        inputAudioSource.loop = true;
        inputAudioSource.clip = m_clipInput;
        inputAudioSource.Play();
    }
    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        return config;
    }


    void onCallRTC()
    {
        // 스트림을 만들어 놓고...
        _receiveStream = new MediaStream();
        _receiveStream.OnAddTrack += OnAddTrack;
        _sendStream = new MediaStream();

        // sdp를 만들고...
        var configuration = GetSelectedSdpSemantics();
        
        _pc1 = new RTCPeerConnection(ref configuration)
        {
            // negotiation 만들고...
            OnNegotiationNeeded = () => StartCoroutine(PeerNegotiationNeeded(_pc1)),
            
            // candidate이 생기면 pc2에다가 집어넣기
            OnIceCandidate = candidate => _pc2.AddIceCandidate(candidate)
        };

        _pc2 = new RTCPeerConnection(ref configuration)
        {
            // 이 부분이 예제 특정적인 건지 아닌지 모르겠음
            // 예제 특정적인 것 같긴함 (추측)
            OnIceCandidate = candidate => _pc1.AddIceCandidate(candidate),
            OnTrack = e => _receiveStream.AddTrack(e.Track),
        };

        // transceiver가 뭐하는건지 알아봐야함
        var transceiver2 = _pc2.AddTransceiver(TrackKind.Audio);
        transceiver2.Direction = RTCRtpTransceiverDirection.SendRecv;

        m_audioTrack = new AudioStreamTrack(inputAudioSource);
        m_audioTrack.Loopback = true;
        _pc1.AddTrack(m_audioTrack, _sendStream);


        //////////////////////////////////////////////////////////////////////////////////////////////////
        // codec 관련 처리
        var transceiver1 = _pc1.GetTransceivers().First();
        if (availableCodecs.Count == 0) // 
        {
            var error = transceiver1.SetCodecPreferences(this.availableCodecs.ToArray());
            if (error != RTCErrorType.None)
                Debug.LogError(error);
        }
        else
        {
            var codec = availableCodecs[0]; // 임시, 나중에 코덱 뭐가 있는지 찍어서 직접 선택할 것임
            var error = transceiver1.SetCodecPreferences(new[] { codec });
            if (error != RTCErrorType.None)
                Debug.LogError(error);
        }
    }

        void OnPause()
        {
            var transceiver1 = _pc1.GetTransceivers().First();
            var track = transceiver1.Sender.Track;
            track.Enabled = false;

            // buttonResume.gameObject.SetActive(true);
            // buttonPause.gameObject.SetActive(false);
        }

        void OnResume()
        {
            var transceiver1 = _pc1.GetTransceivers().First();
            var track = transceiver1.Sender.Track;
            track.Enabled = true;

            // buttonResume.gameObject.SetActive(false);
            // buttonPause.gameObject.SetActive(true);
        }

        void OnAddTrack(MediaStreamTrackEvent e)
        {
            var track = e.Track as AudioStreamTrack;
            outputAudioSource.SetTrack(track);
            outputAudioSource.loop = true;
            outputAudioSource.Play();

        }

        void OnHangUp()
        {
            Microphone.End(m_micName);
            m_clipInput = null;

            m_audioTrack?.Dispose();
            _receiveStream?.Dispose();
            _sendStream?.Dispose();
            _pc1?.Dispose();
            _pc2?.Dispose();
            _pc1 = null;
            _pc2 = null;

            inputAudioSource.Stop();
            outputAudioSource.Stop();
        }

    IEnumerator PeerNegotiationNeeded(RTCPeerConnection pc)
    {
        var op = pc.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (pc.SignalingState != RTCSignalingState.Stable)
            {
                yield break;
            }

            yield return StartCoroutine(OnCreateOfferSuccess(pc, op.Desc));
        }
        else
        {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

        private IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
        {
            var op = pc.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(pc);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }

            // 이 파트가 없어져야 할 것 같음
            var otherPc = GetOtherPc(pc);
            var op2 = otherPc.SetRemoteDescription(ref desc);
            yield return op2;
            if (op2.IsError)
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }

            var op3 = otherPc.CreateAnswer();
            yield return op3;
            if (!op3.IsError)
            {
                yield return OnCreateAnswerSuccess(otherPc, op3.Desc);
            }
        }

        IEnumerator OnCreateAnswerSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
        {
            var op = pc.SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError) {
                OnSetLocalSuccess(pc);
            }
            else {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }

            var otherPc = GetOtherPc(pc);
            var op2 = otherPc.SetRemoteDescription(ref desc);
            yield return op2;
            if (op2.IsError) {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }

        private RTCPeerConnection GetOtherPc(RTCPeerConnection pc)
        {
            return (pc == _pc1) ? _pc2 : _pc1;
        }


        private void OnSetLocalSuccess(RTCPeerConnection pc)
        {
            Debug.Log("SetLocalDescription complete");
        }

        static void OnSetSessionDescriptionError(ref RTCError error)
        {
            Debug.LogError($"Error Detail Type: {error.message}");
        }

    

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator LoopStatsCoroutine()
        {
            while (true)
            {
                yield return StartCoroutine(UpdateStatsCoroutine());
                yield return new WaitForSeconds(1f);
            }
        }

    private IEnumerator UpdateStatsCoroutine()
        {
            RTCRtpSender sender = _pc1?.GetSenders().First();
            if (sender == null)
                yield break;
            RTCStatsReportAsyncOperation op = sender.GetStats();
            yield return op;
            if (op.IsError)
            {
                Debug.LogErrorFormat("RTCRtpSender.GetStats() is failed {0}", op.Error.errorType);
            }
            else
            {
                UpdateStatsPacketSize(op.Value);
            }
        }

        void OnDSPBufferSizeChanged(int value)
        {
            var audioConf = AudioSettings.GetConfiguration();
            audioConf.dspBufferSize = dspBufferSizeOptions.Values.ToArray()[value];
            if (!AudioSettings.Reset(audioConf))
            {
                Debug.LogError("Failed changing Audio Settings");
            }
        }
        private RTCStatsReport lastResult = null;
        private void UpdateStatsPacketSize(RTCStatsReport res)
        {
            foreach (RTCStats stats in res.Stats.Values)
            {
                if (!(stats is RTCOutboundRTPStreamStats report))
                {
                    continue;
                }

                long now = report.Timestamp;
                ulong bytes = report.bytesSent;

                if (lastResult != null)
                {
                    if (!lastResult.TryGetValue(report.Id, out RTCStats last))
                        continue;

                    var lastStats = last as RTCOutboundRTPStreamStats;
                    var duration = (double)(now - lastStats.Timestamp) / 1000000;
                    ulong bitrate = (ulong)(8 * (bytes - lastStats.bytesSent) / duration);
                    // textBandwidth.text = (bitrate / 1000.0f).ToString("f2");
                    //if (autoScroll.isOn)
                    //{
                    //    statsField.MoveTextEnd(false);
                    //}
                }

            }
            lastResult = res;
        }

        private void OnBandwidthChanged(ulong bandwidth)
        {
            if (_pc1 == null || _pc2 == null)
                return;

            RTCRtpSender sender = _pc1.GetSenders().First();
            RTCRtpSendParameters parameters = sender.GetParameters();

            parameters.encodings[0].maxBitrate = bandwidth * 1000;
            parameters.encodings[0].minBitrate = bandwidth * 1000;

            RTCError error = sender.SetParameters(parameters);
            if (error.errorType != RTCErrorType.None)
            {
                Debug.LogErrorFormat("RTCRtpSender.SetParameters failed {0}", error.errorType);
            }

            Debug.Log("SetParameters:" + bandwidth);
        }
        
}
