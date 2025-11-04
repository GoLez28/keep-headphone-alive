#!/usr/bin/env python3
import pulsectl
import sounddevice as sd
import numpy as np
import time

# === CONFIGURATION ===
TARGET_SINK = "alsa_output.usb-XiiSound_Technology_Corporation_H510-WL_Zeus_X_Wireless_headset-00.analog-stereo"
MONITOR_SOURCE = "Audifonos Zeus X (H510-WL)"
CHECK_INTERVAL = 1       # seconds between checks
CHECK_INTERVAL_EX = 20       # seconds between checks if no selected
SILENCE_TIME = 240       # 5 minutes
BEEP_FREQ = 150         # Hz
BEEP_DURATION = 0.15      # seconds
RMS_THRESHOLD = 0.0008   # minimum level to consider "active audio"

pulse = pulsectl.Pulse('headphone-keeper')

def get_device_index(name):
    """Find a sounddevice device index by matching part of its name."""
    devices = sd.query_devices()
    for idx, dev in enumerate(devices):
        if name in dev['name']:
            return idx
    return None

def is_target_sink_selected():
    default_sink = pulse.server_info().default_sink_name
    return TARGET_SINK in default_sink

def is_audio_playing(monitor_index):
    try:
        duration = 0.08
        fs = 48000
        audio = sd.rec(int(duration * fs), samplerate=fs, channels=1,
                       dtype='float32', device=monitor_index)
        sd.wait()
        rms = float(np.sqrt(np.mean(np.square(audio))))
        return rms > RMS_THRESHOLD
    except Exception as e:
        print("Audio check failed:", e)
        return False

def beep():
    fs = 44100
    t = np.linspace(0, BEEP_DURATION, int(fs * BEEP_DURATION), endpoint=False)
    wave = 0.3 * np.sin(2 * np.pi * BEEP_FREQ * t)
    sd.play(wave, fs)
    sd.wait()

monitor_index = get_device_index(MONITOR_SOURCE)
if monitor_index is None:
    print(f"Could not find monitor source: {MONITOR_SOURCE}")
    exit(1)

print(f"Using monitor source index: {monitor_index}")
last_sound_time = time.time()

while True:
    if is_target_sink_selected():
        if is_audio_playing(monitor_index):
            last_sound_time = time.time()
        elif time.time() - last_sound_time > SILENCE_TIME:
            print("Silent - beeping")
            beep()
            last_sound_time = time.time()
    else:
        time.sleep(CHECK_INTERVAL_EX)
    time.sleep(CHECK_INTERVAL)
