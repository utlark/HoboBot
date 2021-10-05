using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HoboBot
{
    public partial class VkBotDBContext : DbContext
    {
        public VkBotDBContext()
        {
        }

        public VkBotDBContext(DbContextOptions<VkBotDBContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
        public virtual DbSet<VkAchivment> VkAchivments { get; set; }
        public virtual DbSet<VkAvatar> VkAvatars { get; set; }
        public virtual DbSet<VkBattle> VkBattles { get; set; }
        public virtual DbSet<VkGroup> VkGroups { get; set; }
        public virtual DbSet<VkGroupsCommand> VkGroupsCommands { get; set; }
        public virtual DbSet<VkUser> VkUsers { get; set; }
        public virtual DbSet<VkUsersGroup> VkUsersGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<VkAchivment>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.GroupId })
                    .HasName("PK_VK_ACHIVMENTS");

                entity.ToTable("vk_Achivments");

                entity.Property(e => e.UserId).HasColumnName("User_Id");

                entity.Property(e => e.GroupId).HasColumnName("Group_Id");

                entity.Property(e => e.Achiv1).HasColumnName("Achiv_1");

                entity.Property(e => e.Achiv2).HasColumnName("Achiv_2");

                entity.Property(e => e.Achiv3).HasColumnName("Achiv_3");

                entity.Property(e => e.Achiv4).HasColumnName("Achiv_4");

                entity.Property(e => e.Achiv5).HasColumnName("Achiv_5");

                entity.Property(e => e.Achiv6).HasColumnName("Achiv_6");

                entity.Property(e => e.Achiv7).HasColumnName("Achiv_7");

                entity.Property(e => e.Achiv8).HasColumnName("Achiv_8");

                entity.Property(e => e.Achiv9).HasColumnName("Achiv_9");

                entity.HasOne(d => d.VkUsersGroup)
                    .WithOne(p => p.VkAchivment)
                    .HasForeignKey<VkAchivment>(d => new { d.UserId, d.GroupId })
                    .HasConstraintName("vk_Achivments_fk0");
            });

            modelBuilder.Entity<VkAvatar>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.GroupId })
                    .HasName("PK_VK_AVATARS");

                entity.ToTable("vk_Avatars");

                entity.Property(e => e.UserId).HasColumnName("User_Id");

                entity.Property(e => e.GroupId).HasColumnName("Group_Id");

                entity.Property(e => e.BadFood)
                    .HasColumnName("Bad_Food")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.BadMedecine)
                    .HasColumnName("Bad_Medecine")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.Bottels).HasDefaultValueSql("('0')");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1500)
                    .HasDefaultValueSql("(N'Не повезло, не фортануло, стал бомжом')");

                entity.Property(e => e.Exp).HasDefaultValueSql("('0')");

                entity.Property(e => e.GoodFood)
                    .HasColumnName("Good_Food")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.GoodMedecine)
                    .HasColumnName("Good_Medecine")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.Health).HasDefaultValueSql("('100')");

                entity.Property(e => e.HfId)
                    .IsRequired()
                    .HasColumnName("Hf_Id")
                    .HasDefaultValueSql("(N'пусто')");

                entity.Property(e => e.KillStatus).HasColumnName("Kill_Status");

                entity.Property(e => e.Level).HasDefaultValueSql("('1')");

                entity.Property(e => e.LevelUpExp)
                    .HasColumnName("LevelUp_Exp")
                    .HasDefaultValueSql("('10')");

                entity.Property(e => e.LoseCount)
                    .HasColumnName("Lose_Count")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.Money).HasDefaultValueSql("('0')");

                entity.Property(e => e.Mood).HasDefaultValueSql("('0')");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValueSql("(N'Просто бомж')");

                entity.Property(e => e.Satiety).HasDefaultValueSql("('100')");

                entity.Property(e => e.TopCount)
                    .HasColumnName("Top_Count")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.WinCount)
                    .HasColumnName("Win_Count")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.WorkType)
                    .HasColumnName("Work_Type")
                    .HasDefaultValueSql("('1')");

                entity.HasOne(d => d.VkUsersGroup)
                    .WithOne(p => p.VkAvatar)
                    .HasForeignKey<VkAvatar>(d => new { d.UserId, d.GroupId })
                    .HasConstraintName("vk_Avatars_fk0");
            });

            modelBuilder.Entity<VkBattle>(entity =>
            {
                entity.HasKey(e => new { e.BattleId, e.UserId, e.GroupId, e.Type })
                    .HasName("PK_VK_BATTLES");

                entity.ToTable("vk_Battles");

                entity.Property(e => e.BattleId).HasColumnName("Battle_Id");

                entity.Property(e => e.UserId).HasColumnName("User_Id");

                entity.Property(e => e.GroupId).HasColumnName("Group_Id");

                entity.Property(e => e.Member)
                    .IsRequired()
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.Ready)
                    .IsRequired()
                    .HasDefaultValueSql("('0')");

                entity.HasOne(d => d.VkAvatar)
                    .WithMany(p => p.VkBattles)
                    .HasForeignKey(d => new { d.UserId, d.GroupId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("vk_Battles_fk0");
            });

            modelBuilder.Entity<VkGroup>(entity =>
            {
                entity.HasKey(e => e.GroupId)
                    .HasName("PK_VK_GROUPS");

                entity.ToTable("vk_Groups");

                entity.Property(e => e.GroupId)
                    .ValueGeneratedNever()
                    .HasColumnName("Group_Id");

                entity.Property(e => e.LastTop).HasColumnName("Last_Top");
            });

            modelBuilder.Entity<VkGroupsCommand>(entity =>
            {
                entity.HasKey(e => new { e.GroupId, e.Command })
                    .HasName("PK_VK_COMMANDS");

                entity.ToTable("vk_Groups_Commands");

                entity.Property(e => e.GroupId).HasColumnName("Group_Id");

                entity.Property(e => e.Command).HasMaxLength(100);

                entity.Property(e => e.Answer)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Prefix)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.VkGroupsCommands)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("vk_Groups_Commands_fk0");
            });

            modelBuilder.Entity<VkUser>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PK_VK_USERS");

                entity.ToTable("vk_Users");

                entity.Property(e => e.UserId)
                    .ValueGeneratedNever()
                    .HasColumnName("User_Id");

                entity.Property(e => e.Money).HasDefaultValueSql("('0')");

                entity.Property(e => e.Prime)
                    .IsRequired()
                    .HasDefaultValueSql("('0')");
            });

            modelBuilder.Entity<VkUsersGroup>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.GroupId })
                    .HasName("PK_VK_USERS_GROUPS");

                entity.ToTable("vk_Users_Groups");

                entity.Property(e => e.UserId).HasColumnName("User_Id");

                entity.Property(e => e.GroupId).HasColumnName("Group_Id");

                entity.Property(e => e.PrimePermision)
                    .IsRequired()
                    .HasColumnName("Prime_Permision")
                    .HasDefaultValueSql("('0')");

                entity.Property(e => e.UserNick)
                    .HasMaxLength(50)
                    .HasColumnName("User_Nick");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.VkUsersGroups)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("vk_Users_Groups_fk1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VkUsersGroups)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("vk_Users_Groups_fk0");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
