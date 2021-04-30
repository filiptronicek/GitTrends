using System.Threading.Tasks;

namespace GitTrends
{
    public interface IBrowserServices
    {
        Task TryClearBrowserHistory();
    }
}
