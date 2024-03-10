using ProtonVPN.Api.Contracts.Servers;
using ProtonVPN.Core.Abstract;

namespace ProtonVPN.Core.Servers.Specs
{
    public class ServerByName : Specification<LogicalServerResponse>
    {
        private readonly string _name;

        public ServerByName(string name)
        {
            _name = name;
        }

        public override bool IsSatisfiedBy(LogicalServerResponse item)
        {
            return item.Name == _name;
        }
    }
}