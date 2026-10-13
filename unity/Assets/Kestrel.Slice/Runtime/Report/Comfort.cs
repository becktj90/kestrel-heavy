// Same two PlayerPrefs the hangar already honours. Reduce Motion or Reduce Flashing
// both kill flash and shake on the failure visual and snap the report open.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class Comfort
    {
        public static bool ReduceMotion =>
            PlayerPrefs.GetInt(HangarCamera.ReduceMotionPref, 0) != 0
            || PlayerPrefs.GetInt(HangarCamera.ReduceFlashingPref, 0) != 0;
    }
}
