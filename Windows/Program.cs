using System;
using System.Threading;
using NAudio.CoreAudioApi;
using System.Media;
using System.Diagnostics;

class Program {
    static void Main(string[] args) {
        Debug.WriteLine("Headphone Keeper Running");

        //get default playback device
        var deviceEnumerator = new MMDeviceEnumerator();
        device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        count = 0;
        Timer timer = new Timer(CheckAndBeep, deviceEnumerator, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        //keep app alive
        Thread.Sleep(Timeout.Infinite);
    }
    static int count = 0;
    static int deviceCount = 0;
    static bool targetDevice = true;
    static MMDevice device;

    private static void CheckAndBeep(object state) {
        if (deviceCount >= 10) {
            deviceCount = 0;
            var deviceEnumerator = (MMDeviceEnumerator)state;
            device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            string deviceName = device.FriendlyName.ToLower();
            if (!deviceName.Equals("audifonos (inalambrico) (h510-wl zeus x wireless headset)")) {
                Debug.WriteLine($"[{DateTime.Now}] Active device = {device.FriendlyName} → Skipping beep");
                count = 0;
                targetDevice = false;
                return;
            } else
                targetDevice = true;
        }
        deviceCount++;

        //skip if not headphones
        if (!targetDevice) return;

        float peak = device.AudioMeterInformation.MasterPeakValue;

        if (peak < 0.001f) {//silent
            count++;
            Debug.WriteLine($"[{DateTime.Now}] Silent → Counting: {count}");
        } else {
            Debug.WriteLine($"[{DateTime.Now}] Audio activity detected → No beep");
            count = 0;
        }

        if (count >= 300) {
            count = 0;
            Beep(device);
        }
    }
    private static void Beep(MMDevice device) {
        Debug.WriteLine($"[{DateTime.Now}] Silent → Sending low-volume beep");
        float originalVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
        try {
            //lower volume (independent if volume is less than 5%)
            device.AudioEndpointVolume.MasterVolumeLevelScalar = 0.05f;

            Console.Beep(100, 100); //100Hz, 100ms
        } finally {
            //restore original volume
            device.AudioEndpointVolume.MasterVolumeLevelScalar = originalVolume;
        }
    }
}