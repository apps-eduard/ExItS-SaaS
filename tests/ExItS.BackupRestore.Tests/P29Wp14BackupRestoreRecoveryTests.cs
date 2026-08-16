using System.Diagnostics;
using System.Globalization;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ExItS.BackupRestore.Tests;

/// <summary>P29-WP14: richer Platform+POS backup → clean restore, older-dump upgrade, write smoke, archive inspect.</summary>
public sealed class P29Wp14BackupRestoreRecoveryTests
{
    private const string PostgresImage = "postgres:16";

    /// <summary>Migration immediately before HardenElectronicSalePaymentReservation (sales.stock_reservation_state).</summary>
    private const string PosMigrationBeforeSaleStockReservation =
        "20260816121841_StrengthenCustomerOrderLineTenantForeignKeys";

    [Fact]
    public async Task A_Current_backup_restore_preserves_platform_and_pos_fingerprints()
    {
        await using var platformSource = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_platform")
            .Build();
        await using var platformTarget = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_platform")
            .Build();
        await using var posSource = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();
        await using var posTarget = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();

        await Task.WhenAll(
            platformSource.StartAsync(),
            platformTarget.StartAsync(),
            posSource.StartAsync(),
            posTarget.StartAsync());

        var platformSourceCs = platformSource.GetConnectionString();
        var platformTargetCs = platformTarget.GetConnectionString();
        var posSourceCs = posSource.GetConnectionString();
        var posTargetCs = posTarget.GetConnectionString();

        await using (var db = new PlatformDbContext(
                         new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(platformSourceCs).Options))
        {
            await db.Database.MigrateAsync();
        }

        await using (var db = new PosDbContext(
                         new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(posSourceCs).Options))
        {
            await db.Database.MigrateAsync();
        }

        var platformSeed = await SeedPlatformLatestAsync(platformSourceCs);
        var posSeed = await SeedPosLatestAsync(posSourceCs);

        var outDir = Path.Combine(Path.GetTempPath(), "exits-p29-wp14-a", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var service = new PostgreSqlBackupService();
            var platformBackup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.Platform,
                platformSourceCs,
                outDir,
                "Testing",
                DockerContainerId: platformSource.Id));
            var posBackup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                posSourceCs,
                outDir,
                "Testing",
                DockerContainerId: posSource.Id));

            Assert.True(platformBackup.Manifest.ArtifactSizeBytes > 0);
            Assert.True(posBackup.Manifest.ArtifactSizeBytes > 0);
            await PostgreSqlBackupService.VerifyArtifactAsync(platformBackup.ArtifactPath, platformBackup.ManifestPath);
            await PostgreSqlBackupService.VerifyArtifactAsync(posBackup.ArtifactPath, posBackup.ManifestPath);

            var platformRestored = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.Platform,
                platformTargetCs,
                platformBackup.ArtifactPath,
                platformBackup.ManifestPath,
                DockerContainerId: platformTarget.Id));
            Assert.True(platformRestored.Succeeded, platformRestored.Message);

            var posRestored = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                posTargetCs,
                posBackup.ArtifactPath,
                posBackup.ManifestPath,
                DockerContainerId: posTarget.Id));
            Assert.True(posRestored.Succeeded, posRestored.Message);

            var platformValidation = await RestoreValidator.ValidatePlatformAsync(
                platformTargetCs,
                new Dictionary<string, long>
                {
                    ["organizations"] = 2,
                    ["organization_branches"] = 2,
                    ["branch_delivery_policies"] = 1
                },
                requirePhase29Tables: true,
                checkPhase29ConstraintsBestEffort: true);
            Assert.True(platformValidation.Passed, string.Join("; ", platformValidation.Findings));

            var platformConstraints = await RestoreValidator.EnsureNamedConstraintsBestEffortAsync(
                platformTargetCs,
                "platform",
                RestoreValidator.PlatformPhase29ConstraintNames,
                require: true);
            Assert.True(platformConstraints.Passed, string.Join("; ", platformConstraints.Findings));

            var posValidation = await RestoreValidator.ValidatePosAsync(
                posTargetCs,
                new Dictionary<string, long>
                {
                    ["products"] = 2,
                    ["sales"] = 1,
                    ["sale_lines"] = 1,
                    ["stock_movements"] = 1,
                    ["customer_orders"] = 1,
                    ["customer_order_lines"] = 1,
                    ["payment_attempts"] = 1,
                    ["inventory_accounts"] = 2
                },
                requirePhase29Tables: true,
                checkPhase29ConstraintsBestEffort: true,
                validateInventoryReservations: true);
            Assert.True(posValidation.Passed, string.Join("; ", posValidation.Findings));

            var posConstraints = await RestoreValidator.EnsureNamedConstraintsBestEffortAsync(
                posTargetCs,
                "pos",
                RestoreValidator.PosPhase29ConstraintNames,
                require: true);
            Assert.True(posConstraints.Passed, string.Join("; ", posConstraints.Findings));

            var fingerprints = new List<CriticalRecordFingerprint>
            {
                new(
                    "platform",
                    "organizations",
                    platformSeed.Org1Id,
                    new Dictionary<string, object?>
                    {
                        ["display_name"] = "P29 WP14 Org One",
                        ["slug"] = platformSeed.Org1Slug
                    }),
                new(
                    "platform",
                    "organizations",
                    platformSeed.Org2Id,
                    new Dictionary<string, object?>
                    {
                        ["display_name"] = "P29 WP14 Org Two",
                        ["slug"] = platformSeed.Org2Slug
                    }),
                new(
                    "platform",
                    "organization_branches",
                    platformSeed.Branch1Id,
                    new Dictionary<string, object?>
                    {
                        ["organization_id"] = platformSeed.Org1Id,
                        ["code"] = "MAIN"
                    }),
                new(
                    "pos",
                    "sales",
                    posSeed.SaleId,
                    new Dictionary<string, object?>
                    {
                        ["organization_id"] = posSeed.Org1Id,
                        ["sale_number"] = "P29-WP14-SALE-1",
                        ["stock_reservation_state"] = "None"
                    }),
                new(
                    "pos",
                    "customer_orders",
                    posSeed.OrderId,
                    new Dictionary<string, object?>
                    {
                        ["seller_organization_id"] = posSeed.Org1Id,
                        ["order_number"] = "P29-WP14-CO-1",
                        ["stock_reservation_state"] = "Reserved",
                        ["total"] = 120.00m
                    }),
                new(
                    "pos",
                    "inventory_accounts",
                    posSeed.Inventory1Id,
                    new Dictionary<string, object?>
                    {
                        ["organization_id"] = posSeed.Org1Id,
                        ["on_hand_quantity"] = 90m,
                        ["reserved_quantity"] = 5m
                    })
            };

            var platformFp = await RestoreValidator.CompareCriticalFingerprintsAsync(
                platformTargetCs,
                fingerprints.Where(f => f.Schema == "platform").ToList());
            Assert.True(platformFp.Passed, string.Join("; ", platformFp.Findings));

            var posFp = await RestoreValidator.CompareCriticalFingerprintsAsync(
                posTargetCs,
                fingerprints.Where(f => f.Schema == "pos").ToList());
            Assert.True(posFp.Passed, string.Join("; ", posFp.Findings));

            await AssertTenantOrgIdsPreservedAsync(posTargetCs, posSeed.Org1Id, posSeed.Org2Id);
            await AssertNoNegativeInventoryAsync(posTargetCs);

            // Application-layer readback: EF Core opens restored DBs, migrates as no-op, queries known rows.
            await AssertPlatformEfReadbackAsync(platformTargetCs, platformSeed);
            await AssertPosEfReadbackAsync(posTargetCs, posSeed);
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    [Fact]
    public async Task B_Older_pos_backup_restore_then_migrate_to_latest()
    {
        await using var source = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();
        await using var target = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();
        await source.StartAsync();
        await target.StartAsync();

        var sourceCs = source.GetConnectionString();
        var targetCs = target.GetConnectionString();

        await using (var db = new PosDbContext(
                         new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(sourceCs).Options))
        {
            await db.Database.MigrateAsync(PosMigrationBeforeSaleStockReservation);
        }

        var seed = await SeedPosCompatibleWithPreSaleReservationAsync(sourceCs);

        var outDir = Path.Combine(Path.GetTempPath(), "exits-p29-wp14-b", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var service = new PostgreSqlBackupService();
            var backup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                sourceCs,
                outDir,
                "Testing",
                MigrationSchemaVersion: PosMigrationBeforeSaleStockReservation,
                DockerContainerId: source.Id));
            await PostgreSqlBackupService.VerifyArtifactAsync(backup.ArtifactPath, backup.ManifestPath);

            var restored = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                targetCs,
                backup.ArtifactPath,
                backup.ManifestPath,
                DockerContainerId: target.Id));
            Assert.True(restored.Succeeded, restored.Message);

            Assert.False(await ColumnExistsAsync(targetCs, "pos", "sales", "stock_reservation_state"));

            await using (var db = new PosDbContext(
                             new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(targetCs).Options))
            {
                await db.Database.MigrateAsync();
            }

            Assert.True(await ColumnExistsAsync(targetCs, "pos", "sales", "stock_reservation_state"));

            var fp = await RestoreValidator.CompareCriticalFingerprintsAsync(
                targetCs,
                [
                    new(
                        "pos",
                        "customers",
                        seed.CustomerId,
                        new Dictionary<string, object?>
                        {
                            ["organization_id"] = seed.OrgId,
                            ["display_name"] = "P29 WP14 Legacy Customer"
                        }),
                    new(
                        "pos",
                        "products",
                        seed.ProductId,
                        new Dictionary<string, object?>
                        {
                            ["organization_id"] = seed.OrgId,
                            ["name"] = "P29 WP14 Legacy Product"
                        }),
                    new(
                        "pos",
                        "customer_orders",
                        seed.OrderId,
                        new Dictionary<string, object?>
                        {
                            ["seller_organization_id"] = seed.OrgId,
                            ["order_number"] = "P29-LEGACY-CO1"
                        })
                ]);
            Assert.True(fp.Passed, string.Join("; ", fp.Findings));
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    [Fact]
    public async Task C_Post_restore_write_smoke_inserts_customer()
    {
        await using var source = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();
        await using var target = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();
        await source.StartAsync();
        await target.StartAsync();

        var sourceCs = source.GetConnectionString();
        var targetCs = target.GetConnectionString();

        await using (var db = new PosDbContext(
                         new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(sourceCs).Options))
        {
            await db.Database.MigrateAsync();
        }

        var orgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await SeedPosCustomerAsync(sourceCs, orgId, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Seed Customer");

        var outDir = Path.Combine(Path.GetTempPath(), "exits-p29-wp14-c", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var service = new PostgreSqlBackupService();
            var backup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                sourceCs,
                outDir,
                "Testing",
                DockerContainerId: source.Id));
            await PostgreSqlBackupService.VerifyArtifactAsync(backup.ArtifactPath, backup.ManifestPath);

            var restored = await service.RestoreAsync(new RestoreRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                targetCs,
                backup.ArtifactPath,
                backup.ManifestPath,
                DockerContainerId: target.Id));
            Assert.True(restored.Succeeded, restored.Message);

            var newCustomerId = Guid.NewGuid();
            await SeedPosCustomerAsync(targetCs, orgId, newCustomerId, "Post Restore Write Smoke");

            await using var connection = new NpgsqlConnection(targetCs);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT display_name
                FROM pos.customers
                WHERE id = @id AND organization_id = @org;
                """;
            cmd.Parameters.AddWithValue("id", newCustomerId);
            cmd.Parameters.AddWithValue("org", orgId);
            var name = (string?)await cmd.ExecuteScalarAsync();
            Assert.Equal("Post Restore Write Smoke", name);
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    [Fact]
    public async Task D_Backup_artifact_verify_and_optional_pg_restore_list()
    {
        await using var source = new PostgreSqlBuilder()
            .WithImage(PostgresImage)
            .WithDatabase("exits_pos")
            .Build();
        await source.StartAsync();
        var sourceCs = source.GetConnectionString();

        await using (var db = new PosDbContext(
                         new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(sourceCs).Options))
        {
            await db.Database.MigrateAsync();
        }

        await SeedPosCustomerAsync(
            sourceCs,
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Guid.NewGuid(),
            "Archive Inspect Customer");

        var outDir = Path.Combine(Path.GetTempPath(), "exits-p29-wp14-d", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        try
        {
            var service = new PostgreSqlBackupService();
            var backup = await service.CreateBackupAsync(new BackupRequest(
                ExItsBackupDatabaseKind.PinoyBusinessPos,
                sourceCs,
                outDir,
                "Testing",
                DockerContainerId: source.Id));

            Assert.True(backup.Manifest.ArtifactSizeBytes > 0);
            Assert.True(new FileInfo(backup.ArtifactPath).Length > 0);
            await PostgreSqlBackupService.VerifyArtifactAsync(backup.ArtifactPath, backup.ManifestPath);

            // Optional: inspect custom-format TOC inside the container that produced the dump.
            var listExit = await TryDockerPgRestoreListAsync(source.Id, backup.ArtifactPath);
            if (listExit is not null)
            {
                Assert.Equal(0, listExit.Value);
            }
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, true);
            }
        }
    }

    private static async Task<int?> TryDockerPgRestoreListAsync(string containerId, string localArtifactPath)
    {
        try
        {
            var remote = "/tmp/exits-p29-wp14-list.dump";
            var copy = await RunProcessCaptureAsync("docker", ["cp", localArtifactPath, $"{containerId}:{remote}"]);
            if (copy.ExitCode != 0)
            {
                return null;
            }

            var list = await RunProcessCaptureAsync(
                "docker",
                ["exec", containerId, "pg_restore", "-l", remote]);
            return list.ExitCode;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(int ExitCode, string Stderr)> RunProcessCaptureAsync(
        string fileName,
        IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process.");
        _ = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stderr);
    }

    private sealed record PlatformSeed(
        Guid Org1Id,
        Guid Org2Id,
        string Org1Slug,
        string Org2Slug,
        Guid Branch1Id,
        Guid Branch2Id);

    private sealed record PosLatestSeed(
        Guid Org1Id,
        Guid Org2Id,
        Guid SaleId,
        Guid OrderId,
        Guid Inventory1Id,
        Guid PaymentAttemptId);

    private sealed record PosLegacySeed(
        Guid OrgId,
        Guid CustomerId,
        Guid ProductId,
        Guid OrderId);

    private static async Task<PlatformSeed> SeedPlatformLatestAsync(string connectionString)
    {
        var org1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var org2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var branch1 = Guid.Parse("33333333-3333-3333-3333-333333333331");
        var branch2 = Guid.Parse("33333333-3333-3333-3333-333333333332");
        var slug1 = "p29-wp14-o1";
        var slug2 = "p29-wp14-o2";
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecAsync(
            connection,
            """
            INSERT INTO platform.organizations (id, display_name, slug, status, created_at_utc, updated_at_utc)
            VALUES
              (@org1, 'P29 WP14 Org One', @slug1, 'Active', @now, @now),
              (@org2, 'P29 WP14 Org Two', @slug2, 'Active', @now, @now);
            """,
            ("org1", org1),
            ("org2", org2),
            ("slug1", slug1),
            ("slug2", slug2),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO platform.organization_branches
              (id, organization_id, code, name, is_primary, status, pickup_enabled, delivery_enabled, created_at_utc, updated_at_utc)
            VALUES
              (@b1, @org1, 'MAIN', 'Main One', TRUE, 'Active', TRUE, TRUE, @now, @now),
              (@b2, @org2, 'MAIN', 'Main Two', TRUE, 'Active', TRUE, TRUE, @now, @now);
            """,
            ("b1", branch1),
            ("b2", branch2),
            ("org1", org1),
            ("org2", org2),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO platform.branch_delivery_policies
              (branch_id, organization_id, minimum_order_amount, base_delivery_fee, included_distance_km,
               additional_fee_per_km, maximum_delivery_distance_km, free_delivery_threshold, created_at_utc, updated_at_utc)
            VALUES
              (@b1, @org1, 0, 50, 5, 10, 20, 500, @now, @now);
            """,
            ("b1", branch1),
            ("org1", org1),
            ("now", now));

        return new PlatformSeed(org1, org2, slug1, slug2, branch1, branch2);
    }

    private static async Task<PosLatestSeed> SeedPosLatestAsync(string connectionString)
    {
        var org1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var org2 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var product1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var product2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var inv1 = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var inv2 = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
        var saleId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1");
        var lineId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1");
        var movementId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff1");
        var orderId = Guid.Parse("12121212-1212-1212-1212-121212121212");
        var orderLineId = Guid.Parse("13131313-1313-1313-1313-131313131313");
        var attemptId = Guid.Parse("14141414-1414-1414-1414-141414141414");
        var actor = Guid.Parse("15151515-1515-1515-1515-151515151515");
        var buyer = Guid.Parse("16161616-1616-1616-1616-161616161616");
        var branchId = Guid.Parse("17171717-1717-1717-1717-171717171717");
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.products (
                id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                catalog_source, created_at_utc, updated_at_utc)
            VALUES
              (@p1, @org1, 'P29 WP14 Product One', 'Piece', 'PerItem', 25.00, 'Active', 'Manual', @now, @now),
              (@p2, @org2, 'P29 WP14 Product Two', 'Piece', 'PerItem', 40.00, 'Active', 'Manual', @now, @now);
            """,
            ("p1", product1),
            ("p2", product2),
            ("org1", org1),
            ("org2", org2),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.inventory_accounts (
                id, organization_id, product_id, is_tracked, on_hand_quantity, reserved_quantity,
                created_at_utc, updated_at_utc)
            VALUES
              (@i1, @org1, @p1, TRUE, 90, 5, @now, @now),
              (@i2, @org2, @p2, TRUE, 50, 0, @now, @now);
            """,
            ("i1", inv1),
            ("i2", inv2),
            ("org1", org1),
            ("org2", org2),
            ("p1", product1),
            ("p2", product2),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.sales (
                id, organization_id, sale_number, status, stock_reservation_state, payment_method,
                subtotal, tax_amount, total, recorded_at_utc, recorded_by, updated_at_utc, buyer_party_kind,
                amount_tendered, change_amount)
            VALUES (
                @sid, @org1, 'P29-WP14-SALE-1', 'Completed', 'None', 'Cash',
                25.00, 0, 25.00, @now, @actor, @now, 'WalkIn',
                50.00, 25.00);
            """,
            ("sid", saleId),
            ("org1", org1),
            ("actor", actor),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.sale_lines (
                id, sale_id, organization_id, product_id, line_number, name_snapshot,
                quantity, unit_price, line_total, unit_of_measure_snapshot, selling_mode_snapshot)
            VALUES (
                @lid, @sid, @org1, @p1, 1, 'P29 WP14 Product One',
                1, 25.00, 25.00, 'Piece', 'PerItem');
            """,
            ("lid", lineId),
            ("sid", saleId),
            ("org1", org1),
            ("p1", product1));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.stock_movements (
                id, organization_id, inventory_account_id, product_id, movement_type, source_type,
                source_id, quantity_effect, reason, recorded_at_utc, recorded_by)
            VALUES (
                @mid, @org1, @i1, @p1, 'SaleDeduction', 'Sale',
                @sid, -1, 'P29 WP14 cash sale', @now, @actor);
            """,
            ("mid", movementId),
            ("org1", org1),
            ("i1", inv1),
            ("p1", product1),
            ("sid", saleId),
            ("actor", actor),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @org1, 'P29-WP14-CO-1', 'Submitted', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Personal', 'WP14 Buyer', @buyer,
                100.00, 20.00, 120.00, 'Reserved',
                @now, @now);
            """,
            ("oid", orderId),
            ("org1", org1),
            ("branch", branchId),
            ("buyer", buyer),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_order_lines (
                id, order_id, seller_organization_id, product_id, line_number,
                name_snapshot, unit_snapshot, quantity, unit_price, discount, line_total)
            VALUES (
                @olid, @oid, @org1, @p1, 1,
                'P29 WP14 Product One', 'Piece', 1, 100.00, 0, 100.00);
            """,
            ("olid", orderLineId),
            ("oid", orderId),
            ("org1", org1),
            ("p1", product1));

        // Payment attempt attached to the completed cash sale (schema allows Cash method).
        await ExecAsync(
            connection,
            """
            INSERT INTO pos.payment_attempts (
                id, organization_id, sale_id, method, provider, provider_reference,
                amount, currency, status, idempotency_key, created_by,
                created_at_utc, updated_at_utc, provider_event_sequence)
            VALUES (
                @aid, @org1, @sid, 'Cash', 'None', NULL,
                25.00, 'PHP', 'Paid', 'p29-wp14-cash-1', @actor,
                @now, @now, 0);
            """,
            ("aid", attemptId),
            ("org1", org1),
            ("sid", saleId),
            ("actor", actor),
            ("now", now));

        return new PosLatestSeed(org1, org2, saleId, orderId, inv1, attemptId);
    }

    private static async Task<PosLegacySeed> SeedPosCompatibleWithPreSaleReservationAsync(string connectionString)
    {
        var orgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10");
        var customerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb10");
        var productId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc10");
        var orderId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd10");
        var buyer = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeee10");
        var branchId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0010");
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await SeedPosCustomerAsync(connectionString, orgId, customerId, "P29 WP14 Legacy Customer");

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.products (
                id, organization_id, name, unit_of_measure, selling_mode, selling_price, status,
                catalog_source, created_at_utc, updated_at_utc)
            VALUES (
                @pid, @org, 'P29 WP14 Legacy Product', 'Piece', 'PerItem', 15.00, 'Active',
                'Manual', @now, @now);
            """,
            ("pid", productId),
            ("org", orgId),
            ("now", now));

        // sales without stock_reservation_state (column not yet present)
        await ExecAsync(
            connection,
            """
            INSERT INTO pos.sales (
                id, organization_id, sale_number, status, payment_method,
                subtotal, tax_amount, total, recorded_at_utc, recorded_by, updated_at_utc, buyer_party_kind,
                amount_tendered, change_amount)
            VALUES (
                @sid, @org, 'P29-LEGACY-S1', 'Completed', 'Cash',
                15.00, 0, 15.00, @now, @buyer, @now, 'WalkIn',
                20.00, 5.00);
            """,
            ("sid", Guid.Parse("12121212-1212-1212-1212-121212121210")),
            ("org", orgId),
            ("buyer", buyer),
            ("now", now));

        await ExecAsync(
            connection,
            """
            INSERT INTO pos.customer_orders (
                id, seller_organization_id, order_number, status, fulfillment_status, payment_status,
                fulfillment_type, fulfillment_branch_id, branch_name_snapshot,
                customer_party_type, customer_display_name_snapshot, customer_platform_user_id,
                merchandise_subtotal, delivery_fee, total, stock_reservation_state,
                created_at_utc, updated_at_utc)
            VALUES (
                @oid, @org, 'P29-LEGACY-CO1', 'Draft', 'Pending', 'Unpaid',
                'Pickup', @branch, 'Main',
                'Personal', 'Legacy Buyer', @buyer,
                15.00, 0, 15.00, 'None',
                @now, @now);
            """,
            ("oid", orderId),
            ("org", orgId),
            ("branch", branchId),
            ("buyer", buyer),
            ("now", now));

        return new PosLegacySeed(orgId, customerId, productId, orderId);
    }

    private static async Task SeedPosCustomerAsync(
        string connectionString,
        Guid orgId,
        Guid customerId,
        string displayName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO pos.customers (
                id, organization_id, display_name, mobile_number, normalized_mobile,
                address, notes, status, created_at_utc, updated_at_utc)
            VALUES (
                @id, @org, @name, NULL, NULL,
                NULL, NULL, 'Active', @now, @now);
            """;
        cmd.Parameters.AddWithValue("id", customerId);
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("name", displayName);
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task AssertTenantOrgIdsPreservedAsync(string connectionString, Guid org1, Guid org2)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(DISTINCT organization_id)::int
            FROM (
              SELECT organization_id FROM pos.products
              UNION
              SELECT organization_id FROM pos.inventory_accounts
              UNION
              SELECT organization_id FROM pos.sales
            ) t
            WHERE organization_id IN (@org1, @org2);
            """;
        cmd.Parameters.AddWithValue("org1", org1);
        cmd.Parameters.AddWithValue("org2", org2);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        Assert.Equal(2, count);
    }

    private static async Task AssertNoNegativeInventoryAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*)::int
            FROM pos.inventory_accounts
            WHERE is_tracked = TRUE AND on_hand_quantity < 0;
            """;
        var negatives = Convert.ToInt32(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        Assert.Equal(0, negatives);
    }

    /// <summary>
    /// Proves ExItS Platform Infrastructure can open the restored DB (EF CanConnect + Migrate no-op + read).
    /// Not an HTTP API host smoke.
    /// </summary>
    private static async Task AssertPlatformEfReadbackAsync(string connectionString, PlatformSeed seed)
    {
        await using var db = new PlatformDbContext(
            new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connectionString).Options);
        Assert.True(await db.Database.CanConnectAsync());
        await db.Database.MigrateAsync();

        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT o.display_name, b.code, p.free_delivery_threshold::text
            FROM platform.organizations o
            JOIN platform.organization_branches b ON b.organization_id = o.id AND b.id = @branch
            LEFT JOIN platform.branch_delivery_policies p
              ON p.branch_id = b.id AND p.organization_id = o.id
            WHERE o.id = @org;
            """;
        var orgParam = cmd.CreateParameter();
        orgParam.ParameterName = "org";
        orgParam.Value = seed.Org1Id;
        cmd.Parameters.Add(orgParam);
        var branchParam = cmd.CreateParameter();
        branchParam.ParameterName = "branch";
        branchParam.Value = seed.Branch1Id;
        cmd.Parameters.Add(branchParam);

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("P29 WP14 Org One", reader.GetString(0));
        Assert.Equal("MAIN", reader.GetString(1));
        Assert.Equal(500m, decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Proves ExItS POS Infrastructure can open the restored DB (EF CanConnect + Migrate no-op + read).
    /// Not an HTTP API host smoke.
    /// </summary>
    private static async Task AssertPosEfReadbackAsync(string connectionString, PosLatestSeed seed)
    {
        await using var db = new PosDbContext(
            new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(connectionString).Options);
        Assert.True(await db.Database.CanConnectAsync());
        await db.Database.MigrateAsync();

        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT s.sale_number, s.organization_id::text, co.order_number, co.total::text,
                   ia.on_hand_quantity::text, ia.reserved_quantity::text, pa.status
            FROM pos.sales s
            JOIN pos.customer_orders co ON co.id = @order
            JOIN pos.inventory_accounts ia ON ia.id = @inv
            LEFT JOIN pos.payment_attempts pa ON pa.id = @attempt
            WHERE s.id = @sale;
            """;
        void Add(string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        Add("sale", seed.SaleId);
        Add("order", seed.OrderId);
        Add("inv", seed.Inventory1Id);
        Add("attempt", seed.PaymentAttemptId);

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("P29-WP14-SALE-1", reader.GetString(0));
        Assert.Equal(seed.Org1Id.ToString(), reader.GetString(1));
        Assert.Equal("P29-WP14-CO-1", reader.GetString(2));
        Assert.Equal(120.00m, decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture));
        Assert.Equal(90m, decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture));
        Assert.Equal(5m, decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture));
        Assert.Equal("Paid", reader.GetString(6));
    }

    private static async Task<bool> ColumnExistsAsync(
        string connectionString,
        string schema,
        string table,
        string column)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = @schema AND table_name = @table AND column_name = @column);
            """;
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("column", column);
        return Convert.ToBoolean(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task ExecAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        await cmd.ExecuteNonQueryAsync();
    }
}
