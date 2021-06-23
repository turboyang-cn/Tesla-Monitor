
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class TokenEntityConfiguration : BaseEntityConfiguration<TokenEntity>
    {
        public override void Configure(EntityTypeBuilder<TokenEntity> builder)
        {
            base.Configure(builder);
        }
    }
}
