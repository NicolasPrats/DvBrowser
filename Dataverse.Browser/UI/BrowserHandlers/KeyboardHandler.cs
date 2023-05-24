using CefSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dataverse.UI.BrowserHandlers
{
    public class KeyboardHandler : IKeyboardHandler
    {
        private const int VK_F5 = 0x74;
        private const int VK_F12 = 0x7B;

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            return false;
        }

        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            switch (windowsKeyCode)
            {
                case VK_F5:
                    browser.Reload();
                    break;
                case VK_F12:
                    //TODO when executed step by step, it's working nice
                    // when executed normaly devtools are opened 2 times
                    browser.ShowDevTools();
                    break;
                default:
                    return true;
            }
            return false;
        }
    }
}
