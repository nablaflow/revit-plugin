using Build.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using Shouldly;
using Sourcy.DotNet;
using File = ModularPipelines.FileSystem.File;

namespace Build.Modules;

/// <summary>
///     Sign the compiled add-in assemblies with Azure Artifact Signing.
/// </summary>
/// <remarks>
///     Runs between compilation and packaging so the signed assemblies are the ones that end up inside the
///     .bundle archive and the MSI, rather than sitting next to them.
/// </remarks>
[DependsOn<CompileProjectModule>]
public sealed class SignAssembliesModule(IOptions<SignOptions> signOptions) : Module
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

        var addinTarget = new File(Projects.ArchiWindRevitAddIn.FullName);
        var targetDirectories = addinTarget.Folder!
            .GetFolder("bin")
            .GetFolders(folder => folder.Name == "publish")
            .ToArray();

        targetDirectories.ShouldNotBeEmpty("No content was found to sign");

        var assemblies = targetDirectories
            .Select(directory => directory.GetFolder(addinTarget.NameWithoutExtension).GetFile($"{addinTarget.NameWithoutExtension}.dll"))
            .Where(assembly => assembly.Exists)
            .ToArray();

        assemblies.ShouldNotBeEmpty($"No {addinTarget.NameWithoutExtension}.dll was found to sign");

        var signExecutable = await SignRunner.InstallAsync(context, options, cancellationToken);
        await SignRunner.SignAsync(context, signExecutable, options, assemblies, cancellationToken);

        context.Summary.KeyValue("Signing", "Assemblies", assemblies.Length.ToString());
    }
}
