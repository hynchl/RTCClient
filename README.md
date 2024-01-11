# RTC Client for Unity

This project is the modified version of Unity RTC Client from [Unity's RTC samples](https://docs.unity3d.com/Packages/com.unity.webrtc@2.4/manual/sample.html) simplifying the signaling process by direct access to opponent information. In real-world applications, direct access to opponent information for connection setup is typically not available.

This implementation includes samples for Audio, Video, and Data Channels. Unlike Unity's original samples, the signaling process occurs through a socket server even when the clients are in the same scene. To execute this example, you'll require a Node.js-based socket server, available at [WebRTCSignalingServer](https://github.com/hynchl/WebRTCSignalingServer)
