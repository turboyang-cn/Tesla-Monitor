using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class FirewareEntityConfiguration : BaseEntityConfiguration<FirewareEntity>
    {
        public override void Configure(EntityTypeBuilder<FirewareEntity> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Timestamp);

            builder.Property(x => x.Version)
                   .IsRequired();
            builder.Property(x => x.Timestamp)
                   .IsRequired();
            builder.Property(x => x.State)
                   .IsRequired()
                   .HasDefaultValue(FirewareState.Pending);
            builder.Property(x => x.CarId)
                   .IsRequired();
        }
    }
}
