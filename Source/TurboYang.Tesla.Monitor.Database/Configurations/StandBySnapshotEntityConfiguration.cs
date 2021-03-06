using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class StandBySnapshotEntityConfiguration : BaseEntityConfiguration<StandBySnapshotEntity>
    {
        public override void Configure(EntityTypeBuilder<StandBySnapshotEntity> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Timestamp);

            builder.Property(x => x.StandById)
                   .IsRequired();
            builder.Property(x => x.Location)
                   .HasColumnType("geography (point)");
        }
    }
}
