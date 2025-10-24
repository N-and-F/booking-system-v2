using reservationSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace reservationSystem.Data
{
    public class DataSet : DbContext
    {
        public DataSet(DbContextOptions<DataSet> options) : base(options)
        {
        }

        public virtual DbSet<Guest> Guests { get; set; }
        public virtual DbSet<Room> Rooms { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<BookingRoom> BookingRooms { get; set; }
        public virtual DbSet<BookingPayment> BookingPayments { get; set; }
        public virtual DbSet<Hotel> Hotels { get; set; }
        public virtual DbSet<RoomType> RoomTypes { get; set; }
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<AccountTransaction> AccountTransactions { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the entity models here
            modelBuilder.Entity<Guest>()
                .HasMany(g => g.Bookings)
                .WithOne(g => g.Guest)
                .HasForeignKey(g => g.GuestId)
                .IsRequired();
            modelBuilder.Entity<Guest>()
                .HasOne(g => g.Hotel)
                .WithMany(g => g.Guests)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Room>()
                .HasOne(g => g.Hotel)
                .WithMany(g => g.Rooms)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Room>()
                .HasOne(g => g.RoomType)
                .WithMany(g => g.Rooms)
                .HasForeignKey(g => g.Type)
                .IsRequired();
            modelBuilder.Entity<User>()
                .HasOne(g => g.Hotel)
                .WithMany(g => g.Users)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Booking>()
                .HasOne(g => g.Guest)
                .WithMany(g => g.Bookings)
                .HasForeignKey(g => g.GuestId)
                .IsRequired();
            modelBuilder.Entity<Booking>()
                .HasOne(g => g.Hotel)
                .WithMany(g => g.Bookings)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Booking>()
				.HasMany(g => g.BookingPayments)
				.WithOne(g => g.Booking)
				.HasForeignKey(g => g.BookingId)
				.IsRequired();
            modelBuilder.Entity<Booking>()
                .HasOne(g => g.Account)
                .WithMany(g => g.Bookings)
                .HasForeignKey(g => g.AccountId)
                .IsRequired();
            modelBuilder.Entity<BookingRoom>()
		        .HasKey(br => new { br.BookingId, br.RoomId });

			modelBuilder.Entity<BookingRoom>()
				.HasOne(br => br.Booking)
				.WithMany(b => b.BookingRooms)
				.HasForeignKey(br => br.BookingId);

			modelBuilder.Entity<BookingRoom>()
				.HasOne(br => br.Room)
				.WithMany(r => r.BookingRooms)
				.HasForeignKey(br => br.RoomId);
			modelBuilder.Entity<BookingPayment>()
				.HasOne(g => g.Booking)
				.WithMany(g => g.BookingPayments)
				.HasForeignKey(g => g.BookingId)
				.IsRequired();
            modelBuilder.Entity<Hotel>()
                .HasMany(g => g.Bookings)
                .WithOne(g => g.Hotel)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Hotel>()
                .HasMany(g => g.Rooms)
                .WithOne(g => g.Hotel)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Hotel>()
                .HasMany(g => g.Users)
                .WithOne(g => g.Hotel)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Hotel>()
                .HasMany(g => g.Guests)
                .WithOne(g => g.Hotel)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Hotel>()
                .HasMany(g => g.RoomTypes)
                .WithOne(g => g.Hotel)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<RoomType>()
                .HasMany(g => g.Rooms)
                .WithOne(g => g.RoomType)
                .HasForeignKey(g => g.Type)
                .IsRequired();
            modelBuilder.Entity<RoomType>()
                .HasOne(g => g.Hotel)
                .WithMany(g => g.RoomTypes)
                .HasForeignKey(g => g.HotelId)
                .IsRequired();
            modelBuilder.Entity<Account>()
                .HasMany(g => g.AccountTransactions)
                .WithOne(g => g.Account)
                .HasForeignKey(g => g.AccountId)
                .IsRequired();
            modelBuilder.Entity<Account>()
                .HasMany(g => g.Bookings)
                .WithOne(g => g.Account)
                .HasForeignKey(g => g.AccountId)
                .IsRequired();
            modelBuilder.Entity<AccountTransaction>()
                .HasOne(g => g.Account)
                .WithMany(g => g.AccountTransactions)
                .HasForeignKey(g => g.AccountId)
                .IsRequired();
        }
    }

}
