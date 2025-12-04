using Nocturne.Core.Models;

namespace Nocturne.Connectors.Core.Interfaces;

/// <summary>
/// Service for submitting data directly to the Nocturne API via HTTP
/// </summary>
public interface IApiDataSubmitter
{
    /// <summary>
    /// Submit glucose entries to the API
    /// </summary>
    /// <param name="entries">Glucose entries to submit</param>
    /// <param name="source">Source connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitEntriesAsync(
        IEnumerable<Entry> entries,
        string source,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Submit treatments to the API
    /// </summary>
    /// <param name="treatments">Treatments to submit</param>
    /// <param name="source">Source connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitTreatmentsAsync(
        IEnumerable<Treatment> treatments,
        string source,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Submit device status to the API
    /// </summary>
    /// <param name="deviceStatuses">Device statuses to submit</param>
    /// <param name="source">Source connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitDeviceStatusAsync(
        IEnumerable<DeviceStatus> deviceStatuses,
        string source,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Submit profiles to the API
    /// </summary>
    /// <param name="profiles">Profiles to submit</param>
    /// <param name="source">Source connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitProfilesAsync(
        IEnumerable<Profile> profiles,
        string source,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Submit food items to the API
    /// </summary>
    /// <param name="foods">Food items to submit</param>
    /// <param name="source">Source connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitFoodAsync(
        IEnumerable<Food> foods,
        string source,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Submit activity events to the API
    /// </summary>
    /// <param name="activities">Activity events to submit</param>
    /// <param name="source">Source connector identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful</returns>
    Task<bool> SubmitActivityAsync(
        IEnumerable<Activity> activities,
        string source,
        CancellationToken cancellationToken = default
    );
}
