using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.ScheduleGenerationSettings;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class ScheduleGenerationSettingsService : IScheduleGenerationSettingsService
{
    public const decimal DefaultMinimumRestHours = 11m;
    public const int DefaultMaxConsecutiveWorkDays = 5;
    public const bool DefaultBalanceWeekends = true;

    private readonly AppDbContext _context;
    private readonly IStoreRepository _storeRepository;

    public ScheduleGenerationSettingsService(AppDbContext context, IStoreRepository storeRepository)
    {
        _context = context;
        _storeRepository = storeRepository;
    }

    public async Task<ScheduleGenerationSettingsReadDto> GetByStoreIdAsync(int storeId)
    {
        if (!await _storeRepository.ExistsAsync(storeId))
        {
            throw new InvalidOperationException($"Store {storeId} does not exist.");
        }

        var settings = await _context.ScheduleGenerationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.StoreId == storeId);

        return ToReadDto(storeId, settings);
    }

    public async Task<ScheduleGenerationSettingsReadDto> UpdateAsync(int storeId, ScheduleGenerationSettingsUpdateDto dto)
    {
        if (!await _storeRepository.ExistsAsync(storeId))
        {
            throw new InvalidOperationException($"Store {storeId} does not exist.");
        }

        if (dto.MinimumRestHours < 0 || dto.MinimumRestHours > 24)
        {
            throw new ArgumentException("Minimum rest hours must be between 0 and 24.");
        }

        if (dto.MaxConsecutiveWorkDays < 1 || dto.MaxConsecutiveWorkDays > 28)
        {
            throw new ArgumentException("Max consecutive work days must be between 1 and 28.");
        }

        var settings = await _context.ScheduleGenerationSettings
            .FirstOrDefaultAsync(item => item.StoreId == storeId);

        if (settings is null)
        {
            settings = new ScheduleGenerationSettings
            {
                StoreId = storeId
            };
            _context.ScheduleGenerationSettings.Add(settings);
        }

        settings.MinimumRestHours = dto.MinimumRestHours;
        settings.MaxConsecutiveWorkDays = dto.MaxConsecutiveWorkDays;
        settings.BalanceWeekends = dto.BalanceWeekends;
        await _context.SaveChangesAsync();

        return ToReadDto(storeId, settings);
    }

    private static ScheduleGenerationSettingsReadDto ToReadDto(
        int storeId,
        ScheduleGenerationSettings? settings)
    {
        return new ScheduleGenerationSettingsReadDto
        {
            StoreId = storeId,
            MinimumRestHours = settings?.MinimumRestHours ?? DefaultMinimumRestHours,
            MaxConsecutiveWorkDays =
                settings?.MaxConsecutiveWorkDays ?? DefaultMaxConsecutiveWorkDays,
            BalanceWeekends = settings?.BalanceWeekends ?? DefaultBalanceWeekends
        };
    }
}
