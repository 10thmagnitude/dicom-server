﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class DicomTagSqlEntry
    {
        private static Dictionary<DicomTag, DicomTagSqlEntry> _tagToSqlMappingCache = new Dictionary<DicomTag, DicomTagSqlEntry>()
        {
                { DicomTag.StudyInstanceUID, new DicomTagSqlEntry(DicomTag.StudyInstanceUID, SqlTableType.StudyTable, VLatest.Study.StudyInstanceUid) },
                { DicomTag.StudyDate, new DicomTagSqlEntry(DicomTag.StudyDate, SqlTableType.StudyTable, VLatest.Study.StudyDate) },
                { DicomTag.StudyDescription, new DicomTagSqlEntry(DicomTag.StudyDescription, SqlTableType.StudyTable, VLatest.Study.StudyDescription) },
                { DicomTag.AccessionNumber, new DicomTagSqlEntry(DicomTag.AccessionNumber, SqlTableType.StudyTable, VLatest.Study.AccessionNumber) },
                { DicomTag.PatientID, new DicomTagSqlEntry(DicomTag.PatientID, SqlTableType.StudyTable, VLatest.Study.PatientId) },
                { DicomTag.PatientName, new DicomTagSqlEntry(DicomTag.PatientName, SqlTableType.StudyTable, VLatest.Study.PatientName, VLatest.StudyTable.PatientNameWords) },
                { DicomTag.ReferringPhysicianName, new DicomTagSqlEntry(DicomTag.ReferringPhysicianName, SqlTableType.StudyTable, VLatest.Study.ReferringPhysicianName) },
                { DicomTag.SeriesInstanceUID, new DicomTagSqlEntry(DicomTag.SeriesInstanceUID, SqlTableType.SeriesTable, VLatest.Series.SeriesInstanceUid) },
                { DicomTag.Modality, new DicomTagSqlEntry(DicomTag.Modality, SqlTableType.SeriesTable, VLatest.Series.Modality) },
                { DicomTag.PerformedProcedureStepStartDate, new DicomTagSqlEntry(DicomTag.PerformedProcedureStepStartDate, SqlTableType.SeriesTable, VLatest.Series.PerformedProcedureStepStartDate) },
                { DicomTag.SOPInstanceUID, new DicomTagSqlEntry(DicomTag.SOPInstanceUID, SqlTableType.InstanceTable, VLatest.Instance.SopInstanceUid) },
        };

        private DicomTagSqlEntry(DicomTag dicomTag, SqlTableType sqlTableType, Column sqlColumn, string fullTextIndexColumnName = null)
        {
            DicomTag = dicomTag;
            SqlTableType = sqlTableType;
            SqlColumn = sqlColumn;
            FullTextIndexColumnName = fullTextIndexColumnName;
        }

        public SqlTableType SqlTableType { get; }

        public DicomTag DicomTag { get; }

        public Column SqlColumn { get; }

        public string FullTextIndexColumnName { get; }

        public static DicomTagSqlEntry GetDicomTagSqlEntry(DicomTag dicomTag)
        {
            return _tagToSqlMappingCache[dicomTag];
        }
    }
}
