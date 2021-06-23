
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class CarEntityConfiguration : BaseEntityConfiguration<CarEntity>
    {
        public override void Configure(EntityTypeBuilder<CarEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.VehicleId)
                   .IsRequired();
            builder.Property(x => x.TokenId)
                   .IsRequired();
        }
    }
}
