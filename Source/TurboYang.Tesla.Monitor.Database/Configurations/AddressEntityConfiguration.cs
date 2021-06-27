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
    internal class AddressEntityConfiguration : BaseEntityConfiguration<AddressEntity>
    {
        public override void Configure(EntityTypeBuilder<AddressEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Location)
                   .IsRequired()
                   .HasColumnType("geography (point)");
            builder.Property(x => x.Radius)
                   .IsRequired()
                   .HasDefaultValue(0.03);
        }
    }
}
