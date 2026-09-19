using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Entities.HistoryMap;

namespace Streetcode.DAL.Repositories.Interfaces.HistoryMap
{
    public interface IHistoryMapRecordRepository : IRepositoryBase<HistoryMapRecord>
    {
        Task<IEnumerable<HistoryMapRecord>> GetByStreetcodeIdAsync(int streetcodeId);
        Task<HistoryMapRecord?> GetByStreetcodeAndNumberAsync(int streetcodeId, int physicalStreetcodeNumber);
        Task<IEnumerable<HistoryMapRecord>> GetByToponymIdAsync(int toponymId);
    }
}