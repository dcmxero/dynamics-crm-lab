using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Builds the metadata objects the platform expects, with the boilerplate
/// gathered in one place.
/// </summary>
/// <remarks>
/// Every label in Dataverse is a <see cref="Label"/> carrying a language code,
/// and repeating that at each call site buries what is actually being declared.
/// </remarks>
internal static class MetadataFactory
{
    /// <summary>English (United States), the language these environments start with.</summary>
    private const int LanguageCode = 1033;

    public static Label Label(string text) => new(text, LanguageCode);

    public static StringAttributeMetadata Text(
        string schemaName,
        string display,
        string description,
        int maxLength = 100,
        bool required = false) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            Description = Label(description),
            MaxLength = maxLength,
            FormatName = StringFormatName.Text,
            RequiredLevel = Requirement(required)
        };

    public static MemoAttributeMetadata Memo(string schemaName, string display, string description) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            Description = Label(description),
            MaxLength = 2000,
            RequiredLevel = Requirement(required: false)
        };

    public static IntegerAttributeMetadata Whole(
        string schemaName,
        string display,
        string description,
        int minimum = 0,
        int maximum = int.MaxValue,
        bool required = false) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            Description = Label(description),
            MinValue = minimum,
            MaxValue = maximum,
            RequiredLevel = Requirement(required)
        };

    public static MoneyAttributeMetadata Currency(string schemaName, string display, string description) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            Description = Label(description),
            PrecisionSource = 2,
            RequiredLevel = Requirement(required: false)
        };

    public static BooleanAttributeMetadata YesNo(
        string schemaName,
        string display,
        string description,
        bool defaultValue) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            Description = Label(description),
            DefaultValue = defaultValue,
            RequiredLevel = Requirement(required: false),
            OptionSet = new BooleanOptionSetMetadata(
                new OptionMetadata(Label("Yes"), 1),
                new OptionMetadata(Label("No"), 0))
        };

    /// <summary>
    /// Builds a choice column whose values are supplied rather than generated.
    /// </summary>
    /// <remarks>
    /// The numbers have to match the domain enum, so they are stated here
    /// instead of letting the platform assign its own from the publisher range.
    /// </remarks>
    public static PicklistAttributeMetadata Choice(
        string schemaName,
        string display,
        string description,
        int defaultValue) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            Description = Label(description),
            RequiredLevel = Requirement(required: true),
            OptionSet = new OptionSetMetadata { IsGlobal = false, OptionSetType = OptionSetType.Picklist },
            DefaultFormValue = defaultValue
        };

    public static EntityMetadata Table(
        string schemaName,
        string display,
        string plural,
        string description) =>
        new()
        {
            SchemaName = schemaName,
            DisplayName = Label(display),
            DisplayCollectionName = Label(plural),
            Description = Label(description),
            OwnershipType = OwnershipTypes.UserOwned,
            IsActivity = false,
            HasNotes = false,
            HasActivities = false
        };

    private static AttributeRequiredLevelManagedProperty Requirement(bool required) =>
        new(required ? AttributeRequiredLevel.ApplicationRequired : AttributeRequiredLevel.None);
}
