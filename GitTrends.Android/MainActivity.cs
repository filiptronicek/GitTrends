using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Autofac;
using GitTrends.Shared;
using Newtonsoft.Json;
using Shiny;
using Xamarin.Forms;

namespace GitTrends.Droid
{
    [Activity(Label = "GitTrends", Icon = "@mipmap/icon", RoundIcon = "@mipmap/icon_round", Theme = "@style/LaunchTheme", LaunchMode = LaunchMode.SingleTop, MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.SetTheme(Resource.Style.MainTheme);
            base.OnCreate(savedInstanceState);

            Forms.Init(this, savedInstanceState);

            using var scope = ContainerService.Container.BeginLifetimeScope();
            var notificationService = scope.Resolve<INotificationService>();
            var analyticsService = scope.Resolve<IAnalyticsService>();
            var themeService = scope.Resolve<ThemeService>();
            var splashScreenPage = scope.Resolve<SplashScreenPage>();

            LoadApplication(new App(analyticsService, notificationService, themeService, splashScreenPage));

            TryHandleOpenedFromNotification(Intent);
        }

        protected override void OnResume()
        {
            base.OnResume();

            Xamarin.Essentials.Platform.OnResume();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            TryHandleOpenedFromNotification(intent);
        }

        async void TryHandleOpenedFromNotification(Intent? intent)
        {
            try
            {
                if (intent?.GetStringExtra("ShinyNotification") is string notificationString)
                {
                    var notification = JsonConvert.DeserializeObject<Shiny.Notifications.Notification>(notificationString);

                    using var scope = ContainerService.Container.BeginLifetimeScope();
                    var analyticsService = scope.Resolve<IAnalyticsService>();

                    var notificationService = scope.Resolve<NotificationService>();

                    if (notification.Title is string notificationTitle
                        && notification.Message is string notificationMessage
                        && notification.BadgeCount is int badgeCount
                        && badgeCount > 0)
                    {
                        await notificationService.HandleNotification(notificationTitle, notificationMessage, badgeCount).ConfigureAwait(false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {

            }
        }
    }

    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataSchemes = new[] { CallbackConstants.Scheme })]
    public class WebAuthenticationCallbackActivity : Xamarin.Essentials.WebAuthenticatorCallbackActivity
    {
    }
}