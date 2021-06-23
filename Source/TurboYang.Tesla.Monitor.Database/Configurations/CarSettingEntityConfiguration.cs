using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class CarSettingEntityConfiguration : BaseEntityConfiguration<CarSettingEntity>
    {
        public override void Configure(EntityTypeBuilder<CarSettingEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.SamplingRate)
                   .IsRequired()
                   .HasDefaultValue(10);
            builder.Property(x => x.IsSamplingCompression)
                   .IsRequired()
                   .HasDefaultValue(true);
            builder.Property(x => x.TryAsleepDelay)
                   .IsRequired()
                   .HasDefaultValue(300);

            builder.Property(x => x.CarId)
                   .IsRequired();

            builder.HasIndex(x => x.CarId)
                   .IsUnique();
        }
    }
}
