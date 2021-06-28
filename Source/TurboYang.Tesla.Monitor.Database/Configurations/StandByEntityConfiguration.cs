using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class StandByEntityConfiguration : BaseEntityConfiguration<StandByEntity>
    {
        public override void Configure(EntityTypeBuilder<StandByEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CarId)
                   .IsRequired();
        }
    }
}
