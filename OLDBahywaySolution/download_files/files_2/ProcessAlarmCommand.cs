using MediatR;
using BahyWay.SharedKernel.Domain.Primitives;

namespace AlarmInsight.Application.Alarms.Commands.ProcessAlarm;

/// <summary>
/// Command to mark an alarm as processing.
/// </summary>
public sealed record ProcessAlarmCommand(int AlarmId) : IRequest<r>;
