//Uncomment the next line to enable debugging
//#define ProfileAstar
using System.Collections.Generic;
using System;
using UnityEngine;

public class AstarProfiler
{
	public struct ProfilePoint
	{
		public DateTime lastRecorded;
		public TimeSpan totalTime;
		public int totalCalls;
	}
	
	private static Dictionary<string, ProfilePoint> profiles = new Dictionary<string, ProfilePoint>();
	private static DateTime startTime = DateTime.UtcNow;
	
	public static ProfilePoint[] fastProfiles;
	public static string[] fastProfileNames;
	
	private AstarProfiler()
	{
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void InitializeFastProfile (string[] profileNames) {
		fastProfileNames = profileNames;
		fastProfiles = new ProfilePoint[profileNames.Length];
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void StartFastProfile(int tag)
	{
		//profiles.TryGetValue(tag, out point);
		fastProfiles[tag].lastRecorded = DateTime.UtcNow;
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void EndFastProfile(int tag)
	{
		DateTime now = DateTime.UtcNow;
		/*if (!profiles.ContainsKey(tag))
		{
			Debug.LogError("Can only end profiling for a tag which has already been started (tag was " + tag + ")");
			return;
		}*/
		ProfilePoint point = fastProfiles[tag];
		point.totalTime += now - point.lastRecorded;
		point.totalCalls++;
		fastProfiles[tag] = point;
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void StartProfile(string tag)
	{
		ProfilePoint point;
		
		profiles.TryGetValue(tag, out point);
		point.lastRecorded = DateTime.UtcNow;
		profiles[tag] = point;
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void EndProfile(string tag)
	{
		if (!profiles.ContainsKey(tag))
		{
			Debug.LogError("Can only end profiling for a tag which has already been started (tag was " + tag + ")");
			return;
		}
		DateTime now = DateTime.UtcNow;
		ProfilePoint point = profiles[tag];
		point.totalTime += now - point.lastRecorded;
		++point.totalCalls;
		profiles[tag] = point;
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void Reset()
	{
		profiles.Clear();
		startTime = DateTime.UtcNow;
		
		if (fastProfiles != null) {
			for (int i=0;i<fastProfiles.Length;i++) {
				fastProfiles[i] = new ProfilePoint ();
			}
		}
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void PrintFastResults()
	{
		TimeSpan endTime = DateTime.UtcNow - startTime;
		System.Text.StringBuilder output = new System.Text.StringBuilder();
		output.Append("============================\n\t\t\t\tProfile results:\n============================\n");
		//foreach(KeyValuePair<string, ProfilePoint> pair in profiles)
		for (int i=0;i<fastProfiles.Length;i++)
		{
			string name = fastProfileNames[i];
			ProfilePoint value = fastProfiles[i];
			
			double totalTime = value.totalTime.TotalMilliseconds;
			int totalCalls = value.totalCalls;
			if (totalCalls < 1) continue;
			output.Append("\nProfile ");
			output.Append(name);
			output.Append(" took \t");
			output.Append(totalTime.ToString("0.0"));
			output.Append(" ms to complete over ");
			output.Append(totalCalls);
			output.Append(" iteration");
			if (totalCalls != 1) output.Append("s");
			output.Append(", averaging \t");
			output.Append((totalTime / totalCalls).ToString("0.000"));
			output.Append(" ms per call");
		}
		output.Append("\n\n============================\n\t\tTotal runtime: ");
		output.Append(endTime.TotalSeconds.ToString("F3"));
		output.Append(" seconds\n============================");
		Debug.Log(output.ToString());
	}
	
	[System.Diagnostics.Conditional ("ProfileAstar")]
	public static void PrintResults()
	{
		TimeSpan endTime = DateTime.UtcNow - startTime;
		System.Text.StringBuilder output = new System.Text.StringBuilder();
		output.Append("============================\n\t\t\t\tProfile results:\n============================\n");
		foreach(KeyValuePair<string, ProfilePoint> pair in profiles)
		{
			double totalTime = pair.Value.totalTime.TotalMilliseconds;
			int totalCalls = pair.Value.totalCalls;
			if (totalCalls < 1) continue;
			output.Append("\nProfile ");
			output.Append(pair.Key);
			output.Append(" took ");
			output.Append(totalTime.ToString("0"));
			output.Append(" ms to complete over ");
			output.Append(totalCalls);
			output.Append(" iteration");
			if (totalCalls != 1) output.Append("s");
			output.Append(", averaging ");
			output.Append((totalTime / totalCalls).ToString("0.0"));
			output.Append(" ms per call");
		}
		output.Append("\n\n============================\n\t\tTotal runtime: ");
		output.Append(endTime.TotalSeconds.ToString("F3"));
		output.Append(" seconds\n============================");
		Debug.Log(output.ToString());
	}
}