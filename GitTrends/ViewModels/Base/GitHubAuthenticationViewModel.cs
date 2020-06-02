using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using GitTrends.Mobile.Shared;
using GitTrends.Shared;
using Xamarin.Essentials.Interfaces;

namespace GitTrends
{
    public abstract class GitHubAuthenticationViewModel : BaseViewModel
    {
        bool _isAuthenticating = false;

        protected GitHubAuthenticationViewModel(GitHubAuthenticationService gitHubAuthenticationService,
                                                    DeepLinkingService deepLinkingService,
                                                    IAnalyticsService analyticsService,
                                                    IMainThread mainThread,
                                                    GitHubUserService gitHubUserService) : base(analyticsService, mainThread)
        {
            gitHubAuthenticationService.AuthorizeSessionStarted += HandleAuthorizeSessionStarted;
            gitHubAuthenticationService.AuthorizeSessionCompleted += HandleAuthorizeSessionCompleted;

            ConnectToGitHubButtonCommand = new AsyncCommand<(CancellationToken CancellationToken, Xamarin.Essentials.BrowserLaunchOptions? BrowserLaunchOptions)>(tuple => ExecuteConnectToGitHubButtonCommand(gitHubAuthenticationService, deepLinkingService, gitHubUserService, tuple.CancellationToken, tuple.BrowserLaunchOptions), _ => IsNotAuthenticating);
            DemoButtonCommand = new AsyncCommand<string>(text => ExecuteDemoButtonCommand(text), _ => IsNotAuthenticating);

            GitHubAuthenticationService = gitHubAuthenticationService;
            GitHubUserService = gitHubUserService;
        }

        public IAsyncCommand<(CancellationToken CancellationToken, Xamarin.Essentials.BrowserLaunchOptions? BrowserLaunchOptions)> ConnectToGitHubButtonCommand { get; }
        public IAsyncCommand<string> DemoButtonCommand { get; }

        public bool IsNotAuthenticating => !IsAuthenticating;

        public virtual bool IsDemoButtonVisible => !IsAuthenticating && GitHubUserService.Alias != DemoDataConstants.Alias;

        protected GitHubAuthenticationService GitHubAuthenticationService { get; }
        protected GitHubUserService GitHubUserService { get; }

        public bool IsAuthenticating
        {
            get => _isAuthenticating;
            set => SetProperty(ref _isAuthenticating, value, () =>
            {
                NotifyIsAuthenticatingPropertyChanged();
                MainThread.InvokeOnMainThreadAsync(ConnectToGitHubButtonCommand.RaiseCanExecuteChanged).SafeFireAndForget(ex => Debug.WriteLine(ex));
            });
        }

        protected virtual void NotifyIsAuthenticatingPropertyChanged()
        {
            OnPropertyChanged(nameof(IsNotAuthenticating));
            OnPropertyChanged(nameof(IsDemoButtonVisible));
        }

        protected virtual Task ExecuteDemoButtonCommand(string buttonText)
        {
            IsAuthenticating = true;
            return Task.CompletedTask;
        }

        protected async virtual Task ExecuteConnectToGitHubButtonCommand(GitHubAuthenticationService gitHubAuthenticationService, DeepLinkingService deepLinkingService, GitHubUserService gitHubUserService, CancellationToken cancellationToken, Xamarin.Essentials.BrowserLaunchOptions? browserLaunchOptions = null)
        {
            IsAuthenticating = true;

            var fiveMinuteTimeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            try
            {
                var loginUrl = await gitHubAuthenticationService.GetGitHubLoginUrl(cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(loginUrl))
                {
                    var result = await deepLinkingService.LaunchWebAuthenticator(new Uri(loginUrl), new Uri(CallbackConstants.CallbackUrl)).ConfigureAwait(false);
                    await gitHubAuthenticationService.AuthorizeSession(result.Properties["code"], result.Properties["state"], fiveMinuteTimeoutCancellationToken.Token);
                }
                else
                {
                    await displayConnectionErrorAlert().ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                //Only Show Error Message only if IsCancellationRequested
                //On Android, TaskCanceledException is thrown by Xamarin.Essentials if the user clicks 'X' on the browser to return to the app
                if (fiveMinuteTimeoutCancellationToken.IsCancellationRequested)
                    await deepLinkingService.DisplayAlert("Login Timeout", "GitHub Login took longer than five minutes", "OK").ConfigureAwait(false);
                else if (cancellationToken.IsCancellationRequested)
                    await displayConnectionErrorAlert().ConfigureAwait(false);
            }
            catch (Exception e) when (e.GetType().FullName.Contains("NSErrorException"))
            {
                //On iOS, Foundation.NSErrorException is thrown by Xamarin.Essentials if the user clicks 'X' on the browser to return to the app
            }
            catch (Exception e)
            {
                AnalyticsService.Report(e);
                await displayConnectionErrorAlert().ConfigureAwait(false);
            }
            finally
            {
                IsAuthenticating = false;
            }

            Task displayConnectionErrorAlert() => deepLinkingService.DisplayAlert("Error", "Couldn't connect to GitHub Login. Check your internet connection and try again", "OK");
        }

        void HandleAuthorizeSessionStarted(object sender, EventArgs e) => IsAuthenticating = true;
        void HandleAuthorizeSessionCompleted(object sender, AuthorizeSessionCompletedEventArgs e) => IsAuthenticating = false;
    }
}
