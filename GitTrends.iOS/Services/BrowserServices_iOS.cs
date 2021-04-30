using System.Threading.Tasks;
using Foundation;
using GitTrends.iOS;
using WebKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(BrowserServices_iOS))]
namespace GitTrends.iOS
{
    public class BrowserServices_iOS : IBrowserServices
    {
        public Task TryClearBrowserHistory()
        {
            var clearBrowserHistoryTCS = new TaskCompletionSource<object?>();

            WKWebsiteDataStore.DefaultDataStore.RemoveDataOfTypes(WKWebsiteDataStore.AllWebsiteDataTypes, NSDate.FromTimeIntervalSince1970(0), () => clearBrowserHistoryTCS.SetResult(null));

            return clearBrowserHistoryTCS.Task;
        }
    }
}
