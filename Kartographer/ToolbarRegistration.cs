using UnityEngine;
using ToolbarControl_NS;

namespace Kartographer
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(AppLauncher.MODID, AppLauncher.MODNAME);
        }
    }
}