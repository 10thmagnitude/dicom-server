﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class DicomWebClient
    {
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicom = new MediaTypeWithQualityHeaderValue("application/dicom");
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicomJson = new MediaTypeWithQualityHeaderValue("application/dicom+json");
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public DicomWebClient(HttpClient httpClient)
        {
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            HttpClient = httpClient;

            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.Converters.Add(new JsonDicomConverter(writeTagsAsKeywords: true));
        }

        public HttpClient HttpClient { get; }

        public async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUID = null)
        {
            var postContent = new List<byte[]>();

            foreach (DicomFile dicomFile in dicomFiles)
            {
                using (var stream = new MemoryStream())
                {
                    await dicomFile.SaveAsync(stream);
                    postContent.Add(stream.ToArray());
                }
            }

            return await PostAsync(postContent, studyInstanceUID);
        }

        public async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<Stream> streams, string studyInstanceUID = null)
        {
            var postContent = new List<byte[]>();

            foreach (Stream stream in streams)
            {
                byte[] content = await ConvertStreamToByteArrayAsync(stream);
                postContent.Add(content);
            }

            return await PostAsync(streams, studyInstanceUID);
        }

        private static MultipartContent GetMultipartContent(string mimeType)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{mimeType}\""));
            return multiContent;
        }

        private async Task<HttpResult<DicomDataset>> PostAsync(IEnumerable<byte[]> postContent, string studyInstanceUID)
        {
            MultipartContent multiContent = GetMultipartContent(MediaTypeApplicationDicom.MediaType);

            foreach (byte[] content in postContent)
            {
                var byteContent = new ByteArrayContent(content);
                byteContent.Headers.ContentType = MediaTypeApplicationDicom;
                multiContent.Add(byteContent);
            }

            return await PostMultipartContentAsync(multiContent, $"studies/{studyInstanceUID}");
        }

        internal async Task<HttpResult<DicomDataset>> PostMultipartContentAsync(MultipartContent multiContent, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = multiContent;

            using (HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Accepted ||
                    response.StatusCode == HttpStatusCode.Conflict)
                {
                    var contentText = await response.Content.ReadAsStringAsync();
                    DicomDataset dataset = JsonConvert.DeserializeObject<DicomDataset>(contentText, _jsonSerializerSettings);

                    return new HttpResult<DicomDataset>(response.StatusCode, dataset);
                }

                return new HttpResult<DicomDataset>(response.StatusCode);
            }
        }

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory);
                return memory.ToArray();
            }
        }
    }
}
