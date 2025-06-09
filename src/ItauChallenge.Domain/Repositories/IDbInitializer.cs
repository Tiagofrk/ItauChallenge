using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IDbInitializer
    {
        Task InitializeDatabaseAsync();
    }
}
