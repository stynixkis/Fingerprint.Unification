using System;
using System.Collections.Generic;

namespace FingerprintForAdministrator.Models;

public partial class AudioFile
{
    public int IdAudio { get; set; }

    public byte[] FftPrint { get; set; } = null!;

    public byte[] MfccPrint { get; set; } = null!;

    public string TitleAudio { get; set; } = null!;
}
