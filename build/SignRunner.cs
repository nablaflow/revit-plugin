using Build.Options;
using Microsoft.Extensions.Logging;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.FileSystem;
using ModularPipelines.Options;
using Shouldly;
using File = ModularPipelines.FileSystem.File;

namespace Build;

/// <summary>
///     Invokes the <c>sign</c> dotnet tool against Azure Artifact Signing.
/// </summary>
/// <seealso href="https://github.com/dotnet/sign">dotnet/sign</seealso>
internal static class SignRunner
{
    /// <summary>
    ///     Verify that everything needed to sign has been configured.
    /// </summary>
    public static void ValidateOptions(SignOptions options)
    {
        options.Endpoint.ShouldNotBeNullOrWhiteSpace("Signing is enabled but Sign__Endpoint was not set");
        options.Account.ShouldNotBeNullOrWhiteSpace("Signing is enabled but Sign__Account was not set");
        options.CertificateProfile.ShouldNotBeNullOrWhiteSpace("Signing is enabled but Sign__CertificateProfile was not set");
    }

    /// <summary>
    ///     Install the <c>sign</c> tool into a throwaway folder and return the path to its executable.
    /// </summary>
    /// <remarks>
    ///     Installed per module rather than globally, mirroring how the WiX toolset is provisioned in
    ///     <see cref="Modules.CreateInstallerModule" />, so the build never mutates the machine's global tools.
    /// </remarks>
    public static async Task<File> InstallAsync(IModuleContext context, SignOptions options, CancellationToken cancellationToken)
    {
        var toolFolder = Folder.CreateTemporaryFolder();

        string[] versionArguments = string.IsNullOrWhiteSpace(options.ToolVersion)
            ? []
            : ["--version", options.ToolVersion];

        await context.DotNet().Tool.Execute(new DotNetToolOptions
        {
            Arguments = ["install", "sign", "--prerelease", "--tool-path", toolFolder.Path, ..versionArguments]
        }, cancellationToken: cancellationToken);

        var signExecutable = toolFolder.GetFile(OperatingSystem.IsWindows() ? "sign.exe" : "sign");
        signExecutable.Exists.ShouldBeTrue($"The sign tool was not installed at: {signExecutable.Path}");

        return signExecutable;
    }

    /// <summary>
    ///     Sign the given files, one invocation per file so a failure names the artifact that caused it.
    /// </summary>
    public static async Task SignAsync(
        IModuleContext context,
        File signExecutable,
        SignOptions options,
        IEnumerable<File> files,
        CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            context.Logger.LogInformation("Signing {File}", file.Path);

            await context.Shell.Command.ExecuteCommandLineTool(
                new GenericCommandLineToolOptions(signExecutable.Path)
                {
                    Arguments =
                    [
                        "code", "artifact-signing",
                        "--azure-credential-type", options.CredentialType,
                        "--artifact-signing-endpoint", options.Endpoint!,
                        "--artifact-signing-account", options.Account!,
                        "--artifact-signing-certificate-profile", options.CertificateProfile!,
                        "--timestamp-url", options.TimestampUrl,
                        "--verbosity", "information",
                        file.Path
                    ]
                }, cancellationToken: cancellationToken);
        }
    }
}
