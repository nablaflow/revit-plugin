using System.ComponentModel.DataAnnotations;

namespace Build.Options;

/// <summary>
///     Azure Artifact Signing (formerly Trusted Signing) options.
/// </summary>
/// <remarks>
///     Every value is bindable from the environment using the <c>Sign__</c> prefix, e.g.
///     <c>Sign__CertificateProfile</c>. Azure credentials themselves are not bound here:
///     the signing tool reads them from the ambient environment or from an <c>az login</c> session.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/azure/artifact-signing/overview">What is Artifact Signing?</seealso>
[Serializable]
public sealed record SignOptions
{
    /// <summary>
    ///     Whether artifacts should be signed. Disabled by default so local builds work without Azure access.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    ///     The Artifact Signing account URI, region specific.
    /// </summary>
    /// <example>https://weu.codesigning.azure.net/</example>
    public string? Endpoint { get; init; }

    /// <summary>
    ///     The Artifact Signing account name.
    /// </summary>
    public string? Account { get; init; }

    /// <summary>
    ///     The certificate profile to sign with.
    /// </summary>
    /// <remarks>
    ///     Use a <c>Public Trust Test</c> profile for the inner dev loop: those signatures chain to a root that
    ///     is deliberately not trusted, and are not distributable.
    /// </remarks>
    public string? CertificateProfile { get; init; }

    /// <summary>
    ///     The Azure credential the signing tool authenticates with.
    /// </summary>
    /// <remarks>
    ///     <c>azure-cli</c> covers both a local <c>az login</c> and CI, where <c>azure/login</c> establishes the
    ///     session from a federated OIDC token.
    /// </remarks>
    [Required] public string CredentialType { get; init; } = "azure-cli";

    /// <summary>
    ///     An RFC3161 timestamping service.
    /// </summary>
    /// <remarks>
    ///     Mandatory: Artifact Signing certificates are short lived, so an untimestamped signature stops
    ///     verifying within a day.
    /// </remarks>
    [Required] public string TimestampUrl { get; init; } = "http://timestamp.acs.microsoft.com";

    /// <summary>
    ///     Version of the <c>sign</c> dotnet tool to install. When empty, the latest prerelease is used.
    /// </summary>
    public string? ToolVersion { get; init; }
}
