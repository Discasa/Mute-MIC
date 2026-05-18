using NAudio.CoreAudioApi;

namespace MuteMIC.App;

internal sealed record AudioInputDeviceState(string Id, string Name, bool IsMuted);

internal sealed class AudioInputMuteService
{
    public IReadOnlyList<AudioInputDeviceState> GetActiveInputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(device => new AudioInputDeviceState(
                device.ID,
                string.IsNullOrWhiteSpace(device.FriendlyName) ? "Unknown input" : device.FriendlyName,
                device.AudioEndpointVolume.Mute))
            .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> SetAllMuted(bool muted)
    {
        using var enumerator = new MMDeviceEnumerator();
        List<string> errors = [];

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            try
            {
                device.AudioEndpointVolume.Mute = muted;
            }
            catch (Exception ex)
            {
                errors.Add($"{device.FriendlyName}: {ex.Message}");
            }
        }

        return errors;
    }

    public IReadOnlyList<string> ToggleAll()
    {
        IReadOnlyList<AudioInputDeviceState> devices = GetActiveInputDevices();
        bool shouldMute = devices.Any(device => !device.IsMuted);
        return SetAllMuted(shouldMute);
    }
}
