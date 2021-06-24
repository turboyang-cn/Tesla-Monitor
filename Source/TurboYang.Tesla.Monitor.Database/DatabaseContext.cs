using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

using NLog;

using NodaTime;

using Npgsql;

using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Database.Functions;

namespace TurboYang.Tesla.Monitor.Database
{
    public class DatabaseContext : DbContext
    {
        private ILogger Logger { get; } = LogManager.GetCurrentClassLogger();

        private static IReadOnlyDictionary<String, String> NameMapping
        {
            get
            {
                return new ReadOnlyDictionary<String, String>(new Dictionary<String, String>());
            }
        }
        private static String TableNameSuffix { get; } = "Entity";
        private static List<Type> EnumTypes { get; } = typeof(BaseEntity).Assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(typeof(BaseEntity))).SelectMany(x => x.GetProperties()).Select(x => x.PropertyType).Where(x => x.IsEnum || (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>) && x.GetGenericArguments().FirstOrDefault().IsEnum)).Select(x =>
        {
            if (x.IsEnum)
            {
                return x;
            }

            return x.GetGenericArguments().FirstOrDefault();
        }).ToList();
        private static List<Type> FunctionResultTypes { get; } = typeof(DatabaseContext).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.GetCustomAttribute<DbFunctionAttribute>() != null && x.ReturnType.IsGenericType && x.ReturnType.GetGenericTypeDefinition() == typeof(IQueryable<>)).Select(x => x.ReturnType.GetGenericArguments().FirstOrDefault()).ToList();
        public DatabaseContext(DbContextOptions options)
              : base(options)
        {
            GlobalEnumTypeMapper();
        }

        private DatabaseFunction _Functions;
        public DatabaseFunction Functions
        {
            get
            {
                if (_Functions == null)
                {
                    _Functions = new DatabaseFunction(this);
                }

                return _Functions;
            }
        }

        #region Table

        public DbSet<TokenEntity> Token { get; set; }
        public DbSet<CarEntity> Car { get; set; }
        public DbSet<StateEntity> State { get; set; }
        public DbSet<SnapshotEntity> Snapshot { get; set; }
        public DbSet<DrivingEntity> Driving { get; set; }
        public DbSet<DrivingSnapshotEntity> DrivingSnapshot { get; set; }
        public DbSet<StandByEntity> StandBy { get; set; }
        public DbSet<StandBySnapshotEntity> StandBySnapshot { get; set; }
        public DbSet<ChargingEntity> Charging { get; set; }
        public DbSet<ChargingSnapshotEntity> ChargingSnapshot { get; set; }
        public DbSet<CarSettingEntity> CarSetting { get; set; }
        public DbSet<AddressEntity> Address { get; set; }

        #endregion

        #region Function

        #endregion

        #region SaveChanges

        public new Int32 SaveChanges()
        {
            ApplyCreateUpdateMetadata();

            return base.SaveChanges();
        }

        public new Int32 SaveChanges(Boolean acceptAllChangesOnSuccess)
        {
            ApplyCreateUpdateMetadata();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public Int32 SaveChanges(String userCode)
        {
            ApplyCreateUpdateMetadata(userCode);

            return base.SaveChanges();
        }

        public Int32 SaveChanges(String userCode, Boolean acceptAllChangesOnSuccess)
        {
            ApplyCreateUpdateMetadata(userCode);

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public new async Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyCreateUpdateMetadata();

            return await base.SaveChangesAsync(cancellationToken);
        }

        public new Task<Int32> SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyCreateUpdateMetadata();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public async Task<Int32> SaveChangesAsync(String userCode, CancellationToken cancellationToken = default)
        {
            ApplyCreateUpdateMetadata(userCode);

            return await base.SaveChangesAsync(cancellationToken);
        }

        public async Task<Int32> SaveChangesAsync(String userCode, Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyCreateUpdateMetadata(userCode);

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected virtual void ApplyCreateUpdateMetadata()
        {
            ApplyCreateUpdateMetadata("System");
        }

        protected virtual void ApplyCreateUpdateMetadata(String userCode)
        {
            if (String.IsNullOrEmpty(userCode))
            {
                userCode = "System";
            }

            Instant now = Instant.FromDateTimeUtc(DateTime.UtcNow);

            foreach (EntityEntry entry in ChangeTracker.Entries().Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified)))
            {
                BaseEntity entity = entry.Entity as BaseEntity;

                if (entry.State == EntityState.Modified)
                {
                    entity.UpdateBy = userCode;
                    if (entity.UpdateTimestamp == null)
                    {
                        entity.UpdateTimestamp = now;
                    }
                }
                else if (entry.State == EntityState.Added)
                {
                    entity.CreateBy = userCode;
                    entity.UpdateBy = userCode;
                    if (entity.CreateTimestamp == null)
                    {
                        entity.CreateTimestamp = now;
                    }
                    if (entity.UpdateTimestamp == null)
                    {
                        entity.UpdateTimestamp = now;
                    }
                }
            }

            Logger.Trace($"Change Tracker Entity Count: {ChangeTracker.Entries().Count()}");
        }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            RegisterFunction(modelBuilder);

            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("postgis");

            RegisterEnumType(modelBuilder);

            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                #region Rename Table Name

                String tableName = entityType.GetDefaultTableName();

                if (tableName.EndsWith(TableNameSuffix))
                {
                    tableName = tableName.Remove(tableName.Length - TableNameSuffix.Length);
                }

                foreach (KeyValuePair<String, String> mapping in NameMapping)
                {
                    tableName = tableName.Replace(mapping.Key, mapping.Value);
                }

                entityType.SetTableName(tableName);

                #endregion

                foreach (IMutableProperty property in entityType.GetProperties())
                {
                    #region Rename Field

                    String fieldName = property.GetColumnName(StoreObjectIdentifier.Create(property.DeclaringEntityType, StoreObjectType.Table).GetValueOrDefault());

                    foreach (KeyValuePair<String, String> mapping in NameMapping)
                    {
                        fieldName = fieldName.Replace(mapping.Key, mapping.Value);
                    }

                    property.SetColumnName(fieldName);

                    #endregion

                    #region Rename Foreign Key

                    foreach (IMutableForeignKey foreignKey in entityType.FindForeignKeys(property))
                    {
                        String foreignTableName = foreignKey.PrincipalEntityType.GetTableName();
                        foreach (KeyValuePair<String, String> mapping in NameMapping)
                        {
                            foreignTableName = foreignTableName.Replace(mapping.Key, mapping.Value);
                        }

                        String foreignKeyName = $"FK_{tableName}_{foreignTableName}_{fieldName}";

                        foreignKey.SetConstraintName(foreignKeyName);
                    }

                    #endregion
                }
                #region Rename Index

                foreach (IMutableIndex index in entityType.GetIndexes())
                {
                    String indexName = $"IX_{tableName}";
                    foreach (IMutableProperty item in index.Properties)
                    {
                        indexName += $"_{item.Name}";
                    }
                    foreach (KeyValuePair<String, String> mapping in NameMapping)
                    {
                        indexName = indexName.Replace(mapping.Key, mapping.Value);
                    }

                    index.SetDatabaseName(indexName);
                }

                #endregion
            }

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        private void GlobalEnumTypeMapper()
        {
            MethodInfo mapEnumMethod = NpgsqlConnection.GlobalTypeMapper.GetType().GetMethods().Single(x => x.Name == nameof(NpgsqlConnection.GlobalTypeMapper.MapEnum) && x.IsGenericMethod);

            foreach (Type enumType in EnumTypes)
            {
                mapEnumMethod.MakeGenericMethod(enumType).Invoke(NpgsqlConnection.GlobalTypeMapper, new Object[] { null, null });
            }
        }

        private ModelBuilder RegisterEnumType(ModelBuilder modelBuilder)
        {
            MethodInfo hasPostgresEnumMethod = typeof(NpgsqlModelBuilderExtensions).GetMethods().Single(x => x.Name == nameof(NpgsqlModelBuilderExtensions.HasPostgresEnum) && x.IsGenericMethod);

            foreach (Type enumType in EnumTypes)
            {
                hasPostgresEnumMethod.MakeGenericMethod(enumType).Invoke(null, new Object[] { modelBuilder, null, null, null });
            }

            return modelBuilder;
        }

        private ModelBuilder RegisterFunction(ModelBuilder modelBuilder)
        {
            foreach (Type functionResultType in FunctionResultTypes)
            {
                modelBuilder.Entity(functionResultType).HasNoKey().ToTable("Table", x => x.ExcludeFromMigrations());
            }

            return modelBuilder;
        }
    }
}
