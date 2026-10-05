#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Console;
#endif

namespace SLWardrobe.Common
{
    // LabApi.Features.Console.Logger.Debug fires unconditionally regardless of any config -
    // Config.Debug ("Enable debug logging") was a no-op on LabAPI builds until every Debug
    // call routed through this gate. Info/Warn/Error are unaffected and still called directly.
    public static class GatedLogger
    {
        public static void Debug(string message)
        {
            if (SLWardrobe.Instance == null || !SLWardrobe.Instance.Config.Debug) return;
#if EXILED
            Log.Debug(message);
#else
            Logger.Debug(message);
#endif
        }
    }
}
