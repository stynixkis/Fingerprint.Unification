using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audiofingerprint.Interfaces
{
    public interface IFingerprintService
    {
        string GenerateFingerprint(string path, string filePathForSave);
        double CompareFingerprints(string firstPath, string secondPath);

    }
}
