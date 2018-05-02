using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace TraceContextTest {
	public class Startup {
		public Startup() {

		}

		public void ConfigureServices(IServiceCollection services) {
			services.AddMvc();
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseBrowserLink();
				app.UseDeveloperExceptionPage();
			} else {
				app.UseExceptionHandler("/Error");
			}
			app.UseMvc();

			app.Run(async context => {
				string traceparent = GetHeaderValue(context.Request.Headers, "traceparent");
				string tracestate = GetHeaderValue(context.Request.Headers, "tracestate");
				string requestRoute = GetHeaderValue(context.Request.Headers, "requestroute");

				Console.WriteLine();
				Console.WriteLine($"==== NEW REQUEST ==== ({DateTime.Now})");
				Console.WriteLine($"  traceparent: {traceparent}");
				Console.WriteLine($"  tracestate: {tracestate}");
				Console.WriteLine($"  requestroute: {requestRoute}");
				Console.WriteLine();

				System.Threading.Thread.Sleep(500);

				if (!string.IsNullOrEmpty(requestRoute)) {
					await DoNextRequest(context, requestRoute);
				}
			});
		}

		private string GetHeaderValue(IHeaderDictionary headers, string name) {
			string value = null;
			if (headers.TryGetValue(name, out StringValues values)) {
				value = values.FirstOrDefault();
			} else {
				Console.WriteLine($"header '{name}' not found");
			}
			return value;
		}
		
		private static async Task DoNextRequest(HttpContext context, string requestRoute) {
			(string nextRequest, string moreRequests) = ParseNextRequest(requestRoute);

			using (var httpClient = new HttpClient()) {
				httpClient.DefaultRequestHeaders.Add("requestroute", moreRequests);
				await httpClient.GetAsync(nextRequest);
				await httpClient.GetAsync(nextRequest);
				await httpClient.GetAsync(nextRequest);
			}
		}

		private static (string nextrequest, string moreRequests) ParseNextRequest(string requestRoute) {
			var parts = requestRoute.Split(";");
			if (parts.Length == 0) return (null, null);
			if (parts.Length == 1) return (parts[0], null);
			return (parts[0], string.Join(";", parts.Skip(1)));
		}
	}
}
