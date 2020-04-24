﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Polly;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomSqlDataStoreTestsFixture : IAsyncLifetime
    {
        private const string LocalConnectionString = "server=(local);Integrated Security=true";

        private readonly string _masterConnectionString;
        private readonly string _databaseName;
        private readonly SchemaInitializer _schemaInitializer;

        public DicomSqlDataStoreTestsFixture()
        {
            string initialConnectionString = Environment.GetEnvironmentVariable("SqlServer:ConnectionString") ?? LocalConnectionString;

            _databaseName = $"DICOMINTEGRATIONTEST_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{BigInteger.Abs(new BigInteger(Guid.NewGuid().ToByteArray()))}";
            _masterConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = "master" }.ToString();
            TestConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = _databaseName }.ToString();

            var config = new SqlServerDataStoreConfiguration { ConnectionString = TestConnectionString, Initialize = true };

            var scriptProvider = new ScriptProvider<SchemaVersion>();

            var schemaUpgradeRunner = new SchemaUpgradeRunner(scriptProvider, config, NullLogger<SchemaUpgradeRunner>.Instance);

            var schemaInformation = new SchemaInformation((int)SchemaVersion.V1, (int)SchemaVersion.V1);

            _schemaInitializer = new SchemaInitializer(config, schemaUpgradeRunner, schemaInformation, NullLogger<SchemaInitializer>.Instance);

            var dicomSqlIndexSchema = new DicomSqlIndexSchema(schemaInformation, NullLogger<DicomSqlIndexSchema>.Instance);

            DicomIndexDataStore = new DicomSqlIndexDataStore(
                dicomSqlIndexSchema,
                config,
                NullLogger<DicomSqlIndexDataStore>.Instance);

            DicomInstanceStore = new DicomSqlInstanceStore(config);

            TestHelper = new DicomSqlIndexDataStoreTestHelper(TestConnectionString);
        }

        public string TestConnectionString { get; }

        public IDicomIndexDataStore DicomIndexDataStore { get; }

        public IDicomInstanceStore DicomInstanceStore { get; }

        public DicomSqlIndexDataStoreTestHelper TestHelper { get; }

        public async Task InitializeAsync()
        {
            // Create the database
            using (var sqlConnection = new SqlConnection(_masterConnectionString))
            {
                await sqlConnection.OpenAsync();

                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandTimeout = 600;
                    command.CommandText = $"CREATE DATABASE {_databaseName}";
                    await command.ExecuteNonQueryAsync();
                }
            }

            // verify that we can connect to the new database. This sometimes does not work right away with Azure SQL.
            await Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    retryCount: 7,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(async () =>
                {
                    using (var sqlConnection = new SqlConnection(TestConnectionString))
                    {
                        await sqlConnection.OpenAsync();
                        using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                        {
                            sqlCommand.CommandText = "SELECT 1";
                            await sqlCommand.ExecuteScalarAsync();
                        }
                    }
                });

            _schemaInitializer.Start();
        }

        public async Task DisposeAsync()
        {
            using (var sqlConnection = new SqlConnection(_masterConnectionString))
            {
                await sqlConnection.OpenAsync();
                SqlConnection.ClearAllPools();

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandTimeout = 600;
                    sqlCommand.CommandText = $"DROP DATABASE IF EXISTS {_databaseName}";
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}