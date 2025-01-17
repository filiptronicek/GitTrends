﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using GitHubApiStatus;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using GitTrends.Shared;
using Refit;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace GitTrends
{
	public class TrendsViewModel : BaseViewModel
	{
		public const int MinimumChartHeight = 20;

		readonly static WeakEventManager<Repository> _repostoryEventManager = new();

		readonly GitHubApiV3Service _gitHubApiV3Service;
		readonly RepositoryDatabase _repositoryDatabase;
		readonly GitHubApiStatusService _gitHubApiStatusService;
		readonly BackgroundFetchService _backgroundFetchService;
		readonly GitHubGraphQLApiService _gitHubGraphQLApiService;

		bool _isFetchingData = true;
		bool _isViewsSeriesVisible, _isUniqueViewsSeriesVisible, _isClonesSeriesVisible, _isUniqueClonesSeriesVisible;

		string _starsStatisticsText = string.Empty;
		string _viewsStatisticsText = string.Empty;
		string _clonesStatisticsText = string.Empty;
		string _starsHeaderMessageText = string.Empty;
		string _uniqueViewsStatisticsText = string.Empty;
		string _uniqueClonesStatisticsText = string.Empty;
		string _starsEmptyDataViewTitleText = string.Empty;
		string _starsEmptyDataViewDescriptionText = string.Empty;
		string _viewsClonesEmptyDataViewTitleText = string.Empty;

		ImageSource? __starsEmptyDataViewImage;
		ImageSource? _viewsClonesEmptyDataViewImage;

		IReadOnlyList<DailyStarsModel>? _dailyStarsList;
		IReadOnlyList<DailyViewsModel>? _dailyViewsList;
		IReadOnlyList<DailyClonesModel>? _dailyClonesList;

		public TrendsViewModel(IMainThread mainThread,
								IAnalyticsService analyticsService,
								RepositoryDatabase repositoryDatabse,
								GitHubApiV3Service gitHubApiV3Service,
								GitHubApiStatusService gitHubApiStatusService,
								BackgroundFetchService backgroundFetchService,
								GitHubGraphQLApiService gitHubGraphQLApiService,
								TrendsChartSettingsService trendsChartSettingsService) : base(analyticsService, mainThread)
		{
			_gitHubApiV3Service = gitHubApiV3Service;
			_repositoryDatabase = repositoryDatabse;
			_gitHubApiStatusService = gitHubApiStatusService;
			_backgroundFetchService = backgroundFetchService;
			_gitHubGraphQLApiService = gitHubGraphQLApiService;

			IsViewsSeriesVisible = trendsChartSettingsService.ShouldShowViewsByDefault;
			IsUniqueViewsSeriesVisible = trendsChartSettingsService.ShouldShowUniqueViewsByDefault;
			IsClonesSeriesVisible = trendsChartSettingsService.ShouldShowClonesByDefault;
			IsUniqueClonesSeriesVisible = trendsChartSettingsService.ShouldShowUniqueClonesByDefault;

			ViewsCardTappedCommand = new Command(() => IsViewsSeriesVisible = !IsViewsSeriesVisible);
			UniqueViewsCardTappedCommand = new Command(() => IsUniqueViewsSeriesVisible = !IsUniqueViewsSeriesVisible);
			ClonesCardTappedCommand = new Command(() => IsClonesSeriesVisible = !IsClonesSeriesVisible);
			UniqueClonesCardTappedCommand = new Command(() => IsUniqueClonesSeriesVisible = !IsUniqueClonesSeriesVisible);

			RefreshState = RefreshState.Uninitialized;

			FetchDataCommand = new AsyncCommand<(Repository Repository, CancellationToken CancellationToken)>(tuple => ExecuteFetchDataCommand(tuple.Repository, tuple.CancellationToken));
		}

		public ICommand ViewsCardTappedCommand { get; }
		public ICommand UniqueViewsCardTappedCommand { get; }
		public ICommand ClonesCardTappedCommand { get; }
		public ICommand UniqueClonesCardTappedCommand { get; }

		public static event EventHandler<Repository> RepositorySavedToDatabase
		{
			add => _repostoryEventManager.AddEventHandler(value);
			remove => _repostoryEventManager.RemoveEventHandler(value);
		}

		public IAsyncCommand<(Repository Repository, CancellationToken CancellationToken)> FetchDataCommand { get; }

		public double DailyViewsClonesMinValue { get; } = 0;
		public double MinDailyStarsValue { get; } = 0;

		public double TotalStars => DailyStarsList.Any() ? DailyStarsList.Last().TotalStars : 0;

		public bool IsStarsEmptyDataViewVisible => !IsStarsChartVisible && !IsFetchingData;
		public bool IsStarsChartVisible => !IsFetchingData && TotalStars > 1;

		public bool IsViewsClonesEmptyDataViewVisible => !IsViewsClonesChartVisible && !IsFetchingData;
		public bool IsViewsClonesChartVisible => !IsFetchingData && DailyViewsList.Sum(x => x.TotalViews + x.TotalUniqueViews) + DailyClonesList.Sum(x => x.TotalClones + x.TotalUniqueClones) > 0;

		public double ViewsClonesChartYAxisInterval => DailyViewsClonesMaxValue > 20 ? Math.Round(DailyViewsClonesMaxValue / 10) : 2;
		public double StarsChartYAxisInterval => MaxDailyStarsValue > 20 ? Math.Round(MaxDailyStarsValue / 10) : 2;

		public DateTime MinViewsClonesDate => DateTimeService.GetMinimumLocalDateTime(DailyViewsList, DailyClonesList);
		public DateTime MaxViewsClonesDate => DateTimeService.GetMaximumLocalDateTime(DailyViewsList, DailyClonesList);

		public DateTime MaxDailyStarsDate => DailyStarsList.Any() ? DailyStarsList.Last().LocalDay : DateTime.Today;
		public DateTime MinDailyStarsDate => DailyStarsList.Any() ? DailyStarsList.First().LocalDay : DateTime.Today.Subtract(TimeSpan.FromDays(14));

		public double MaxDailyStarsValue => TotalStars > MinimumChartHeight ? TotalStars : MinimumChartHeight;

		public double DailyViewsClonesMaxValue
		{
			get
			{
				var dailyViewMaxValue = DailyViewsList.Any() ? DailyViewsList.Max(x => x.TotalViews) : 0;
				var dailyClonesMaxValue = DailyClonesList.Any() ? DailyClonesList.Max(x => x.TotalClones) : 0;

				return Math.Max(Math.Max(dailyViewMaxValue, dailyClonesMaxValue), MinimumChartHeight);
			}
		}

		public ImageSource? ViewsClonesEmptyDataViewImage
		{
			get => _viewsClonesEmptyDataViewImage;
			set => SetProperty(ref _viewsClonesEmptyDataViewImage, value);
		}

		public ImageSource? StarsEmptyDataViewImage
		{
			get => __starsEmptyDataViewImage;
			set => SetProperty(ref __starsEmptyDataViewImage, value);
		}

		public string ViewsClonesEmptyDataViewTitleText
		{
			get => _viewsClonesEmptyDataViewTitleText;
			set => SetProperty(ref _viewsClonesEmptyDataViewTitleText, value);
		}

		public string StarsEmptyDataViewTitleText
		{
			get => _starsEmptyDataViewTitleText;
			set => SetProperty(ref _starsEmptyDataViewTitleText, value);
		}

		public string StarsEmptyDataViewDescriptionText
		{
			get => _starsEmptyDataViewDescriptionText;
			set => SetProperty(ref _starsEmptyDataViewDescriptionText, value);
		}

		public string StarsStatisticsText
		{
			get => _starsStatisticsText;
			set => SetProperty(ref _starsStatisticsText, value);
		}

		public string ViewsStatisticsText
		{
			get => _viewsStatisticsText;
			set => SetProperty(ref _viewsStatisticsText, value);
		}

		public string UniqueViewsStatisticsText
		{
			get => _uniqueViewsStatisticsText;
			set => SetProperty(ref _uniqueViewsStatisticsText, value);
		}

		public string ClonesStatisticsText
		{
			get => _clonesStatisticsText;
			set => SetProperty(ref _clonesStatisticsText, value);
		}

		public string UniqueClonesStatisticsText
		{
			get => _uniqueClonesStatisticsText;
			set => SetProperty(ref _uniqueClonesStatisticsText, value);
		}

		public bool IsViewsSeriesVisible
		{
			get => _isViewsSeriesVisible;
			set => SetProperty(ref _isViewsSeriesVisible, value);
		}

		public bool IsUniqueViewsSeriesVisible
		{
			get => _isUniqueViewsSeriesVisible;
			set => SetProperty(ref _isUniqueViewsSeriesVisible, value);
		}

		public bool IsClonesSeriesVisible
		{
			get => _isClonesSeriesVisible;
			set => SetProperty(ref _isClonesSeriesVisible, value);
		}

		public bool IsUniqueClonesSeriesVisible
		{
			get => _isUniqueClonesSeriesVisible;
			set => SetProperty(ref _isUniqueClonesSeriesVisible, value);
		}

		public string StarsHeaderMessageText
		{
			get => _starsHeaderMessageText;
			set => SetProperty(ref _starsHeaderMessageText, value);
		}

		public bool IsFetchingData
		{
			get => _isFetchingData;
			set => SetProperty(ref _isFetchingData, value, OnIsFetchingDataChanged);
		}

		public IReadOnlyList<DailyViewsModel> DailyViewsList
		{
			get => _dailyViewsList ??= Array.Empty<DailyViewsModel>();
			set => SetProperty(ref _dailyViewsList, value, OnDailyViewsListChanged);
		}

		public IReadOnlyList<DailyClonesModel> DailyClonesList
		{
			get => _dailyClonesList ??= Array.Empty<DailyClonesModel>();
			set => SetProperty(ref _dailyClonesList, value, OnDailyClonesListChanged);
		}

		public IReadOnlyList<DailyStarsModel> DailyStarsList
		{
			get => _dailyStarsList ??= Array.Empty<DailyStarsModel>();
			set => SetProperty(ref _dailyStarsList, value, OnDailyStarsListChanged);
		}

		RefreshState RefreshState
		{
			set
			{
				ViewsClonesEmptyDataViewImage = EmptyDataViewService.GetViewsClonesImage(value);
				ViewsClonesEmptyDataViewTitleText = EmptyDataViewService.GetViewsClonesTitleText(value);

				StarsEmptyDataViewImage = EmptyDataViewService.GetStarsImage(value, TotalStars);
				StarsEmptyDataViewTitleText = EmptyDataViewService.GetStarsTitleText(value, TotalStars);
				StarsEmptyDataViewDescriptionText = EmptyDataViewService.GetStarsEmptyDataViewDescriptionText(value, TotalStars);

				StarsHeaderMessageText = TotalStars switch
				{
					0 or 1 => TrendsChartTitleConstants.YouGotThis,
					> 1 => TrendsChartTitleConstants.KeepItUp,
					_ => throw new NotSupportedException($"{nameof(TotalStars)} cannot be negative")
				};
			}
		}

		async Task ExecuteFetchDataCommand(Repository repository, CancellationToken cancellationToken)
		{
			var refreshState = RefreshState.Uninitialized;

			IReadOnlyList<DateTimeOffset> repositoryStars = Array.Empty<DateTimeOffset>();
			IReadOnlyList<DailyViewsModel> repositoryViews = Array.Empty<DailyViewsModel>();
			IReadOnlyList<DailyClonesModel> repositoryClones = Array.Empty<DailyClonesModel>();

			var minimumTimeTask = Task.Delay(TimeSpan.FromSeconds(1));

			try
			{
				if (repository.ContainsTrafficData
					&& repository.DataDownloadedAt > DateTimeOffset.Now.AddDays(-1))
				{
					repositoryStars = repository.StarredAt ?? throw new InvalidOperationException($"{nameof(Repository.StarredAt)} cannot be null when {nameof(Repository.ContainsTrafficData)} is true");
					repositoryViews = repository.DailyViewsList ?? throw new InvalidOperationException($"{nameof(Repository.DailyViewsList)} cannot be null when {nameof(Repository.ContainsTrafficData)} is true");
					repositoryClones = repository.DailyClonesList ?? throw new InvalidOperationException($"{nameof(Repository.DailyClonesList)} cannot be null when {nameof(Repository.ContainsTrafficData)} is true");
				}
				else
				{
					IsFetchingData = true;

					var getStarGazersTask = _gitHubGraphQLApiService.GetStarGazers(repository.Name, repository.OwnerLogin, cancellationToken);
					var getRepositoryViewStatisticsTask = _gitHubApiV3Service.GetRepositoryViewStatistics(repository.OwnerLogin, repository.Name, cancellationToken);
					var getRepositoryCloneStatisticsTask = _gitHubApiV3Service.GetRepositoryCloneStatistics(repository.OwnerLogin, repository.Name, cancellationToken);

					await Task.WhenAll(getRepositoryViewStatisticsTask, getRepositoryCloneStatisticsTask, getStarGazersTask).ConfigureAwait(false);

					var starGazersResponse = await getStarGazersTask.ConfigureAwait(false);
					var repositoryViewsResponse = await getRepositoryViewStatisticsTask.ConfigureAwait(false);
					var repositoryClonesResponse = await getRepositoryCloneStatisticsTask.ConfigureAwait(false);

					repositoryStars = starGazersResponse.StarredAt.Select(x => x.StarredAt).ToList();
					repositoryViews = repositoryViewsResponse.DailyViewsList;
					repositoryClones = repositoryClonesResponse.DailyClonesList;

					var updatedRepository = repository with
					{
						DataDownloadedAt = DateTimeOffset.UtcNow,
						StarredAt = repositoryStars,
						DailyViewsList = repositoryViews,
						DailyClonesList = repositoryClones
					};

					await _repositoryDatabase.SaveRepository(updatedRepository).ConfigureAwait(false);
					OnRepositorySavedToDatabase(updatedRepository);
				}

				refreshState = RefreshState.Succeeded;
			}
			catch (Exception e) when (e is ApiException { StatusCode: HttpStatusCode.Unauthorized })
			{
				var repositoryData = await GetNewestRepsitoryData(repository).ConfigureAwait(false);

				repositoryStars = repositoryData.RepositoryStars;
				repositoryViews = repositoryData.RepositoryViews;
				repositoryClones = repositoryData.RepositoryClones;

				refreshState = RefreshState.LoginExpired;
			}
			catch (Exception e) when (_gitHubApiStatusService.HasReachedMaximumApiCallLimit(e))
			{
				var repositoryData = await GetNewestRepsitoryData(repository).ConfigureAwait(false);

				repositoryStars = repositoryData.RepositoryStars;
				repositoryViews = repositoryData.RepositoryViews;
				repositoryClones = repositoryData.RepositoryClones;

				refreshState = RefreshState.MaximumApiLimit;
			}
			catch (Exception e) when (_gitHubApiStatusService.IsAbuseRateLimit(e, out var retryTimeSpan))
			{
				_backgroundFetchService.TryScheduleRetryRepositoriesViewsClones(repository, retryTimeSpan.Value);

				var repositoryFromDatabase = await _repositoryDatabase.GetRepository(repository.Url).ConfigureAwait(false);

				if (repositoryFromDatabase is null)
				{
					repositoryStars = Array.Empty<DateTimeOffset>();
					repositoryViews = Array.Empty<DailyViewsModel>();
					repositoryClones = Array.Empty<DailyClonesModel>();

					refreshState = RefreshState.Error;
				}
				else if (repositoryFromDatabase.DataDownloadedAt > repository.DataDownloadedAt) //If data from database is more recent, display data from database
				{
					repositoryStars = repositoryFromDatabase.StarredAt ?? Array.Empty<DateTimeOffset>();
					repositoryViews = repositoryFromDatabase.DailyViewsList ?? Array.Empty<DailyViewsModel>();
					repositoryClones = repositoryFromDatabase.DailyClonesList ?? Array.Empty<DailyClonesModel>();

					refreshState = RefreshState.Succeeded;
				}
				else //If data passed in as parameter is more recent, display data passed in as parameter
				{
					repositoryStars = repository.StarredAt ?? Array.Empty<DateTimeOffset>();
					repositoryViews = repository.DailyViewsList ?? Array.Empty<DailyViewsModel>();
					repositoryClones = repository.DailyClonesList ?? Array.Empty<DailyClonesModel>();

					refreshState = RefreshState.Succeeded;
				}
			}
			catch (Exception e)
			{
				AnalyticsService.Report(e);

				var repositoryData = await GetNewestRepsitoryData(repository).ConfigureAwait(false);

				repositoryStars = repositoryData.RepositoryStars;
				repositoryViews = repositoryData.RepositoryViews;
				repositoryClones = repositoryData.RepositoryClones;

				refreshState = RefreshState.Error;
			}
			finally
			{
				DailyStarsList = GetDailyStarsList(repositoryStars).OrderBy(x => x.Day).ToList();
				DailyViewsList = repositoryViews.OrderBy(x => x.Day).ToList();
				DailyClonesList = repositoryClones.OrderBy(x => x.Day).ToList();

				StarsStatisticsText = repositoryStars.Count.ToAbbreviatedText();

				ViewsStatisticsText = repositoryViews.Sum(x => x.TotalViews).ToAbbreviatedText();
				UniqueViewsStatisticsText = repositoryViews.Sum(x => x.TotalUniqueViews).ToAbbreviatedText();

				ClonesStatisticsText = repositoryClones.Sum(x => x.TotalClones).ToAbbreviatedText();
				UniqueClonesStatisticsText = repositoryClones.Sum(x => x.TotalUniqueClones).ToAbbreviatedText();

				//Set RefreshState last, because EmptyDataViews are dependent on the Chart ItemSources, e.g. DailyStarsList
				RefreshState = refreshState;

				//Display the Activity Indicator for a minimum time to ensure consistant UX
				await minimumTimeTask.ConfigureAwait(false);
				IsFetchingData = false;
			}

			PrintDays();
		}

		async Task<(IReadOnlyList<DateTimeOffset> RepositoryStars, IReadOnlyList<DailyViewsModel> RepositoryViews, IReadOnlyList<DailyClonesModel> RepositoryClones)>
			GetNewestRepsitoryData(Repository repository)
		{
			IReadOnlyList<DateTimeOffset> repositoryStars = Array.Empty<DateTimeOffset>();
			IReadOnlyList<DailyViewsModel> repositoryViews = Array.Empty<DailyViewsModel>();
			IReadOnlyList<DailyClonesModel> repositoryClones = Array.Empty<DailyClonesModel>();

			var repositoryFromDatabase = await _repositoryDatabase.GetRepository(repository.Url).ConfigureAwait(false);

			if (repositoryFromDatabase is null)
			{
				repositoryStars = Array.Empty<DateTimeOffset>();
				repositoryViews = Array.Empty<DailyViewsModel>();
				repositoryClones = Array.Empty<DailyClonesModel>();
			}
			else if (repositoryFromDatabase.DataDownloadedAt > repository.DataDownloadedAt) //If data from database is more recent, display data from database
			{
				repositoryStars = repositoryFromDatabase.StarredAt ?? Array.Empty<DateTimeOffset>();
				repositoryViews = repositoryFromDatabase.DailyViewsList ?? Array.Empty<DailyViewsModel>();
				repositoryClones = repositoryFromDatabase.DailyClonesList ?? Array.Empty<DailyClonesModel>();
			}
			else //If data passed in as parameter is more recent, display data passed in as parameter
			{
				repositoryStars = repository.StarredAt ?? Array.Empty<DateTimeOffset>();
				repositoryViews = repository.DailyViewsList ?? Array.Empty<DailyViewsModel>();
				repositoryClones = repository.DailyClonesList ?? Array.Empty<DailyClonesModel>();
			}

			return (repositoryStars, repositoryViews, repositoryClones);
		}


		IEnumerable<DailyStarsModel> GetDailyStarsList(IReadOnlyList<DateTimeOffset> starredAtDates)
		{
			int totalStars = 0;

			foreach (var starDate in starredAtDates)
				yield return new DailyStarsModel(++totalStars, starDate);

			//Ensure chart includes todays date
			if (starredAtDates.Any() && starredAtDates.Max().DayOfYear != DateTimeOffset.UtcNow.DayOfYear)
				yield return new DailyStarsModel(totalStars, DateTimeOffset.UtcNow);
		}

		void OnDailyStarsListChanged()
		{
			OnPropertyChanged(nameof(IsStarsChartVisible));
			OnPropertyChanged(nameof(IsStarsEmptyDataViewVisible));

			OnPropertyChanged(nameof(MaxDailyStarsValue));
			OnPropertyChanged(nameof(MinDailyStarsValue));

			OnPropertyChanged(nameof(MaxDailyStarsDate));
			OnPropertyChanged(nameof(MinDailyStarsDate));

			OnPropertyChanged(nameof(TotalStars));

			OnPropertyChanged(nameof(StarsChartYAxisInterval));
		}

		void OnDailyClonesListChanged()
		{
			OnPropertyChanged(nameof(IsViewsClonesChartVisible));
			OnPropertyChanged(nameof(IsViewsClonesEmptyDataViewVisible));

			OnPropertyChanged(nameof(DailyViewsClonesMaxValue));
			OnPropertyChanged(nameof(DailyViewsClonesMinValue));

			OnPropertyChanged(nameof(MinViewsClonesDate));
			OnPropertyChanged(nameof(MaxViewsClonesDate));

			OnPropertyChanged(nameof(ViewsClonesChartYAxisInterval));
		}

		void OnDailyViewsListChanged()
		{
			OnPropertyChanged(nameof(IsViewsClonesChartVisible));
			OnPropertyChanged(nameof(IsViewsClonesEmptyDataViewVisible));

			OnPropertyChanged(nameof(DailyViewsClonesMaxValue));
			OnPropertyChanged(nameof(DailyViewsClonesMaxValue));

			OnPropertyChanged(nameof(MinViewsClonesDate));
			OnPropertyChanged(nameof(MaxViewsClonesDate));

			OnPropertyChanged(nameof(ViewsClonesChartYAxisInterval));
		}

		void OnIsFetchingDataChanged()
		{
			OnPropertyChanged(nameof(IsStarsChartVisible));
			OnPropertyChanged(nameof(IsStarsEmptyDataViewVisible));

			OnPropertyChanged(nameof(IsViewsClonesChartVisible));
			OnPropertyChanged(nameof(IsViewsClonesEmptyDataViewVisible));
		}

		void OnRepositorySavedToDatabase(in Repository repository) => _repostoryEventManager.RaiseEvent(this, repository, nameof(RepositorySavedToDatabase));

		[Conditional("DEBUG")]
		void PrintDays()
		{
			Debug.WriteLine("Clones");
			foreach (var cloneDay in DailyClonesList.Select(x => x.Day))
				Debug.WriteLine(cloneDay);

			Debug.WriteLine("");

			Debug.WriteLine("Views");
			foreach (var viewDay in DailyViewsList.Select(x => x.Day))
				Debug.WriteLine(viewDay);
		}
	}
}