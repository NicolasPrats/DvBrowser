using CefSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dataverse.Browser.Misc
{
    public class KeyboardHandler : IKeyboardHandler
    {
        private const int VK_F5 = 0x74;

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            return false;
        }

        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            if (windowsKeyCode == VK_F5 && modifiers == CefEventFlags.ControlDown)
            {
                browser.Reload(true);
                return true;
            }

            if (windowsKeyCode == VK_F5)
            {
                browser.Reload();
                return true;
            }

            return true;
        }
    }
}
