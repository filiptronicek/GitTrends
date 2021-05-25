﻿using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace GitTrends.Functions
{
    class GetGitTrendsStatistics
    {
        readonly BlobStorageService _blobStorageService;

        public GetGitTrendsStatistics(BlobStorageService blobStorageService) => _blobStorageService = blobStorageService;

        [Function(nameof(GetGitTrendsStatistics))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request, FunctionContext context)
        {
            var log = context.GetLogger<GetGitTrendsStatistics>();
            var gitTrendsStatistics = await _blobStorageService.GetGitTrendsStatistics().ConfigureAwait(false);

            var response = request.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(gitTrendsStatistics).ConfigureAwait(false);

            return response;
        }
    }
}
