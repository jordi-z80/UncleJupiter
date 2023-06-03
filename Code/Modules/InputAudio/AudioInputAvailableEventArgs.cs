namespace UncleJupiter;

public class AudioInputAvailableEventArgs : EventArgs
{
    public byte[] AudioData { get; private set; }
    public int AudioDataLength { get; private set; }        // the length of the real data may be less than the length of the array

    public AudioInputAvailableEventArgs(byte[] audioData, int audioLength)
    {
        AudioData = audioData;
        AudioDataLength = audioLength;
    }
}
