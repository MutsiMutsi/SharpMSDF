namespace SharpMSDF.Atlas
{
	public class Workload
	{
		private readonly Func<int, int, bool> _workerFunction;
		private readonly int _chunks;

		public Workload() { }

		public Workload(Func<int, int, bool> workerFunction, int chunks)
		{
			_workerFunction = workerFunction;
			_chunks = chunks;
		}

		public bool Finish(int threadCount)
		{
			if (_chunks == 0)
			{
				return true;
			}

			if (threadCount == 1 || _chunks == 1)
			{
				return FinishSequential();
			}

			return threadCount > 1 && FinishParallel(Math.Min(threadCount, _chunks));
		}

		private bool FinishSequential()
		{
			for (int i = 0; i < _chunks; ++i)
			{
				if (!_workerFunction(i, 0))
				{
					return false;
				}
			}

			return true;
		}

		private bool FinishParallel(int threadCount)
		{
			bool result = true;
			int next = 0;
			object lockObj = new();

			List<Thread> threads = new(threadCount);
			for (int threadNo = 0; threadNo < threadCount; ++threadNo)
			{
				int localThreadNo = threadNo;
				Thread thread = new(() =>
				{
					while (true)
					{
						int i;
						lock (lockObj)
						{
							if (!result || next >= _chunks)
							{
								return;
							}

							i = next++;
						}

						if (!_workerFunction(i, localThreadNo))
						{
							lock (lockObj)
							{
								result = false;
							}

							return;
						}
					}
				});
				threads.Add(thread);
				thread.Start();
			}

			foreach (Thread thread in threads)
			{
				thread.Join();
			}

			return result;
		}
	}
}
