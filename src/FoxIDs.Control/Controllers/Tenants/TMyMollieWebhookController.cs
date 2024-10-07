//using AutoMapper;
//using FoxIDs.Infrastructure;
//using FoxIDs.Repository;
//using FoxIDs.Models;
//using Api = FoxIDs.Models.Api;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;
//using FoxIDs.Logic;
//using System;
//using ITfoxtec.Identity;
//using FoxIDs.Infrastructure.Security;
//using Microsoft.Extensions.DependencyInjection;
//using FoxIDs.Models.Config;
//using Amazon.Runtime;
//using Azure.Core;
//using System.Net.Http.Headers;
//using System.Net.Http;
//using ITfoxtec.Identity.Util;
//using ITfoxtec.Identity.Messages;
//using System.Collections.Generic;

//namespace FoxIDs.Controllers
//{
//    /// <summary>
//    /// Mollie webhook.
//    /// </summary>
//    public class TMyMollieWebhookController :  ApiController
//    {
//        private readonly FoxIDsControlSettings settings;
//        private readonly TelemetryScopedLogger logger;
//        private readonly IServiceProvider serviceProvider;
//        private readonly IMapper mapper;
//        private readonly ITenantDataRepository tenantDataRepository;
//        private readonly TenantCacheLogic tenantCacheLogic;
//        private readonly IHttpClientFactory httpClientFactory;

//        public TMyMollieWebhookController(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IServiceProvider serviceProvider, IMapper mapper, ITenantDataRepository tenantDataRepository, TenantCacheLogic tenantCacheLogic, IHttpClientFactory httpClientFactory) : base(logger)
//        {
//            this.settings = settings;
//            this.logger = logger;
//            this.serviceProvider = serviceProvider;
//            this.mapper = mapper;
//            this.tenantDataRepository = tenantDataRepository;
//            this.tenantCacheLogic = tenantCacheLogic;
//            this.httpClientFactory = httpClientFactory;
//        }

//        /// <summary>
//        /// Mollie webhook.
//        /// </summary>
//        /// <param name="webhook">Webhook.</param>
//        /// <returns>Tenant.</returns>
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        public async Task<ActionResult> PostMyMollieWebhook([FromBody] Api.MollieWebhookRequest webhook)
//        {
//            try
//            {
//                if(settings.PlanPayment?.EnablePlanPayment != true)
//                {
//                    throw new Exception("Payment not configured.");
//                }

              


//                return Ok();
//            }
//            catch (Exception ex)
//            {
//            }
//        }
//    }
//}
