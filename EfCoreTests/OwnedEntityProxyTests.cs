using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfCoreTests
{
    public class OwnedEntityProxyTests
    {
        [Fact]
        public void InMemoryTest()
        {
            Assert.True(Test(new DbContextOptionsBuilder<TestContext>()
                .UseInMemoryDatabase(databaseName: "Test")));
        }

        [Fact]
        public void MsSqlTest()
        {
            Assert.True(Test(new DbContextOptionsBuilder<TestContext>()
                .UseSqlServer("Data Source=.;MultipleActiveResultSets=True;Integrated Security=True")));
        }

        [Fact]
        public void SqliteTest()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            Assert.True(Test(new DbContextOptionsBuilder<TestContext>()
                .UseSqlite(connection)));
        }

        private static bool Test(DbContextOptionsBuilder<TestContext> builder)
        {
            var options = builder.UseLazyLoadingProxies().Options;
            using (var ctx = new TestContext(options))
            {
                ctx.Database.EnsureCreated();
            }
            using (var ctx = new TestContext(options))
            {
                ctx.TestEntities.RemoveRange(ctx.TestEntities.ToArray());
                ctx.TestEntities.Add(new TestEntity());
                ctx.SaveChanges();
            }

            using (var ctx = new TestContext(options))
            {
                var e = ctx.TestEntities.Single();
                e.TestOwnedEntity.Code = "test";
                return ctx.ChangeTracker.Entries().Any(o => o.State != EntityState.Unchanged);
            }
        }
    }

    public class TestContext : DbContext
    {
        public TestContext() : base(new DbContextOptionsBuilder<TestContext>()
            .UseSqlServer("Data Source=.;MultipleActiveResultSets=True;Integrated Security=True")
            .UseLazyLoadingProxies()
            .Options)
        {
        }

        public TestContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; }
    }

    public class TestEntity
    {
        public long Id { get; set; }
        public virtual TestOwnedEntity TestOwnedEntity { get; private set; } = new TestOwnedEntity();
    }

    [Owned]
    public class TestOwnedEntity
    {
        public string Code { get; set; }

        protected bool Equals(TestOwnedEntity other)
        {
            return string.Equals(Code, other.Code);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((TestOwnedEntity) obj);
        }

        public override int GetHashCode()
        {
            return (Code != null ? Code.GetHashCode() : 0);
        }
    }
}