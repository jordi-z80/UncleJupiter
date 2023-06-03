using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncleJupiter;

public interface IInputAudioModule
{
    int SampleRate { get; }
    bool InputAvailable { get; }
    event EventHandler<AudioInputAvailableEventArgs> AudioInput;

}
