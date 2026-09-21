using Microsoft.Extensions.Logging;

namespace DynamicsCrmLab.Provisioning;

/// <summary>
/// Provides the messages the provisioning tool writes as it works.
/// </summary>
/// <remarks>
/// Source generated so that nothing is formatted when a level is switched off,
/// and so the messages are declared in one place rather than scattered as
/// string literals through the steps.
/// </remarks>
internal static partial class ProvisioningLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Publisher {Name} is already there.")]
    public static partial void PublisherExists(ILogger logger, string name);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Created publisher {Name} with prefix {Prefix}.")]
    public static partial void PublisherCreated(ILogger logger, string name, string prefix);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Solution {Name} is already there.")]
    public static partial void SolutionExists(ILogger logger, string name);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Created solution {Name}.")]
    public static partial void SolutionCreated(ILogger logger, string name);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Publishing customizations.")]
    public static partial void Publishing(ILogger logger);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Table {Table} is already there.")]
    public static partial void TableExists(ILogger logger, string table);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Created table {Table}.")]
    public static partial void TableCreated(ILogger logger, string table);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Information,
        Message = "Column {Column} on {Table} is already there.")]
    public static partial void ColumnExists(ILogger logger, string column, string table);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Created column {Column} on {Table}.")]
    public static partial void ColumnCreated(ILogger logger, string column, string table);

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Information,
        Message = "Created lookup {Column} from {Many} to {One}.")]
    public static partial void LookupCreated(ILogger logger, string column, string many, string one);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Information,
        Message = "Alternate key on the work order number is already there.")]
    public static partial void KeyExists(ILogger logger);

    [LoggerMessage(
        EventId = 2011,
        Level = LogLevel.Information,
        Message = "Created the alternate key on the work order number.")]
    public static partial void KeyCreated(ILogger logger);

    [LoggerMessage(EventId = 2023, Level = LogLevel.Information, Message = "Turned activities on for {Table}.")]
    public static partial void ActivitiesEnabled(ILogger logger, string table);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "{Table} {Value} is already there.")]
    public static partial void RecordExists(ILogger logger, string table, string value);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Information, Message = "Created {Table} {Value}.")]
    public static partial void RecordCreated(ILogger logger, string table, string value);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Information, Message = "Registered plug-in package {Name}.")]
    public static partial void PackageRegistered(ILogger logger, string name);

    [LoggerMessage(
        EventId = 2015,
        Level = LogLevel.Information,
        Message = "Uploaded plug-in package {Name} over the one already there.")]
    public static partial void PackageUpdated(ILogger logger, string name);

    [LoggerMessage(EventId = 2016, Level = LogLevel.Information, Message = "Plug-in {Type} is already there.")]
    public static partial void PluginTypeExists(ILogger logger, string type);

    [LoggerMessage(EventId = 2017, Level = LogLevel.Information, Message = "Registered plug-in {Type}.")]
    public static partial void PluginTypeRegistered(ILogger logger, string type);

    [LoggerMessage(EventId = 2018, Level = LogLevel.Information, Message = "Step {Step} is already there.")]
    public static partial void StepExists(ILogger logger, string step);

    [LoggerMessage(
        EventId = 2019,
        Level = LogLevel.Information,
        Message = "Registered step {Step} at {Stage}, {Mode}.")]
    public static partial void StepRegistered(ILogger logger, string step, PipelineStage stage, ExecutionMode mode);

    [LoggerMessage(
        EventId = 2022,
        Level = LogLevel.Information,
        Message = "Removed step {Step}, which the code no longer declares.")]
    public static partial void StepRemoved(ILogger logger, string step);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Information, Message = "Pre image on {Step} is already there.")]
    public static partial void ImageExists(ILogger logger, string step);

    [LoggerMessage(EventId = 2021, Level = LogLevel.Information, Message = "Registered the pre image on {Step}.")]
    public static partial void ImageRegistered(ILogger logger, string step);
}
