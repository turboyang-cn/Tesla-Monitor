﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal class DrivingEntityConfiguration : BaseEntityConfiguration<DrivingEntity>
    {
        public override void Configure(EntityTypeBuilder<DrivingEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CarId)
                   .IsRequired();
            builder.Property(x => x.StartLocation)
                   .HasColumnType("geography (point)");
            builder.Property(x => x.EndLocation)
                   .HasColumnType("geography (point)");
        }
    }
}
