using BahyWay.SharedKernel.Domain.Primitives;
using AlarmInsight.Domain.Aggregates;

namespace AlarmInsight.Application.Abstractions;

/// <summary>
/// Repository interface for Alarm aggregate.
/// Will be implemented in Infrastructure layer.
/// </summary>
public interface IAlarmRepository
{
    /// <summary>
    /// Gets an alarm by ID.
    /// </summary>
    Task<Alarm?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active alarms (pending or processing).
    /// </summary>
    Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alarms by location.
    /// </summary>
    Task<IEnumerable<Alarm>> GetByLocationAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new alarm.
    /// </summary>
    Task AddAsync(Alarm alarm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing alarm.
    /// </summary>
    void Update(Alarm alarm);

    /// <summary>
    /// Deletes an alarm.
    /// </summary>
    void Delete(Alarm alarm);
}
