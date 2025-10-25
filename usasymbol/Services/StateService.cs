using Microsoft.EntityFrameworkCore;
using USASymbol.Data;
using USASymbol.Models;

namespace USASymbol.Services
{
    public class StateService : IStateService
    {
        private readonly AppDbContext _context;

        public StateService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<State>> GetAllStatesAsync()
        {
            return await _context.States
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<State?> GetStateBySlugAsync(string slug)
        {
            return await _context.States
                .Include(s => s.Symbols)
                .FirstOrDefaultAsync(s => s.Slug == slug);
        }

        public async Task<List<State>> GetStatesByRegionAsync(string region)
        {
            return await _context.States
                .Where(s => s.Region == region)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<State>> GetFeaturedStatesAsync(int count = 5)
        {
            // Возвращаем популярные штаты (пока просто первые N)
            return await _context.States
                .OrderByDescending(s => s.Population)
                .Take(count)
                .ToListAsync();
        }
    }
}