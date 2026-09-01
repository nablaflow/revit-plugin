using Build.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using Shouldly;

namespace Build.Modules;

/// <summary>
///     Sign the generated .msi installers with Azure Artifact Signing.
/// </summary>
/// <remarks>
///     Must run after the installers are built: signing an MSI rewrites it, so it cannot be done before WiX
///     produces the file. The .bundle archive is not signable and is covered by <see cref="SignAssembliesModule" />.
/// </remarks>
[DependsOn<CreateInstallerModule>]
public sealed class SignInstallersModule(IOptions<BuildOptions> buildOptions, IOptions<SignOptions> signOptions) : Module
{
    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var options = signOptions.Value;
        if (!options.Enabled)
        {
            context.Logger.LogInformation("Signing is disabled, skipping. Set Sign__Enabled to enable it");
            return;
        }

        SignRunner.ValidateOptions(options);

        var outputFolder = context.Git().RootDirectory.GetFolder(buildOptions.Value.OutputDirectory);
        var installers = outputFolder.GetFiles(file => file.Extension == ".msi").ToArray();

        installers.ShouldNotBeEmpty("No installers were found to sign");

        var signExecutable = await SignRunner.InstallAsync(context, options, cancellationToken);
        await SignRunner.SignAsync(context, signExecutable, options, installers, cancellationToken);

        context.Summary.KeyValue("Signing", "Installers", installers.Length.ToString());
    }
}
