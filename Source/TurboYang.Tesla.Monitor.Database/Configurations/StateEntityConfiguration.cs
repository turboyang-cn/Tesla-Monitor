
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class StateEntityConfiguration : BaseEntityConfiguration<StateEntity>
    {
        public override void Configure(EntityTypeBuilder<StateEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.State)
                   .IsRequired();
            builder.Property(x => x.StartTimestamp)
                   .IsRequired();
            builder.Property(x => x.CarId)
                   .IsRequired();
        }
    }
}
