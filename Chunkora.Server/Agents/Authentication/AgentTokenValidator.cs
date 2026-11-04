using Chunkora.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Agents.Authentication;

public sealed class AgentTokenValidator(GlobalDatabaseContext db)
{
    public async Task<Node?> ValidateAsync(string token)
    {
        return await db.Nodes
            .Where(node => node.AgentToken == token)
            .FirstOrDefaultAsync();
    }
}