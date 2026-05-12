using Microsoft.EntityFrameworkCore;
using weddingapporg.Models;

namespace weddingapporg.Data
{
    /// <summary>
    /// Seeds sample services into the database at startup.
    /// Only inserts services that don't already exist (by Service_Name).
    /// Venues (type=1) are priced per day (full-event flat fee).
    /// Additional services (types 2–7) are priced per hour.
    /// </summary>
    public static class DataSeeder
    {
        public static async Task SeedServicesAsync(ApplicationDbContext db)
        {
            var services = BuildSeedData();

            // Insert only services whose names aren't already in the DB
            var existingNames = await db.Services
                .Select(s => s.Service_Name)
                .ToListAsync();

            var toInsert = services
                .Where(s => !existingNames.Contains(s.Service_Name))
                .ToList();

            if (toInsert.Count == 0)
                return;

            await db.Services.AddRangeAsync(toInsert);
            await db.SaveChangesAsync();
        }

        private static List<Service> BuildSeedData() => new()
        {
            // ══════════════════════════════════════════════════════════════
            //  VENUES  (ServiceType = 1) — Priced per DAY (full event)
            // ══════════════════════════════════════════════════════════════

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 1,
                Service_Name = "The Grand Pavilion",
                Price       = 8500.00m,   // per day / event
                Capacity    = 500,
                City        = "Dubai",
                Address     = "Downtown Dubai, Sheikh Mohammed Blvd",
                MainImage   = "/imgs/unnamed (6).png",
                Description = "An architectural masterpiece set in the heart of Downtown Dubai. " +
                              "Combining neo-classical grandeur with modern editorial touches, " +
                              "The Grand Pavilion offers a breathtaking backdrop for up to 500 guests. " +
                              "Price is charged per event day and includes full venue access, " +
                              "valet parking, and a dedicated event coordinator."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 1,
                Service_Name = "L'Orangerie Sanctuary",
                Price       = 6200.00m,
                Capacity    = 300,
                City        = "Abu Dhabi",
                Address     = "Yas Island, Corniche Road",
                MainImage   = "/imgs/unnamed (8).png",
                Description = "A romantic garden-style venue nestled on Yas Island, " +
                              "designed for intimate celebrations of up to 300 guests. " +
                              "Lush greenery, ambient lighting, and a stunning waterfront view " +
                              "make this venue unforgettable. Full-day pricing includes setup and teardown."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 1,
                Service_Name = "Cote d'Azur Terrace",
                Price       = 5500.00m,
                Capacity    = 200,
                City        = "Sharjah",
                Address     = "Al Mamzar Beach Park, East Wing",
                MainImage   = "/imgs/unnamed (7).png",
                Description = "A sophisticated open-air terrace overlooking the Arabian Gulf. " +
                              "With a Mediterranean-inspired design, this venue accommodates up to 200 guests " +
                              "and provides exclusive access to the beachfront. " +
                              "Per-day pricing covers the full event duration from setup to midnight."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 1,
                Service_Name = "The Pearl Ballroom",
                Price       = 11000.00m,
                Capacity    = 800,
                City        = "Dubai",
                Address     = "Palm Jumeirah, Atlantis Hotel",
                MainImage   = "/imgs/unnamed (11).png",
                Description = "Our most prestigious venue, set inside the iconic Atlantis Hotel on Palm Jumeirah. " +
                              "The Pearl Ballroom features crystal chandeliers, gold-leaf ceilings, " +
                              "and a state-of-the-art sound system. Accommodates up to 800 guests. " +
                              "Full-day hire includes a pre-event cocktail reception space."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 1,
                Service_Name = "Desert Rose Estate",
                Price       = 4800.00m,
                Capacity    = 150,
                City        = "Al Ain",
                Address     = "Al Qattara Arts Centre Vicinity",
                MainImage   = "/imgs/unnamed (9).png",
                Description = "A charming desert estate perfect for intimate weddings. " +
                              "Surrounded by date palms and traditional Emirati architecture, " +
                              "Desert Rose Estate offers a unique cultural setting for up to 150 guests. " +
                              "Per-day pricing includes ambient desert lighting and traditional decor elements."
            },

            // ══════════════════════════════════════════════════════════════
            //  CATERING  (ServiceType = 2) — Priced per HOUR
            // ══════════════════════════════════════════════════════════════

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 2,
                Service_Name = "Maison Elite Catering",
                Price       = 450.00m,   // per hour
                Capacity    = null,
                City        = "Dubai",
                Address     = "Business Bay, Al Asayel St",
                MainImage   = "/imgs/unnamed (5).png",
                Description = "Michelin-inspired fine dining service with a team of professional chefs. " +
                              "We craft bespoke tasting menus using seasonal ingredients and avant-garde presentation. " +
                              "Hourly rate covers setup, live cooking stations, and full service staff for up to 50 guests per hour."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 2,
                Service_Name = "Arabian Feast Co.",
                Price       = 280.00m,
                Capacity    = null,
                City        = "Sharjah",
                Address     = "Al Nahda District",
                MainImage   = "/imgs/unnamed (2).png",
                Description = "Authentic Emirati and Pan-Arabian cuisine for large celebrations. " +
                              "Specialising in traditional mezze spreads, slow-roasted lamb, " +
                              "and elaborate dessert stations. Per-hour rate includes all service staff and equipment."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 2,
                Service_Name = "The Artisan Bar & Mixology",
                Price       = 220.00m,
                Capacity    = null,
                City        = "Dubai",
                Address     = "DIFC, Gate Avenue",
                MainImage   = "/imgs/unnamed (3).png",
                Description = "Signature cocktail bars and mocktail experiences featuring rare botanicals and exotic fruits. " +
                              "Our award-winning mixologists create custom signature drinks named after the wedding couple. " +
                              "Hourly rate includes 2 bartenders and a full bar setup."
            },

