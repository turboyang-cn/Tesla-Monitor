using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class ChargingEntityConfiguration : BaseEntityConfiguration<ChargingEntity>
    {
        public override void Configure(EntityTypeBuilder<ChargingEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CarId)
                   .IsRequired();
        }
    }
}
