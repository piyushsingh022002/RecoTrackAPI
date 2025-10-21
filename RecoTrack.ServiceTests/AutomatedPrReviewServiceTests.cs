using RecoTrack.Application.Models;
using RecoTrack.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.ServiceTests
{
    public class AutomatedPrReviewServiceTests
    {
        private readonly AutomatedPrReviewService _service = new();

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
