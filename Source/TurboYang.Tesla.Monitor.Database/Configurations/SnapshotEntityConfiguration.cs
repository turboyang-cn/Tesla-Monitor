
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class SnapshotEntityConfiguration : BaseEntityConfiguration<SnapshotEntity>
    {
        public override void Configure(EntityTypeBuilder<SnapshotEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CarId)
                   .IsRequired();
            builder.Property(x => x.StateId)
                   .IsRequired();
            builder.Property(x => x.Location)
                   .HasColumnType("geography (point)");
        }
    }
}
