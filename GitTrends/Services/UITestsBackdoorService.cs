﻿#if !AppStore
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Autofac;
using GitTrends.Mobile.Common;
using GitTrends.Shared;
using Plugin.StoreReview.Abstractions;
using Syncfusion.SfChart.XForms;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace GitTrends
{
	public class UITestsBackdoorService
	{
		readonly static AsyncAwaitBestPractices.WeakEventManager _popPageStartedEventManager = new();
		readonly static WeakEventManager<Page> _popPageCompletedEventManager = new();

		readonly IMainThread _mainThread;
		readonly ThemeService _themeService;
		readonly LanguageService _languageService;
		readonly GitHubUserService _gitHubUserService;
		readonly NotificationService _notificationService;
		readonly GitHubGraphQLApiService _gitHubGraphQLApiService;
		readonly TrendsChartSettingsService _trendsChartSettingsService;
		readonly GitHubAuthenticationService _gitHubAuthenticationService;

		public UITestsBackdoorService(IMainThread mainThread,
										ThemeService themeService,
										LanguageService languageService,
										GitHubUserService gitHubUserService,
										NotificationService notificationService,
										GitHubGraphQLApiService gitHubGraphQLApiService,
										TrendsChartSettingsService trendsChartSettingsService,
										GitHubAuthenticationService gitHubAuthenticationService)
		{
			_mainThread = mainThread;
			_themeService = themeService;
			_languageService = languageService;
			_gitHubUserService = gitHubUserService;
			_notificationService = notificationService;
			_gitHubGraphQLApiService = gitHubGraphQLApiService;
			_trendsChartSettingsService = trendsChartSettingsService;
			_gitHubAuthenticationService = gitHubAuthenticationService;
		}

		public static event EventHandler PopPageStarted
		{
			add => _popPageStartedEventManager.AddEventHandler(value);
			remove => _popPageStartedEventManager.RemoveEventHandler(value);
		}

		public static event EventHandler<Page> PopPageCompleted
		{
			add => _popPageCompletedEventManager.AddEventHandler(value);
			remove => _popPageCompletedEventManager.RemoveEventHandler(value);
		}

		public string? GetPreferredLanguage() => _languageService.PreferredLanguage;

		public string GetLoggedInUserName() => _gitHubUserService.Name;
		public string GetLoggedInUserAlias() => _gitHubUserService.Alias;
		public string GetLoggedInUserAvatarUrl() => _gitHubUserService.AvatarUrl;

		public async Task SetGitHubUser(string token, CancellationToken cancellationToken)
		{
			await _gitHubUserService.SaveGitHubToken(new GitHubToken(token, string.Empty, "Bearer")).ConfigureAwait(false);

			var (alias, name, avatarUri) = await _gitHubGraphQLApiService.GetCurrentUserInfo(cancellationToken).ConfigureAwait(false);

			_gitHubUserService.Alias = alias;
			_gitHubUserService.Name = name;
			_gitHubUserService.AvatarUrl = avatarUri.ToString();
		}

		public Task<GitHubToken> GetGitHubToken() => _gitHubUserService.GetGitHubToken();

		public void TriggerReviewRequest() => ContainerService.Container.Resolve<IStoreReview>().RequestReview(true);

		public string GetReviewRequestAppStoreTitle() => AppStoreConstants.RatingRequest;

		public PreferredTheme GetPreferredTheme() => _themeService.Preference;

		public bool ShouldSendNotifications() => _notificationService.ShouldSendNotifications;

		public Task TriggerPullToRefresh() => _mainThread.InvokeOnMainThreadAsync(() => GetVisibleRefreshView().IsRefreshing = true);

		public IReadOnlyList<T> GetVisibleCollection<T>() => GetVisibleCollection().Cast<T>().ToList();

		public IReadOnlyList<NuGetPackageModel> GetVisibleLibraries()
		{
			var aboutPage = (AboutPage)GetVisibleContentPage();
			var aboutViewModel = (AboutViewModel)aboutPage.BindingContext;

			return aboutViewModel.InstalledLibraries;
		}

		public IReadOnlyList<Contributor> GetVisibleContributors()
		{
			var aboutPage = (AboutPage)GetVisibleContentPage();
			var aboutViewModel = (AboutViewModel)aboutPage.BindingContext;

			return aboutViewModel.GitTrendsContributors;
		}

		public async Task PopPage()
		{
			OnPopPageStarted();

			Page pagePopped;

			if (GetVisiblePageFromModalStack() is Page page)
				pagePopped = await page.Navigation.PopModalAsync();
			else
				pagePopped = await GetVisiblePageFromNavigationStack().Navigation.PopAsync();

			OnPopPageCompleted(pagePopped);
		}

		public IEnumerable GetVisibleCollection()
		{
			var collectionView = (CollectionView)GetVisibleRefreshView().Content;
			return collectionView.ItemsSource;
		}

		public TrendsChartOption GetCurrentTrendsChartOption() => _trendsChartSettingsService.CurrentTrendsChartOption;

		public bool IsViewsClonesChartSeriesVisible(string seriesTitle) => IsChartSeriesVisible<ViewsClonesTrendsPage>(seriesTitle);

		public bool IsStarsChartSeriesVisible(string seriesTitle) => IsChartSeriesVisible<StarsTrendsPage>(seriesTitle);

		public int GetCurrentOnboardingPageNumber()
		{
			var onboardingCarouselPage = (OnboardingCarouselPage)GetVisiblePage();
			var currentPage = onboardingCarouselPage.CurrentPage;

			return onboardingCarouselPage.Children.IndexOf(currentPage);
		}

		public int GetCurrentTrendsPageNumber()
		{
			var trendsCarouselPage = (TrendsCarouselPage)GetVisiblePage();
			var currentPage = trendsCarouselPage.CurrentPage;

			return trendsCarouselPage.Children.IndexOf(currentPage);
		}

		public Task<bool> AreNotificationsEnabled() => _notificationService.AreNotificationsEnabled();

		RefreshView GetVisibleRefreshView()
		{
			var visibleContentPage = GetVisibleContentPage();

			if (visibleContentPage.Content is RefreshView refreshView)
				return refreshView;
			else if (visibleContentPage.Content is Layout<View> layout && layout.Children.OfType<RefreshView>().FirstOrDefault() is RefreshView layoutRefreshView)
				return layoutRefreshView;
			else
				throw new Exception($"{visibleContentPage.GetType()} Does Not Contain a RefreshView");
		}

		bool IsChartSeriesVisible<T>(string seriesTitle) where T : BaseTrendsContentPage
		{
			var trendsCarouselPage = (TrendsCarouselPage)GetVisiblePage();
			var starsTrendsPage = trendsCarouselPage.Children.OfType<T>().First();

			var viewsClonesTrendsPageLayout = (Layout<View>)starsTrendsPage.Content;

			var trendsFrame = viewsClonesTrendsPageLayout.Children.OfType<ViewsClonesChart>().First();
			var trendsChart = (SfChart)trendsFrame.Content;

			return trendsChart.Series.First(x => x.Label.Equals(seriesTitle)).IsVisible;
		}

		ContentPage GetVisibleContentPage() => (ContentPage)GetVisiblePage();

		Page GetVisiblePage() => GetVisiblePageFromModalStack() ?? GetVisiblePageFromNavigationStack();

		Page? GetVisiblePageFromModalStack() => Application.Current.MainPage.Navigation.ModalStack.LastOrDefault();

		Page GetVisiblePageFromNavigationStack() => Application.Current.MainPage.Navigation.NavigationStack.Last();

		void OnPopPageStarted() => _popPageStartedEventManager.RaiseEvent(this, EventArgs.Empty, nameof(PopPageStarted));
		void OnPopPageCompleted(Page page) => _popPageCompletedEventManager.RaiseEvent(this, page, nameof(PopPageCompleted));
	}
}
#endif