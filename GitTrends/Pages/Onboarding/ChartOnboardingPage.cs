using GitTrends.Mobile.Common.Constants;
using GitTrends.Shared;
using MediaManager.Forms;
using MediaManager.Playback;
using MediaManager.Video;
using Xamarin.CommunityToolkit.Markup;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.PancakeView;
using static GitTrends.MarkupExtensions;
using static Xamarin.CommunityToolkit.Markup.GridRowsColumns;

namespace GitTrends
{
    public class ChartOnboardingPage : BaseOnboardingContentPage
    {

        public ChartOnboardingPage(IMainThread mainThread,
                                    IAnalyticsService analyticsService,
                                    MediaElementService mediaElementService)
            : base(Color.FromHex(BaseTheme.CoralColorHex), OnboardingConstants.SkipText, mainThread, 1, analyticsService, mediaElementService)
        {

        }

        enum Row { Title, Zoom, LongPress }
        enum Column { Image, Description }

        protected override View CreateImageView() => new PancakeView
        {
            CornerRadius = 4,
            Border = new Border { Color = Color.FromHex("E0E0E0") },
            BackgroundColor = Color.White,
            Padding = new Thickness(5),

#if AppStore
#error iOS: VideoView flashes when looping & video is blurry (not buffered before playing)
#endif
            Content = new VideoView
            {
                AutoPlay = true,
                Repeat = RepeatMode.All,
                BackgroundColor = Color.Transparent,
                VideoAspect = VideoAspectMode.AspectFit,
                Source = MediaElementService.OnboardingChart?.HlsUrl,
            }
        };

        protected override TitleLabel CreateDescriptionTitleLabel() => new(OnboardingConstants.ChartPage_Title);

        protected override View CreateDescriptionBodyView() => new ScrollView
        {
            Content = new Grid
            {
                RowSpacing = 14,

                RowDefinitions = Rows.Define(
                    (Row.Title, Auto),
                    (Row.Zoom, AbsoluteGridLength(48)),
                    (Row.LongPress, AbsoluteGridLength(48))),

                ColumnDefinitions = Columns.Define(
                    (Column.Image, AbsoluteGridLength(56)),
                    (Column.Description, Star)),

                Children =
                {
                    new BodyLabel(OnboardingConstants.ChartPage_Body_ShowAllTraffic).Row(Row.Title).ColumnSpan(All<Column>()),

                    new BodySvg("zoom_gesture.svg").Row(Row.Zoom).Column(Column.Image),
                    new BodyLabel(OnboardingConstants.ChartPage_Body_ZoomInOut).Row(Row.Zoom).Column(Column.Description),

                    new BodySvg("longpress_gesture.svg").Row(Row.LongPress).Column(Column.Image),
                    new BodyLabel(OnboardingConstants.ChartPage_Body_LongPress).Row(Row.LongPress).Column(Column.Description),
                }
            }
        };
    }
}
