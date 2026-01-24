using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RecoTrack.Infrastructure.ServicesV2;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RecoTrack.ServiceTests
{
    public class AuthServiceTests
    {
        private const string JwtSecretKey = "abcdefghijklmnopqrstuvwxyz123456";

        private readonly IConfiguration _configuration;

        public AuthServiceTests()
        {
            var settings = new Dictionary<string, string>
            {
                ["JwtSettings:SecretKey"] = JwtSecretKey,
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience"
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private AuthService CreateSubject(
            Mock<IUserRepository> userRepository,
            Mock<IPasswordResetRepository> passwordResetRepository,
            Mock<IEmailService> emailService,
            Mock<ILogger<AuthService>> logger)
        {
            return new AuthService(
                _configuration,
                userRepository.Object,
                passwordResetRepository.Object,
                emailService.Object,
                logger.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendPasswordResetOtpAsync_Throws_WhenEmailMissing(string email)
        {
            var userRepo = new Mock<IUserRepository>();
            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SendPasswordResetOtpAsync(email));
        }

        [Fact]
        public async Task SendPasswordResetOtpAsync_Throws_WhenUserNotFound()
        {
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendPasswordResetOtpAsync("missing@example.com"));
        }

        [Fact]
        public async Task SendPasswordResetOtpAsync_GeneratesOtpAndSendsEmail_WhenUserExists()
        {
            const string email = "test@example.com";

            var user = new User
            {
                Id = "userId",
                Username = "Test User",
                Email = email,
                PasswordHash = "hash"
            };

            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);

            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            PasswordResetEntry? savedEntry = null;
            passwordResetRepo
                .Setup(x => x.SaveAsync(It.IsAny<PasswordResetEntry>()))
                .Returns<PasswordResetEntry>(entry =>
                {
                    savedEntry = entry;
                    return Task.CompletedTask;
                });
            passwordResetRepo
                .Setup(x => x.DeactivateActiveOtpsAsync(email))
                .Returns(Task.CompletedTask);

            var emailService = new Mock<IEmailService>();
            emailService
                .Setup(x => x.SendOtpEmailAsync(email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            var result = await service.SendPasswordResetOtpAsync(email);

            Assert.NotNull(savedEntry);
            Assert.Equal(email, savedEntry!.Email);
            Assert.Equal(result.Otp, savedEntry.Otp);
            Assert.Equal(1, savedEntry.Active);
            Assert.Equal(result.Otp.Length, 6);
            Assert.Equal("OTP generated and sent to email", result.Message);
            Assert.True(result.ExpiresAtUtc > savedEntry.CreatedAtUtc);

            passwordResetRepo.Verify(x => x.DeactivateActiveOtpsAsync(email), Times.Once);
            passwordResetRepo.Verify(x => x.SaveAsync(It.Is<PasswordResetEntry>(entry => entry.Email == email)), Times.Once);
            emailService.Verify(x => x.SendOtpEmailAsync(email, result.Otp, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("", "123456")]
        [InlineData("test@example.com", "")]
        public async Task VerifyPasswordResetOtpAsync_Throws_WhenEmailOrOtpMissing(string email, string otp)
        {
            var userRepo = new Mock<IUserRepository>();
            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyPasswordResetOtpAsync(email, otp));
        }

        [Fact]
        public async Task VerifyPasswordResetOtpAsync_ReturnsFailure_WhenEntryMissing()
        {
            const string email = "test@example.com";
            const string otp = "123456";

            var userRepo = new Mock<IUserRepository>();
            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            passwordResetRepo.Setup(x => x.GetActiveUnexpiredEntryAsync(email, otp))
                .ReturnsAsync((PasswordResetEntry?)null);

            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            var result = await service.VerifyPasswordResetOtpAsync(email, otp);

            Assert.False(result.Success);
            Assert.Equal("Invalid or inactive OTP.", result.Message);
            passwordResetRepo.Verify(x => x.SetSuccessCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPasswordResetOtpAsync_ReturnsFailure_WhenEntryExpired()
        {
            const string email = "test@example.com";
            const string otp = "123456";

            var entry = new PasswordResetEntry
            {
                Email = email,
                Otp = otp,
                Active = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
            };

            var userRepo = new Mock<IUserRepository>();
            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            passwordResetRepo.Setup(x => x.GetActiveUnexpiredEntryAsync(email, otp)).ReturnsAsync(entry);

            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            var result = await service.VerifyPasswordResetOtpAsync(email, otp);

            Assert.False(result.Success);
            Assert.Equal("OTP has expired.", result.Message);
            passwordResetRepo.Verify(x => x.SetSuccessCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPasswordResetOtpAsync_ReturnsSuccess_WhenEntryValid()
        {
            const string email = "test@example.com";
            const string otp = "123456";

            var entry = new PasswordResetEntry
            {
                Email = email,
                Otp = otp,
                Active = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            };

            var userRepo = new Mock<IUserRepository>();
            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            passwordResetRepo.Setup(x => x.GetActiveUnexpiredEntryAsync(email, otp)).ReturnsAsync(entry);
            passwordResetRepo
                .Setup(x => x.SetSuccessCodeAsync(email, otp, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            var result = await service.VerifyPasswordResetOtpAsync(email, otp);

            Assert.True(result.Success);
            Assert.Equal("OTP verified successfully.", result.Message);
            Assert.False(string.IsNullOrWhiteSpace(result.SuccessCode));
            passwordResetRepo.Verify(x => x.SetSuccessCodeAsync(email, otp, It.Is<string>(code => !string.IsNullOrWhiteSpace(code))), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsFailure_WhenSuccessCodeInvalid()
        {
            const string email = "test@example.com";

            var userRepo = new Mock<IUserRepository>();
            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            passwordResetRepo.Setup(x => x.GetBySuccessCodeAsync(email, It.IsAny<string>()))
                .ReturnsAsync((PasswordResetEntry?)null);

            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            var request = new ResetPasswordRequest
            {
                Email = email,
                SuccessCode = "success-code",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };

            var result = await service.ResetPasswordAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Invalid or expired success code.", result.ErrorMessage);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsSuccess_WhenEntryValid()
        {
            const string email = "test@example.com";
            const string successCode = "success-code";

            var entry = new PasswordResetEntry
            {
                Email = email,
                SuccessCode = successCode,
                Active = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            };

            var user = new User
            {
                Id = "userId",
                Username = "TestUser",
                Email = email,
                PasswordHash = "old-hash"
            };

            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);
            userRepo.Setup(x => x.UpdatePasswordHashAsync(email, It.IsAny<string>())).Returns(Task.CompletedTask);

            var passwordResetRepo = new Mock<IPasswordResetRepository>();
            passwordResetRepo.Setup(x => x.GetBySuccessCodeAsync(email, successCode)).ReturnsAsync(entry);
            passwordResetRepo.Setup(x => x.DeactivateActiveOtpsAsync(email)).Returns(Task.CompletedTask);

            var emailService = new Mock<IEmailService>();
            var logger = new Mock<ILogger<AuthService>>();
            var service = CreateSubject(userRepo, passwordResetRepo, emailService, logger);

            var request = new ResetPasswordRequest
            {
                Email = email,
                SuccessCode = successCode,
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };

            var result = await service.ResetPasswordAsync(request);

            Assert.True(result.Success);
            Assert.Equal(user.Username, result.Username);
            passwordResetRepo.Verify(x => x.DeactivateActiveOtpsAsync(email), Times.Once);
            userRepo.Verify(x => x.UpdatePasswordHashAsync(email, It.IsAny<string>()), Times.Once);
        }
    }
}
