using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class ChargingSnapshotEntityConfiguration : BaseEntityConfiguration<ChargingSnapshotEntity>
    {
        public override void Configure(EntityTypeBuilder<ChargingSnapshotEntity> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Timestamp);

            builder.Property(x => x.ChargingId)
                   .IsRequired();
            builder.Property(x => x.Location)
                   .HasColumnType("geography (point)");
        }
    }
}
