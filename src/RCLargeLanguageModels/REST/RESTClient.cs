using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace RCLargeLanguageModels.REST
{
	public class RESTClient : IDisposable
	{
		private bool isDisposed;
		public HttpClient _http;

		public string EndPoint { get; set; }

		public RESTClient()
		{
			_http = new HttpClient();
		}

		public RESTClient(HttpClient httpClient)
		{
			_http = httpClient;
		}

		~RESTClient()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					_http.Dispose();
				}

				isDisposed = true;
			}
		}
	}
}