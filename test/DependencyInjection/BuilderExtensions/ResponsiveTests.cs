// Copyright (c) 2014-2020 Sarin Na Wangkanai, All Rights Reserved.
// The Apache v2. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wangkanai.Detection.Models;
using Xunit;

namespace Wangkanai.Detection.Hosting
{
    public class ResponsiveCollectionExtensionsTests
    {
        private readonly int          total                       = 17;
        private readonly Func<object> CreateResponsiveNullService = () => ((IServiceCollection) null).AddDetection();

        private static RequestDelegate ResponsiveContextHandler()
        {
            return context => context.GetDevice() switch
            {
                Device.Desktop => context.Response.WriteAsync("Response: Desktop"),
                Device.Tablet  => context.Response.WriteAsync("Response: Tablet"),
                Device.Mobile  => context.Response.WriteAsync("Response: Mobile"),
                _              => context.Response.WriteAsync("Response: Who?")
            };
        }

        private static HttpRequestMessage CreateHttpRequestMessage(string agent, string url = "/")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.SetUserAgent(agent);
            return request;
        }

        [Fact]
        public void AddResponsive_Services()
        {
            var service  = new ServiceCollection();
            var expected = service.Count + total;
            var builder  = service.AddDetection();

            Assert.Same(service, builder.Services);
        }

        [Fact]
        public void AddResponsive_Null_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(CreateResponsiveNullService);
        }

        [Fact]
        public void AddResponsive_Options_Null_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(CreateResponsiveNullService);
        }

        [Fact]
        public void AddResponsive_Options_Builder_Service()
        {
            var service  = new ServiceCollection();
            var expected = service.Count + total;
            var builder  = service.AddDetection(options => { options.Responsive.DefaultTablet = Device.Desktop; });

            Assert.Same(service, builder.Services);
        }

        [Fact]
        public async void AddResponsive_Options_Disable_True()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                    services.AddDetection(options => { options.Responsive.Disable = true; })
                )
                .Configure(app =>
                {
                    app.UseDetection();
                    app.Run(ResponsiveContextHandler());
                });

            using var server   = new TestServer(builder);
            var       client   = server.CreateClient();
            var       request  = CreateHttpRequestMessage("mobile");
            var       response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Contains("desktop", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }


        [Fact]
        public async void AddResponsive_Options_Disable_False()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                    services.AddDetection(options => { options.Responsive.Disable = false; })
                )
                .Configure(app =>
                {
                    app.UseDetection();
                    app.Run(ResponsiveContextHandler());
                });

            using var server   = new TestServer(builder);
            var       client   = server.CreateClient();
            var       request  = CreateHttpRequestMessage("mobile");
            var       response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Contains("mobile", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async void AddResponsive_Options_Disable_IncludeWebApi()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                    services.AddDetection(options =>
                    {
                        options.Responsive.Disable       = true;
                        options.Responsive.IncludeWebApi = true;
                    })
                )
                .Configure(app =>
                {
                    app.UseDetection();
                    app.Run(ResponsiveContextHandler());
                });

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                using var server = new TestServer(builder);
            });
            Assert.Equal("IncludeWebApi is not needed if already Disable", exception.Message);
        }

        [Theory]
        [InlineData(Device.Mobile, "desktop", "/api/dog")]
        [InlineData(Device.Mobile, "mobile", "/api/dog")]
        public async void AddResponsive_WebApi_Exclude_Api(Device device, string agent, string path)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                    services.AddDetection(options =>
                    {
                        options.Responsive.DefaultMobile  = device;
                        options.Responsive.DefaultTablet  = device;
                        options.Responsive.DefaultDesktop = device;
                        options.Responsive.IncludeWebApi  = false;
                    })
                )
                .Configure(app =>
                {
                    app.UseDetection();
                    app.Run(ResponsiveContextHandler());
                });

            using var server   = new TestServer(builder);
            var       client   = server.CreateClient();
            var       request  = CreateHttpRequestMessage(agent, path);
            var       response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Contains("desktop", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(Device.Desktop, "mobile", "")]
        [InlineData(Device.Desktop, "desktop", "")]
        [InlineData(Device.Desktop, "mobile", "/api/dog")]
        [InlineData(Device.Desktop, "desktop", "/api/dog")]
        [InlineData(Device.Mobile, "desktop", "")]
        [InlineData(Device.Mobile, "mobile", "")]
        public async void AddResponsive_WebApi_Exclude_NonApi(Device device, string agent, string path)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                    services.AddDetection(options =>
                    {
                        options.Responsive.DefaultMobile  = device;
                        options.Responsive.DefaultTablet  = device;
                        options.Responsive.DefaultDesktop = device;
                        options.Responsive.IncludeWebApi  = false;
                    })
                )
                .Configure(app =>
                {
                    app.UseDetection();
                    app.Run(ResponsiveContextHandler());
                });

            using var server   = new TestServer(builder);
            var       client   = server.CreateClient();
            var       request  = CreateHttpRequestMessage(agent, path);
            var       response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Contains(device.ToString(), await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(Device.Desktop, "mobile", "")]
        [InlineData(Device.Desktop, "mobile", "/api/dog")]
        [InlineData(Device.Desktop, "desktop", "")]
        [InlineData(Device.Desktop, "desktop", "/api/dog")]
        [InlineData(Device.Mobile, "desktop", "")]
        [InlineData(Device.Mobile, "desktop", "/api/dog")]
        [InlineData(Device.Mobile, "mobile", "")]
        [InlineData(Device.Mobile, "mobile", "/api/dog")]
        public async void AddResponsive_WebApi_Include_Api(Device device, string agent, string path)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                    services.AddDetection(options =>
                    {
                        options.Responsive.DefaultMobile  = device;
                        options.Responsive.DefaultTablet  = device;
                        options.Responsive.DefaultDesktop = device;
                        options.Responsive.IncludeWebApi  = true;
                    })
                )
                .Configure(app =>
                {
                    app.UseDetection();
                    app.Run(ResponsiveContextHandler());
                });

            using var server   = new TestServer(builder);
            var       client   = server.CreateClient();
            var       request  = CreateHttpRequestMessage(agent, path);
            var       response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Contains(device.ToString(), await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
    }
}