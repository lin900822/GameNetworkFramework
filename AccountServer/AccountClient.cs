using Server;

namespace AccountServer;

public class AccountClient : ClientBase<AccountClient>
{
    public int Id { get; set; }
}