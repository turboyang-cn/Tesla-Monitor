using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class DrivingSnapshotEntityConfiguration : BaseEntityConfiguration<DrivingSnapshotEntity>
    {
        public override void Configure(EntityTypeBuilder<DrivingSnapshotEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.DrivingId)
                   .IsRequired();
            builder.Property(x => x.Location)
                   .HasColumnType("geography (point)");
        }
    }
}
