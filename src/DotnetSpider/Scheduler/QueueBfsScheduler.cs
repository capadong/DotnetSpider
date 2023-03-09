using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.Scheduler.Component;
using Microsoft.Extensions.Options;

namespace DotnetSpider.Scheduler
{
	/// <summary>
	/// 基于内存的广度优先调度(不去重 URL)
	/// </summary>
	public class QueueBfsScheduler : SchedulerBase
	{
		readonly SpiderOptions _options;

		private readonly List<Request> _requests =
			new();

		/// <summary>
		/// 构造方法
		/// </summary>
		public QueueBfsScheduler(IRequestHasher requestHasher, IOptions<SpiderOptions> options) : base(new FakeDuplicateRemover(), requestHasher)
		{
			_options = options.Value;
		}

		public override void Dispose()
		{
			_requests.Clear();
			base.Dispose();
		}

		/// <summary>
		/// 如果请求未重复就添加到队列中
		/// </summary>
		/// <param name="request">请求</param>
		protected override Task PushWhenNoDuplicate(Request request)
		{
			if (request == null)
			{
				return Task.CompletedTask;
			}

			_requests.Add(request);
			return Task.CompletedTask;
		}

		/// <summary>
		/// 从队列中取出指定爬虫的指定个数请求
		/// </summary>
		/// <param name="count">出队数</param>
		/// <returns>请求</returns>
		protected override Task<IEnumerable<Request>> ImplDequeueAsync(int count = 1)
		{
			Request[] requests = null;

			if (_options.OneRequestDoneFirst)
			{
				 requests = _requests
					  .OrderByDescending(x => x.Depth)
					  .Take(count).ToArray();

				if (requests.Length > 0)
				{
					for (int i = 0; i < requests.Length; i++)
					{
						_requests.Remove(requests[i]);
					}
				}
			}
			else
			{
				requests = _requests.Take(count).ToArray();
				if (requests.Length > 0)
				{
					_requests.RemoveRange(0, count);
				}
			}

			return Task.FromResult(requests.Select(x => x.Clone()));
		}
	}
}