            // ══════════════════════════════════════════════════════════════
            //  PHOTOGRAPHY  (ServiceType = 3) — Priced per HOUR
            // ══════════════════════════════════════════════════════════════

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 3,
                Service_Name = "Golden Hour Studios",
                Price       = 350.00m,
                Capacity    = null,
                City        = "Dubai",
                Address     = "Al Quoz Creative District",
                MainImage   = "/imgs/unnamed (10).png",
                Description = "Award-winning wedding photography studio specialising in cinematic storytelling. " +
                              "Our team of 2 photographers capture every candid moment and editorial portrait. " +
                              "Per-hour rate includes unlimited shots, a same-day sneak peek, and an edited gallery delivered within 2 weeks."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 3,
                Service_Name = "Moments & Motion Films",
                Price       = 400.00m,
                Capacity    = null,
                City        = "Abu Dhabi",
                Address     = "Saadiyat Island Cultural District",
                MainImage   = "/imgs/unnamed (1).png",
                Description = "Cinematic wedding videography with drone aerial coverage. " +
                              "We produce feature-length films and short highlight reels that tell your love story. " +
                              "Hourly rate covers a full crew of 3, including a drone operator."
            },

            // ══════════════════════════════════════════════════════════════
            //  DECORATION  (ServiceType = 4) — Priced per HOUR
            // ══════════════════════════════════════════════════════════════

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 4,
                Service_Name = "Bloom & Design Studio",
                Price       = 180.00m,
                Capacity    = null,
                City        = "Dubai",
                Address     = "Jumeirah, Al Wasl Road",
                MainImage   = "/imgs/unnamed (4).png",
                Description = "Transforming wedding spaces into ethereal landscapes with the finest imported florals. " +
                              "From ceiling installations to table centrepieces and ceremony arches, " +
                              "our designers create immersive sensory environments. " +
                              "Charged per decoration/setup hour, with a minimum booking of 4 hours."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 4,
                Service_Name = "Lumiere Lighting & Decor",
                Price       = 220.00m,
                Capacity    = null,
                City        = "Sharjah",
                Address     = "Al Khan Waterfront",
                MainImage   = "/imgs/unnamed (3).png",
                Description = "Specialist in dramatic lighting design, neon signage, and custom decor fabrication. " +
                              "We handle fairy-light canopies, LED dance floors, monogram projections, " +
                              "and ambient uplighting. Per-hour rate covers installation and full event monitoring."
            },

            // ══════════════════════════════════════════════════════════════
            //  MUSIC & ENTERTAINMENT  (ServiceType = 5) — Priced per HOUR
            // ══════════════════════════════════════════════════════════════

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 5,
                Service_Name = "The Velvet Orchestra",
                Price       = 600.00m,
                Capacity    = null,
                City        = "Dubai",
                Address     = "Opera District, Downtown",
                MainImage   = "/imgs/unnamed (8).png",
                Description = "A 12-piece live orchestra performing classical, jazz, and contemporary wedding favourites. " +
                              "From the ceremony processional to the first dance, The Velvet Orchestra delivers an unforgettable experience. " +
                              "Per-hour rate covers the full ensemble, a sound engineer, and PA system."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 5,
                Service_Name = "DJ Luxe Entertainment",
                Price       = 300.00m,
                Capacity    = null,
                City        = "Dubai",
                Address     = "Dubai Marina, The Walk",
                MainImage   = "/imgs/unnamed (9).png",
                Description = "Dubai's premier wedding DJ service, specialising in seamless transitions from ceremony to reception. " +
                              "We provide a professional sound and lighting rig, MC services, and custom playlist curation. " +
                              "Hourly rate includes full setup and a 30-minute pre-event soundcheck."
            },

            // ══════════════════════════════════════════════════════════════
            //  TRANSPORTATION  (ServiceType = 6) — Priced per HOUR
            // ══════════════════════════════════════════════════════════════

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 6,
                Service_Name = "Royal Ride Luxury Transfers",
                Price       = 150.00m,
                Capacity    = 4,
                City        = "Dubai",
                Address     = "Al Barsha, Sheikh Zayed Road",
                MainImage   = "/imgs/unnamed (7).png",
                Description = "Chauffeured Rolls-Royce, Bentley, and Mercedes-Maybach fleet for the wedding party. " +
                              "Each vehicle is professionally decorated with white ribbon and fresh florals. " +
                              "Per-hour rate per vehicle (minimum 3 hours). Includes a uniformed chauffeur and complimentary beverages."
            },

            new Service
            {
                Id          = Guid.NewGuid(),
                ServiceType = 6,
                Service_Name = "Grand Coach Shuttle Service",
                Price       = 200.00m,
                Capacity    = 50,
                City        = "Abu Dhabi",
                Address     = "Khalidiyah Area, Hamdan Street",
                MainImage   = "/imgs/unnamed (6).png",
                Description = "Luxury coach transfers for wedding guests between hotels, ceremony, and reception venues. " +
                              "Our coaches seat up to 50 and feature climate control, USB charging, and ambient lighting. " +
                              "Charged per hour per coach — ideal for large guest lists requiring coordinated transport."
            },
        };
    }
}
