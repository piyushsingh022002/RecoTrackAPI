using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using RecoTrack.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace RecoTrack.ServiceTests
{
    public class AutomatedPrReviewServiceTests
    {
        private readonly AutomatedPrReviewService _service;

        public AutomatedPrReviewServiceTests()
        {
            var httpClient = new HttpClient();
            var configurationMock = new Mock<IConfiguration>();
            var githubClientMock = new Mock<IGitHubClientService>();
            _service = new AutomatedPrReviewService(httpClient, configurationMock.Object, githubClientMock.Object);
        }

        [Fact]
        public async Task AnalyzePullRequestAsync_ShouldHandleEmptyMetadataGracefully()
        {
            // Arrange
            var metadata = new PrMetadata();

            // Act
            var result = await _service.AnalyzePullRequestAsync(metadata);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Analyzed PR", result.Summary);
        }


        [Fact]
        public async Task AnalyzePullRequestAsync_ShouldApprove_WhenNoIssuesFound()
        {
            // Arrange
            var metadata = new PrMetadata
            {
                Title = "Add clean feature",
                Description = "Implements feature cleanly with no temporary files.",
                BranchName = "feature/clean",
                Author = "TestUser",
                ChangedFiles = new() { "Controllers/TestController.cs" }
            };

            // Act
            var result = await _service.AnalyzePullRequestAsync(metadata);

            // Assert
            Assert.True(result.Approved);
            Assert.Empty(result.Suggestions);
        }

        [Fact]
        public async Task AnalyzePullRequestAsync_ShouldReject_WhenTodoFilesPresent()
        {
            // Arrange
            var metadata = new PrMetadata
            {
                Title = "Add feature with TODOs",
                Description = "Implements feature but has TODO files.",
                BranchName = "feature/todo",
                Author = "TestUser",
                ChangedFiles = new() { "temp/TODO_Helper.cs" }
            };

            // Act
            var result = await _service.AnalyzePullRequestAsync(metadata);

            // Assert
            Assert.False(result.Approved);
            Assert.Contains("Avoid committing temporary or TODO files.", result.Suggestions);
        }

        [Fact]
        public async Task AnalyzePullRequestAsync_ShouldSuggest_WhenDescriptionTooShort()
        {
            // Arrange
            var metadata = new PrMetadata
            {
                Title = "Short desc",
                Description = "Too short.",
                BranchName = "feature/short",
                Author = "TestUser",
                ChangedFiles = new() { "Controllers/ShortController.cs" }
            };

            // Act
            var result = await _service.AnalyzePullRequestAsync(metadata);

            // Assert
            Assert.Contains("Provide a more detailed PR description.", result.Suggestions);
        }
    }
}
