using System.Threading.Tasks;

namespace ProtonVPN.CLI
{
    public interface ICommandAware
    {
        Task OnCommandReceived(CommandEventArgs args);
    }
}
