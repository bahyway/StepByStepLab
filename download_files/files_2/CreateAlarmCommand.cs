using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Commands.CreateAlarm;

/// <summary>
/// Command to create a new alarm.
/// PATTERN: CQRS Command - Request side of request/response
/// </summary>
public sealed record CreateAlarmCommand(
    string Source,
    string Description,
    int SeverityValue,
    string LocationName,
    double Latitude,
    double Longitude
) : IRequest<Result<int>>; // Returns Result<int> where int is the alarm ID
