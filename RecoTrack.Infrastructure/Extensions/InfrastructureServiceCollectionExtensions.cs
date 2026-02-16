using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RecoTrack.Infrastructure.Extensions
{
 public static class InfrastructureServiceCollectionExtensions
 {
 public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
 {
 // Previously registered email template strategies and IEmailService here.
 // Email sending has been refactored: a single Brevo-backed EmailService is registered in the API project.

 return services;
 }
 }
}
