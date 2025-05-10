using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DLS.Game;
using UnityEngine;

namespace DLS.Simulation
{
	public class SimThread
	{
		public const float SimulationPerformanceTimeWindowSec = 1.5f;
		Project project;
		static bool running;
		
		public void Start(Project project)
		{
			this.project = project;
			running = true;
			
			Thread simThread = new(Run)
			{
				Priority = System.Threading.ThreadPriority.Highest,
				Name = "DLS_SimThread",
				IsBackground = true
			};
			simThread.Start();	
		}

		public static void StopAll()
		{
			running = false;
		}

		
		void Run()
		{
			const int performanceTimeWindowMs = (int)(SimulationPerformanceTimeWindowSec * 1000);
			Queue<long> tickCounterOverTimeWindow = new();

			Stopwatch stopwatch = new();
			Stopwatch stopwatchTotal = Stopwatch.StartNew();

			while (running)
			{
				Simulator.ApplyModifications();

				// If sim is paused, sleep a bit and then check again
				// Also handle advancing a single step
				if (project.simPaused && !project.advanceSingleSimStep)
				{
					Simulator.UpdateInPausedState();
					stopwatchTotal.Stop();
					Thread.Sleep(10);
					continue;
				}

				if (project.advanceSingleSimStep)
				{
					project.simPausedSingleStepCounter++;
					project.advanceSingleSimStep = false;
				}
				else project.simPausedSingleStepCounter = 0;

				double targetTickDurationMs = 1000.0 / project.targetTicksPerSecond;
				stopwatch.Restart();
				if (!stopwatchTotal.IsRunning) stopwatchTotal.Start();

				// ---- Run sim ----
				Simulator.stepsPerClockTransition = project.stepsPerClockTransition;
				SimChip simChip = project.rootSimChip;
				if (simChip == null) continue; // Could potentially be null for a frame when switching between chips
				Simulator.RunSimulationStep(simChip, project.inputPins, project.audioState.simAudio);

				// ---- Wait some amount of time (if needed) to try to hit the target ticks per second ----
				while (true)
				{
					double elapsedMs = stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
					double waitMs = targetTickDurationMs - elapsedMs;

					if (waitMs <= 0) break;

					// Wait some cycles before checking timer again (todo: better approach?)
					Thread.SpinWait(10);
				}

				// ---- Update perf counter (measures average num ticks over last n seconds) ----
				long elapsedMsTotal = stopwatchTotal.ElapsedMilliseconds;
				tickCounterOverTimeWindow.Enqueue(elapsedMsTotal);
				while (tickCounterOverTimeWindow.Count > 0)
				{
					if (elapsedMsTotal - tickCounterOverTimeWindow.Peek() > performanceTimeWindowMs)
					{
						tickCounterOverTimeWindow.Dequeue();
					}
					else break;
				}

				if (tickCounterOverTimeWindow.Count > 0)
				{
					double activeWindowMs = elapsedMsTotal - tickCounterOverTimeWindow.Peek();
					if (activeWindowMs > 0)
					{
						project.simAvgTicksPerSec = tickCounterOverTimeWindow.Count / activeWindowMs * 1000;
					}
				}
			}
		}
		
		public static void Debug_RunMainThreadSimStep(Project project)
		{
			Simulator.stepsPerClockTransition = project.stepsPerClockTransition;
			Simulator.ApplyModifications();
			Simulator.RunSimulationStep(project.rootSimChip, project.inputPins, project.audioState.simAudio);
		}
	}
}