using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Persistence;
using Streetcode.DAL.Repositories.Interfaces.HistoryMap;
using Streetcode.DAL.Repositories.Realizations.Base;

namespace Streetcode.DAL.Repositories.Realizations.HistoryMap
{
    public class HistoryMapRecordRepository
        : RepositoryBase<HistoryMapRecord>, IHistoryMapRecordRepository
    {
        private readonly StreetcodeDbContext _dbContext;
        public HistoryMapRecordRepository(StreetcodeDbContext dbContext)
            : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<HistoryMapRecord>> GetByStreetcodeIdAsync(int streetcodeId)
        {
            return await _dbContext.Set<HistoryMapRecord>()
                .Where(x => x.StreetcodeId == streetcodeId)
                .Include(x => x.Toponym)
                .OrderBy(x => x.PhysicalStreetcodeNumber)
                .ToListAsync();
        }

        public async Task<HistoryMapRecord?> GetByStreetcodeAndNumberAsync(int streetcodeId, int physicalStreetcodeNumber)
        {
            return await _dbContext.Set<HistoryMapRecord>()
                .FirstOrDefaultAsync(x => x.StreetcodeId == streetcodeId
                && x.PhysicalStreetcodeNumber == physicalStreetcodeNumber);
        }

        public async Task<IEnumerable<HistoryMapRecord>> GetByToponymIdAsync(int toponymId)
        {
            return await _dbContext.Set<HistoryMapRecord>()
                .Where(x => x.ToponymId == toponymId)
                .Include(x => x.Streetcode)
                .ToListAsync();
        }
    }
}