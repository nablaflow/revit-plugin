using Build.Modules;
using Build.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines;
using ModularPipelines.Extensions;

var builder = Pipeline.CreateBuilder();

builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true);
builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOptions<BuildOptions>().Bind(builder.Configuration.GetSection("Build"));
builder.Services.AddOptions<BundleOptions>().Bind(builder.Configuration.GetSection("Bundle"));
builder.Services.AddOptions<PublishOptions>().Bind(builder.Configuration.GetSection("Publish"));
builder.Services.AddOptions<SignOptions>().Bind(builder.Configuration.GetSection("Sign"));

if (args.Length == 0)
{
    builder.Services.AddModule<CompileProjectModule>();
}

if (args.Contains("pack"))
{
    builder.Services.AddModule<CleanProjectModule>();
    builder.Services.AddModule<SignAssembliesModule>();
    builder.Services.AddModule<CreateBundleModule>();
    builder.Services.AddModule<CreateInstallerModule>();
    builder.Services.AddModule<SignInstallersModule>();
}

if (args.Contains("publish"))
{
    builder.Services.AddModule<PublishGithubModule>();
}

await builder.Build().RunAsync();