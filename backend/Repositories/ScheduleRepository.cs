using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly AppDbContext _context;

    public ScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Schedule>> GetAllAsync()
    {
        return _context.Schedules
            .AsNoTracking()
            .OrderByDescending(schedule => schedule.PeriodStart)
            .ToListAsync();
    }

    public Task<Schedule?> GetByIdAsync(int id)
    {
        return _context.Schedules.FindAsync(id).AsTask();
    }

    public Task<Schedule?> GetByIdWithShiftsAsync(int id)
    {
        return _context.Schedules
            .Include(schedule => schedule.Shifts)
                .ThenInclude(shift => shift.Employee)
            .Include(schedule => schedule.Shifts)
                .ThenInclude(shift => shift.ShiftType)
            .FirstOrDefaultAsync(schedule => schedule.Id == id);
    }

    public async Task<Schedule> CreateAsync(Schedule schedule)
    {
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<bool> UpdateAsync(Schedule schedule)
    {
        _context.Schedules.Update(schedule);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schedule = await _context.Schedules.FindAsync(id);
        if (schedule is null)
        {
            return false;
        }

        _context.Schedules.Remove(schedule);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<Shift?> GetShiftByIdAsync(int shiftId)
    {
        return _context.Shifts
            .Include(shift => shift.Schedule)
            .Include(shift => shift.Employee)
            .Include(shift => shift.ShiftType)
            .FirstOrDefaultAsync(shift => shift.Id == shiftId);
    }

    public async Task<bool> UpdateShiftAsync(Shift shift)
    {
        _context.Shifts.Update(shift);
        return await _context.SaveChangesAsync() > 0;
    }
}
