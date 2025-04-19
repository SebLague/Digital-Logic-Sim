using System;

namespace DLS.Description.Types
{
  public class ModDescription
  {
    public string ModName;
    public string Version;
    public string Author;
    public string Description;
    public string Licence;
    public bool Enabled;

    // ---- Helper functions ----
    public bool IsStarred(string chipName, bool isCollection)
    {
      return false;
    }
  }
}