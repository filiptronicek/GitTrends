﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitTrends.Shared;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GitTrends.UnitTests
{
    class GitTrendsStatisticsServiceTests : BaseTest
    {
        [Test]
        public async Task InitializeContributorsTest()
        {
            //Arrange
            IReadOnlyList<Contributor> contributors_Initial, contributors_Final;
            long? watchers_Initial, watchers_Final, stars_Initial, stars_Final, forks_Initial, forks_Final;

            var gitTrendsStatisticsService = ServiceCollection.ServiceProvider.GetRequiredService<GitTrendsStatisticsService>();

            //Act
            stars_Initial = gitTrendsStatisticsService.Stars;
            forks_Initial = gitTrendsStatisticsService.Forks;
            watchers_Initial = gitTrendsStatisticsService.Watchers;
            contributors_Initial = gitTrendsStatisticsService.Contributors;

            await gitTrendsStatisticsService.Initialize(CancellationToken.None).ConfigureAwait(false);

            stars_Final = gitTrendsStatisticsService.Stars;
            forks_Final = gitTrendsStatisticsService.Forks;
            watchers_Final = gitTrendsStatisticsService.Watchers;
            contributors_Final = gitTrendsStatisticsService.Contributors;

            //Assert
            Assert.IsNull(stars_Initial);
            Assert.IsNull(forks_Initial);
            Assert.IsNull(watchers_Initial);

            Assert.IsNotNull(contributors_Initial);
            Assert.IsEmpty(contributors_Initial);

            Assert.IsNotNull(stars_Final);
            Assert.IsNotNull(forks_Final);
            Assert.IsNotNull(watchers_Final);

            Assert.Less(0, stars_Final);
            Assert.Less(0, forks_Final);
            Assert.Less(0, watchers_Final);

            Assert.IsNotEmpty(contributors_Final);

            foreach (var contributor in contributors_Final)
            {
                var isAvatarUrlValid = Uri.TryCreate(contributor.AvatarUrl.ToString(), UriKind.Absolute, out _);
                var isGitHubUrlValid = Uri.TryCreate(contributor.GitHubUrl.ToString(), UriKind.Absolute, out _);

                Assert.IsTrue(isAvatarUrlValid);
                Assert.IsTrue(isGitHubUrlValid);
                Assert.Greater(contributor.ContributionCount, 0);
                Assert.IsFalse(string.IsNullOrEmpty(contributor.Login));
            }
        }
    }
}
