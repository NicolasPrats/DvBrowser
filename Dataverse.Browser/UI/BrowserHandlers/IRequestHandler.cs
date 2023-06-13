using CefSharp;

namespace Dataverse.Browser.Requests
{
    internal interface IRequestHandler
    {
        IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling);
    }
}
