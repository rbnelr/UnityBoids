
using System.Runtime.InteropServices;

public static class QPC {
	[DllImport("Kernel32.dll")]
	private static extern bool QueryPerformanceFrequency(out long lpFrequency);

	[DllImport("Kernel32.dll")]
	private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

	private static double GetPeriod () {
		QueryPerformanceFrequency(out long freq);
		return 1.0 / (double)freq;
	}

	public static double Period = GetPeriod();

	public static long GetTimestamp () {
		QueryPerformanceCounter(out long ts);
		return ts;
	}
}

public struct PerfTimer {
	public long start;

	public static PerfTimer Start () {
		return new PerfTimer{ start = QPC.GetTimestamp() };
	}

	public double End () {
		long end = QPC.GetTimestamp();
		return (double)(end - start) * QPC.Period;
	}
}
