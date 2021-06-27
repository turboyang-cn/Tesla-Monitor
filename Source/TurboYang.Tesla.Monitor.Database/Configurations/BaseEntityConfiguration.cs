using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Database.Configurations
{
    internal abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
        where T : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.OpenId);

            builder.Property(x => x.Id)
                   .IsRequired()
                   .HasIdentityOptions(startValue: 10000000);

            builder.Property(x => x.OpenId)
                   .HasDefaultValueSql("uuid_generate_v4()")
                   .IsRequired();
            builder.Property(x => x.CreateBy)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("System");
            builder.Property(x => x.UpdateBy)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("System");
            builder.Property(x => x.CreateTimestamp)
                   .IsRequired()
                   .HasDefaultValueSql("timezone('utc'::text, now())")
                   .ValueGeneratedOnAdd();
            builder.Property(x => x.UpdateTimestamp)
                   .IsRequired()
                   .HasDefaultValueSql("timezone('utc'::text, now())");

            CreateSeedData(builder);
        }

        public virtual void CreateSeedData(EntityTypeBuilder<T> builder)
        {
        }
    }
}
