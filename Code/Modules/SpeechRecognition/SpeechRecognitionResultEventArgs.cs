namespace UncleJupiter
{
    public class SpeechRecognitionResultEventArgs : EventArgs
    {
        public string Text { get; set; }
        public bool Finished { get; set; }

        public SpeechRecognitionResultEventArgs(string text, bool finished)
        {
            Text = text;
            Finished = finished;
        }
    }
}
