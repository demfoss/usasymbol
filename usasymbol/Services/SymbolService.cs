using Microsoft.EntityFrameworkCore;
using USASymbol.Data;
using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services
{
    public class SymbolService : ISymbolService
    {
        private readonly AppDbContext _context;

        public SymbolService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Symbol>> GetSymbolsByStateAsync(int stateId)
        {
            return await _context.Symbols
                .Where(s => s.StateId == stateId)
                .OrderBy(s => s.Type)
                .ToListAsync();
        }

        public async Task<Symbol?> GetSymbolAsync(int stateId, string symbolType)
        {
            return await _context.Symbols
                .Include(s => s.State)
                .FirstOrDefaultAsync(s => s.StateId == stateId && s.Type == symbolType);
        }

        public async Task<List<SymbolWithState>> GetSymbolsByTypeAsync(string type)
        {
            var symbols = await _context.Symbols
                .Include(s => s.State)
                .Where(s => s.Type == type)
                .OrderBy(s => s.State!.Name)
                .ToListAsync();

            return symbols.Select(s => new SymbolWithState
            {
                Symbol = s,
                State = s.State!
            }).ToList();
        }

        public async Task<List<string>> GetAllSymbolTypesAsync()
        {
            return await _context.Symbols
                .Select(s => s.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }
    }
}