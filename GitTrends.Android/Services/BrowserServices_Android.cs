using System.Threading.Tasks;
using Android.Provider;
using GitTrends.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(BrowserServices_Android))]
namespace GitTrends.Droid
{
    public class BrowserServices_Android : IBrowserServices
    {
        public Task TryClearBrowserHistory()
        {
            var currentActivity = Xamarin.Essentials.Platform.CurrentActivity;

            if (Browser.CanClearHistory(currentActivity.ContentResolver))
                Browser.ClearHistory(currentActivity.ContentResolver);

            return Task.CompletedTask;
        }
    }
}
