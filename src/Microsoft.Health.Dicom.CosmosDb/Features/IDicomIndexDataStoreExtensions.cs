﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.CosmosDb.Features
{
    internal static class IDicomIndexDataStoreExtensions
    {
        public static async Task IndexInstanceAsync(
            this IDicomIndexDataStore indexDataStore, DicomDataset dicomDataset, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            await indexDataStore.IndexSeriesAsync(new[] { dicomDataset }, cancellationToken);
        }
    }
}
