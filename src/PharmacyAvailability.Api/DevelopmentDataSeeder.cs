using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Api;

/// <summary>
/// Seeds the database with realistic test data for development and manual testing.
/// All operations are idempotent — safe to run on every startup.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(PharmacyDbContext db, IPasswordHashService passwordHasher, ILogger logger)
    {
        logger.LogInformation("DevelopmentDataSeeder: Starting seed check...");

        // ──────────────────────────────────────────
        //  1. Categories
        // ──────────────────────────────────────────
        if (!await db.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category("Analgesics & Antipyretics", "Pain relievers and fever reducers"),
                new Category("Antibiotics", "Anti-bacterial medications"),
                new Category("Cardiovascular", "Heart and blood pressure medications"),
                new Category("Gastrointestinal", "Digestive system medications"),
                new Category("Respiratory", "Cough, cold, and asthma medications"),
                new Category("Dermatology", "Skin care and treatment medications"),
                new Category("Vitamins & Supplements", "Nutritional supplements and vitamins"),
                new Category("Diabetes", "Blood sugar management medications"),
                new Category("Anti-Inflammatory", "NSAIDs and corticosteroids"),
                new Category("Allergy & Immunology", "Antihistamines and allergy treatments"),
            };
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
            logger.LogInformation("DevelopmentDataSeeder: Seeded {Count} categories.", categories.Length);
        }

        // ──────────────────────────────────────────
        //  2. Manufacturers
        // ──────────────────────────────────────────
        if (!await db.Manufacturers.AnyAsync())
        {
            var manufacturers = new[]
            {
                new Manufacturer("GlaxoSmithKline (GSK)", "United Kingdom"),
                new Manufacturer("Pfizer", "United States"),
                new Manufacturer("EVA Pharma", "Egypt"),
                new Manufacturer("Amoun Pharmaceutical", "Egypt"),
                new Manufacturer("EIPICO", "Egypt"),
                new Manufacturer("Novartis", "Switzerland"),
                new Manufacturer("Sanofi", "France"),
                new Manufacturer("AstraZeneca", "United Kingdom"),
                new Manufacturer("Hikma Pharmaceuticals", "Jordan"),
                new Manufacturer("Medical Union Pharmaceuticals (MUP)", "Egypt"),
            };
            db.Manufacturers.AddRange(manufacturers);
            await db.SaveChangesAsync();
            logger.LogInformation("DevelopmentDataSeeder: Seeded {Count} manufacturers.", manufacturers.Length);
        }

        // ──────────────────────────────────────────
        //  3. Medicines
        // ──────────────────────────────────────────
        if (!await db.Medicines.AnyAsync())
        {
            var cats = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
            var mfrs = await db.Manufacturers.ToDictionaryAsync(m => m.Name, m => m.Id);

            var medicines = new (string Brand, string Generic, string Active, string Strength, string Form, string Cat, string Mfr, string? Desc, bool Rx)[]
            {
                ("Panadol", "Paracetamol", "Paracetamol", "500mg", "Tablet", "Analgesics & Antipyretics", "GlaxoSmithKline (GSK)", "Pain reliever and fever reducer", false),
                ("Panadol Extra", "Paracetamol/Caffeine", "Paracetamol + Caffeine", "500mg/65mg", "Tablet", "Analgesics & Antipyretics", "GlaxoSmithKline (GSK)", "Enhanced pain relief with caffeine", false),
                ("Brufen", "Ibuprofen", "Ibuprofen", "400mg", "Tablet", "Anti-Inflammatory", "Hikma Pharmaceuticals", "Anti-inflammatory pain reliever", false),
                ("Augmentin", "Amoxicillin/Clavulanic Acid", "Amoxicillin + Clavulanic Acid", "1g", "Tablet", "Antibiotics", "GlaxoSmithKline (GSK)", "Broad-spectrum antibiotic", true),
                ("Augmentin", "Amoxicillin/Clavulanic Acid", "Amoxicillin + Clavulanic Acid", "625mg", "Tablet", "Antibiotics", "GlaxoSmithKline (GSK)", "Broad-spectrum antibiotic", true),
                ("Amoxil", "Amoxicillin", "Amoxicillin", "500mg", "Capsule", "Antibiotics", "Pfizer", "Penicillin-type antibiotic", true),
                ("Concor", "Bisoprolol", "Bisoprolol Fumarate", "5mg", "Tablet", "Cardiovascular", "Novartis", "Beta-blocker for hypertension", true),
                ("Concor", "Bisoprolol", "Bisoprolol Fumarate", "10mg", "Tablet", "Cardiovascular", "Novartis", "Beta-blocker for hypertension", true),
                ("Nexium", "Esomeprazole", "Esomeprazole Magnesium", "40mg", "Capsule", "Gastrointestinal", "AstraZeneca", "Proton pump inhibitor for acid reflux", true),
                ("Antinal", "Nifuroxazide", "Nifuroxazide", "200mg", "Capsule", "Gastrointestinal", "Amoun Pharmaceutical", "Intestinal antiseptic for diarrhea", false),
                ("Ventolin", "Salbutamol", "Salbutamol Sulfate", "100mcg", "Inhaler", "Respiratory", "GlaxoSmithKline (GSK)", "Bronchodilator for asthma relief", true),
                ("Telfast", "Fexofenadine", "Fexofenadine HCl", "180mg", "Tablet", "Allergy & Immunology", "Sanofi", "Non-drowsy antihistamine", false),
                ("Glucophage", "Metformin", "Metformin HCl", "500mg", "Tablet", "Diabetes", "Novartis", "First-line medication for type 2 diabetes", true),
                ("Glucophage", "Metformin", "Metformin HCl", "1000mg", "Tablet", "Diabetes", "Novartis", "First-line medication for type 2 diabetes", true),
                ("Cataflam", "Diclofenac Potassium", "Diclofenac Potassium", "50mg", "Tablet", "Anti-Inflammatory", "Novartis", "Fast-acting anti-inflammatory", false),
                ("Vitamin C (EVA)", "Ascorbic Acid", "Ascorbic Acid", "1000mg", "Effervescent Tablet", "Vitamins & Supplements", "EVA Pharma", "Immune system support", false),
                ("Omega-3 Plus", "Fish Oil", "Omega-3 Fatty Acids (EPA/DHA)", "1000mg", "Softgel", "Vitamins & Supplements", "EVA Pharma", "Heart and brain health supplement", false),
                ("Fusiderm", "Fusidic Acid", "Fusidic Acid", "2%", "Cream", "Dermatology", "EVA Pharma", "Topical antibiotic cream for skin infections", false),
                ("Congestal", "Paracetamol/Pseudoephedrine/Chlorpheniramine", "Paracetamol + Pseudoephedrine + Chlorpheniramine", "450mg/30mg/2mg", "Tablet", "Respiratory", "Amoun Pharmaceutical", "Cold and flu symptom relief", false),
                ("Aspocid", "Acetylsalicylic Acid", "Aspirin", "75mg", "Tablet", "Cardiovascular", "EIPICO", "Low-dose aspirin for cardiovascular protection", false),
            };

            foreach (var (brand, generic, active, strength, form, cat, mfr, desc, rx) in medicines)
            {
                // Handle duplicate brand names by appending strength to make them unique in the collection
                var med = new Medicine(brand, generic, active, strength, form, cats[cat], mfrs[mfr], desc, rx);
                med.AddBarcode($"6221{Random.Shared.Next(100000000, 999999999)}", isPrimary: true);
                if (generic != brand)
                    med.AddAlias(generic);

                db.Medicines.Add(med);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("DevelopmentDataSeeder: Seeded {Count} medicines.", medicines.Length);
        }

        // ──────────────────────────────────────────
        //  4. Pharmacies & Branches
        // ──────────────────────────────────────────
        if (!await db.Pharmacies.AnyAsync())
        {
            // Pharmacy 1: El-Ezaby
            var ezaby = new Pharmacy(
                "El-Ezaby Pharmacy",
                "LIC-EZ-2024-001",
                "15 Tahrir Street, Downtown",
                "Cairo",
                new Coordinates(30.0444, 31.2357),
                "+20-2-2574-1234",
                "info@elezaby.com",
                "08:00 AM - 12:00 AM",
                PharmacyStatus.Active);

            var ezabyBranch1 = ezaby.AddBranch(
                "El-Ezaby - Tahrir",
                "15 Tahrir Street, Downtown",
                "Cairo",
                new Coordinates(30.0444, 31.2357),
                "+20-2-2574-1234",
                "08:00 AM - 12:00 AM",
                "EZ-TAH-01");

            var ezabyBranch2 = ezaby.AddBranch(
                "El-Ezaby - Maadi",
                "42 Road 9, Maadi",
                "Cairo",
                new Coordinates(29.9602, 31.2569),
                "+20-2-2359-5678",
                "09:00 AM - 11:00 PM",
                "EZ-MAD-02");

            var ezabyBranch3 = ezaby.AddBranch(
                "El-Ezaby - Heliopolis",
                "8 Beirut Street, Heliopolis",
                "Cairo",
                new Coordinates(30.0911, 31.3225),
                "+20-2-2419-9012",
                "08:00 AM - 11:00 PM",
                "EZ-HEL-03");

            db.Pharmacies.Add(ezaby);

            // Pharmacy 2: Seif Pharmacy
            var seif = new Pharmacy(
                "Seif Pharmacy",
                "LIC-SF-2024-002",
                "78 El-Nozha Street, Heliopolis",
                "Cairo",
                new Coordinates(30.0873, 31.3381),
                "+20-2-2636-4321",
                "contact@seifpharmacy.com",
                "24 Hours",
                PharmacyStatus.Active);

            var seifBranch1 = seif.AddBranch(
                "Seif - Heliopolis",
                "78 El-Nozha Street, Heliopolis",
                "Cairo",
                new Coordinates(30.0873, 31.3381),
                "+20-2-2636-4321",
                "24 Hours",
                "SF-HEL-01");

            var seifBranch2 = seif.AddBranch(
                "Seif - Nasr City",
                "35 Makram Ebeid, Nasr City",
                "Cairo",
                new Coordinates(30.0511, 31.3456),
                "+20-2-2274-8765",
                "08:00 AM - 01:00 AM",
                "SF-NSR-02");

            db.Pharmacies.Add(seif);

            // Pharmacy 3: Pending pharmacy (for admin testing)
            var pending = new Pharmacy(
                "New Cairo Pharmacy",
                "LIC-NC-2024-003",
                "First Settlement, New Cairo",
                "Cairo",
                new Coordinates(30.0302, 31.4694),
                "+20-10-1234-5678",
                "newcairo@pharmacy.com",
                "09:00 AM - 10:00 PM",
                PharmacyStatus.Pending);

            pending.AddBranch(
                "New Cairo - Main",
                "First Settlement, New Cairo",
                "Cairo",
                new Coordinates(30.0302, 31.4694),
                "+20-10-1234-5678",
                "09:00 AM - 10:00 PM",
                "NC-MAIN-01");

            db.Pharmacies.Add(pending);

            await db.SaveChangesAsync();
            logger.LogInformation("DevelopmentDataSeeder: Seeded 3 pharmacies with branches.");

            // ──────────────────────────────────────────
            //  5. Inventory (stock for the active pharmacies)
            // ──────────────────────────────────────────
            var allMedicines = await db.Medicines.ToListAsync();

            var activeBranches = new[] { ezabyBranch1, ezabyBranch2, ezabyBranch3, seifBranch1, seifBranch2 };
            var pharmacyIds = new Dictionary<Guid, Guid>
            {
                [ezabyBranch1.Id] = ezaby.Id,
                [ezabyBranch2.Id] = ezaby.Id,
                [ezabyBranch3.Id] = ezaby.Id,
                [seifBranch1.Id] = seif.Id,
                [seifBranch2.Id] = seif.Id,
            };

            var rng = new Random(42); // Fixed seed for reproducibility

            var inventoryCount = 0;
            foreach (var branch in activeBranches)
            {
                // Each branch stocks 60-100% of medicines
                var branchMedicines = allMedicines
                    .Where(_ => rng.NextDouble() > 0.15) // ~85% chance to stock each
                    .ToList();

                foreach (var med in branchMedicines)
                {
                    // Vary quantity: some low stock, some well-stocked
                    var quantity = rng.Next(0, 200);
                    // Vary price slightly between branches
                    var basePrice = GetBasePrice(med.BrandName, rng);
                    var price = Math.Round(basePrice * (decimal)(0.95 + rng.NextDouble() * 0.10), 2);

                    var inventory = new PharmacyInventory(
                        pharmacyIds[branch.Id],
                        branch.Id,
                        med.Id,
                        quantity,
                        price);

                    db.PharmacyInventories.Add(inventory);
                    inventoryCount++;
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("DevelopmentDataSeeder: Seeded {Count} inventory records across {BranchCount} branches.",
                inventoryCount, activeBranches.Length);
        }

        // ──────────────────────────────────────────
        //  6. User Accounts
        // ──────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.PlatformAdmin))
        {
            var pharmacies = await db.Pharmacies
                .Where(p => p.Status == PharmacyStatus.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var dummyUser = new User("dummy@temp.com", "temp", "Dummy", UserRole.Customer, "+20-10-0000-0000");

            // Platform Admin
            var adminHash = passwordHasher.HashPassword(dummyUser, "Admin123!");
            var admin = new User("admin@pharma.com", adminHash, "Platform Administrator", UserRole.PlatformAdmin, "+20-10-0000-0001");
            admin.SetVerificationStatus(true, true);
            db.Users.Add(admin);

            // Pharmacy Admin for El-Ezaby
            var ezaby = pharmacies.FirstOrDefault(p => p.Name.Contains("Ezaby"));
            if (ezaby != null)
            {
                var pharmacyAdminHash = passwordHasher.HashPassword(dummyUser, "Pharmacy123!");
                var pharmacyAdmin = new User("ezaby.admin@pharma.com", pharmacyAdminHash, "Ahmed El-Ezaby", UserRole.PharmacyAdmin, "+20-10-0000-0002", ezaby.Id);
                pharmacyAdmin.SetVerificationStatus(true, true);
                db.Users.Add(pharmacyAdmin);

                // Staff member for El-Ezaby
                var staffHash = passwordHasher.HashPassword(dummyUser, "Staff123!");
                var staff = new User("ezaby.staff@pharma.com", staffHash, "Mohamed Hassan", UserRole.PharmacyStaff, "+20-10-0000-0003", ezaby.Id);
                staff.SetVerificationStatus(true, true);
                db.Users.Add(staff);
            }

            // Pharmacy Admin for Seif
            var seif = pharmacies.FirstOrDefault(p => p.Name.Contains("Seif"));
            if (seif != null)
            {
                var seifAdminHash = passwordHasher.HashPassword(dummyUser, "Pharmacy123!");
                var seifAdmin = new User("seif.admin@pharma.com", seifAdminHash, "Sara Seif", UserRole.PharmacyAdmin, "+20-10-0000-0004", seif.Id);
                seifAdmin.SetVerificationStatus(true, true);
                db.Users.Add(seifAdmin);
            }

            // Test Customer
            var customerHash = passwordHasher.HashPassword(dummyUser, "Customer123!");
            var customer = new User("customer@test.com", customerHash, "Test Customer", UserRole.Customer, "+20-10-0000-0005");
            customer.SetVerificationStatus(true, true);
            db.Users.Add(customer);

            await db.SaveChangesAsync();
            logger.LogInformation("DevelopmentDataSeeder: Seeded user accounts (admin, pharmacy staff, customer).");
        }
        else
        {
            // Ensure any pre-existing seeded admin/pharmacy/test users are verified
            var unverifiedSeededUsers = await db.Users
                .Where(u => (!u.IsEmailVerified || !u.IsPhoneVerified) &&
                            (u.Email == "admin@pharma.com" ||
                             u.Email == "ezaby.admin@pharma.com" ||
                             u.Email == "ezaby.staff@pharma.com" ||
                             u.Email == "seif.admin@pharma.com" ||
                             u.Email == "customer@test.com"))
                .ToListAsync();

            if (unverifiedSeededUsers.Any())
            {
                foreach (var user in unverifiedSeededUsers)
                {
                    user.SetVerificationStatus(true, true);
                }
                await db.SaveChangesAsync();
                logger.LogInformation("DevelopmentDataSeeder: Updated {Count} pre-existing seeded users to verified.", unverifiedSeededUsers.Count);
            }
        }

        logger.LogInformation("DevelopmentDataSeeder: Seed check complete.");
    }

    private static decimal GetBasePrice(string brandName, Random rng)
    {
        return brandName switch
        {
            "Panadol" => 25.00m,
            "Panadol Extra" => 35.00m,
            "Brufen" => 30.00m,
            "Augmentin" => 95.00m,
            "Amoxil" => 55.00m,
            "Concor" => 75.00m,
            "Nexium" => 120.00m,
            "Antinal" => 18.00m,
            "Ventolin" => 65.00m,
            "Telfast" => 85.00m,
            "Glucophage" => 40.00m,
            "Cataflam" => 28.00m,
            "Vitamin C (EVA)" => 50.00m,
            "Omega-3 Plus" => 110.00m,
            "Fusiderm" => 45.00m,
            "Congestal" => 22.00m,
            "Aspocid" => 15.00m,
            _ => 30.00m + rng.Next(0, 80),
        };
    }
}
