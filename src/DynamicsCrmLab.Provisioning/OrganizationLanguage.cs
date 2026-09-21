using DynamicsCrmLab.Infrastructure.Dataverse;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Reads the language an environment was created with.
/// </summary>
/// <remarks>
/// Every label written through the metadata messages carries a language code,
/// and an environment only accepts codes it was provisioned for. The base
/// language is decided when the environment is created and cannot be changed
/// afterwards, so the code has to be read rather than assumed.
/// </remarks>
/// <param name="client">The Dataverse connection.</param>
internal sealed class OrganizationLanguage(IDataverseClient client)
{
    private const string EntityName = "organization";

    private const string LanguageCodeColumn = "languagecode";

    /// <summary>
    /// Reads the base language of the environment being provisioned.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The locale identifier, for example 1033 for English (United States).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the organization row carries no language code, which leaves
    /// nothing safe to label metadata with.
    /// </exception>
    public async Task<int> BaseCodeAsync(CancellationToken cancellationToken = default)
    {
        // An environment holds exactly one organization row, so the first row is
        // the row; there is nothing to filter on.
        var found = await client.RetrieveMultipleAsync(
            new QueryExpression(EntityName)
            {
                ColumnSet = new ColumnSet(LanguageCodeColumn),
                TopCount = 1
            },
            cancellationToken).ConfigureAwait(false);

        if (found.Entities.Count == 0
            || found.Entities[0].GetAttributeValue<int?>(LanguageCodeColumn) is not { } code)
        {
            throw new InvalidOperationException(
                "The environment did not report a base language, so metadata cannot be labelled.");
        }

        return code;
    }
}
