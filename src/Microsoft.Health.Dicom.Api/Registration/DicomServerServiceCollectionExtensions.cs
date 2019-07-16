﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for enabling a DICOM server.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <returns>A <see cref="IDicomServerBuilder"/> object.</returns>
        public static IDicomServerBuilder AddDicomServer(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddOptions();
            services.AddMvc(options =>
            {
                options.RespectBrowserAcceptHeader = true;
                options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter());
            });

            services.AddSingleton<IDicomRouteProvider, DicomRouteProvider>();
            services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly);
            services.AddTransient<IStartupFilter, DicomServerStartupFilter>();

            return new DicomServerBuilder(services);
        }

        private class DicomServerBuilder : IDicomServerBuilder
        {
            public DicomServerBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));
                Services = services;
            }

            public IServiceCollection Services { get; }
        }

        /// <summary>
        /// An <see cref="IStartupFilter"/> that configures middleware components before any components are added in Startup.Configure
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated.")]
        private class DicomServerStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.UseExceptionHandling();
                    next(app);
                };
            }
        }
    }
}
