using Schemalaggning.DTOs.BaseScheduleRules;
using Schemalaggning.DTOs.Auth;
using Schemalaggning.DTOs.Employees;
using Schemalaggning.DTOs.Roles;
using Schemalaggning.DTOs.Schedules;
using Schemalaggning.DTOs.ShiftTypes;
using Schemalaggning.DTOs.Stores;
using Schemalaggning.DTOs.StoreCoverageRules;
using Schemalaggning.Models;

namespace Schemalaggning.Services;

internal static class MappingExtensions
{
    public static RoleReadDto ToReadDto(this Role role)
    {
        return new RoleReadDto
        {
            Id = role.Id,
            Name = role.Name
        };
    }

    public static StoreReadDto ToReadDto(this Store store)
    {
        return new StoreReadDto
        {
            Id = store.Id,
            Name = store.Name
        };
    }

    public static UserAccountReadDto ToReadDto(this UserAccount userAccount)
    {
        return new UserAccountReadDto
        {
            Id = userAccount.Id,
            Email = userAccount.Email,
            AccessRole = userAccount.AccessRole,
            EmployeeId = userAccount.EmployeeId,
            StoreId = userAccount.StoreId,
            IsActive = userAccount.IsActive
        };
    }

    public static EmployeeReadDto ToReadDto(this Employee employee)
    {
        return new EmployeeReadDto
        {
            Id = employee.Id,
            Name = employee.Name,
            StoreId = employee.StoreId,
            StoreName = employee.Store?.Name ?? string.Empty,
            RoleId = employee.RoleId,
            RoleName = employee.Role?.Name ?? string.Empty,
            EmploymentPercentage = employee.EmploymentPercentage,
            BaseSchedule = employee.BaseScheduleRules
                .OrderBy(rule => rule.WeekInCycle)
                .ThenBy(rule => rule.DayOfWeek)
                .Select(rule => rule.ToReadDto())
                .ToList()
        };
    }

    public static ShiftTypeReadDto ToReadDto(this ShiftType shiftType)
    {
        return new ShiftTypeReadDto
        {
            Id = shiftType.Id,
            Name = shiftType.Name,
            RoleIds = shiftType.RoleShiftTypes
                .Select(roleShiftType => roleShiftType.RoleId)
                .ToList(),
            RoleNames = shiftType.RoleShiftTypes
                .Select(roleShiftType => roleShiftType.Role?.Name ?? string.Empty)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .ToList(),
            DefaultStartTime = shiftType.DefaultStartTime,
            DefaultEndTime = shiftType.DefaultEndTime
        };
    }

    public static BaseScheduleRuleReadDto ToReadDto(this BaseScheduleRule rule)
    {
        return new BaseScheduleRuleReadDto
        {
            Id = rule.Id,
            EmployeeId = rule.EmployeeId,
            EmployeeName = rule.Employee?.Name ?? string.Empty,
            ShiftTypeId = rule.ShiftTypeId,
            ShiftTypeName = rule.ShiftType?.Name ?? string.Empty,
            WeekInCycle = rule.WeekInCycle,
            DayOfWeek = rule.DayOfWeek,
            StartTime = rule.ShiftType?.DefaultStartTime ?? default,
            EndTime = rule.ShiftType?.DefaultEndTime ?? default
        };
    }

    public static StoreCoverageRuleReadDto ToReadDto(this StoreCoverageRule rule)
    {
        return new StoreCoverageRuleReadDto
        {
            Id = rule.Id,
            StoreId = rule.StoreId,
            StoreName = rule.Store?.Name ?? string.Empty,
            ShiftTypeId = rule.ShiftTypeId,
            ShiftTypeName = rule.ShiftType?.Name ?? string.Empty,
            WeekInCycle = rule.WeekInCycle,
            DayOfWeek = rule.DayOfWeek,
            RequiredCount = rule.RequiredCount,
            StartTime = rule.StartTime,
            EndTime = rule.EndTime
        };
    }

    public static ScheduleReadDto ToReadDto(this Schedule schedule)
    {
        return new ScheduleReadDto
        {
            Id = schedule.Id,
            Name = schedule.Name,
            PeriodStart = schedule.PeriodStart,
            PeriodEnd = schedule.PeriodEnd,
            Status = schedule.Status,
            Shifts = schedule.Shifts
                .OrderBy(shift => shift.Date)
                .ThenBy(shift => shift.StartTime)
                .Select(shift => shift.ToReadDto())
                .ToList()
        };
    }

    public static ShiftReadDto ToReadDto(this Shift shift)
    {
        return new ShiftReadDto
        {
            Id = shift.Id,
            EmployeeId = shift.EmployeeId,
            EmployeeName = shift.Employee?.Name ?? string.Empty,
            ShiftTypeId = shift.ShiftTypeId,
            ShiftTypeName = shift.ShiftType?.Name ?? string.Empty,
            Date = shift.Date,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Status = shift.Status
        };
    }
}
