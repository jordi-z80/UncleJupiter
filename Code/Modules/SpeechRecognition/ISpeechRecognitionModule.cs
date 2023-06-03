using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncleJupiter
{

    internal interface ISpeechRecognitionModule
    {
        event EventHandler<SpeechRecognitionResultEventArgs> SpeechRecognitionResult;
    }
}
