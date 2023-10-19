using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics.Contracts;

namespace ForecastHub
{
    // Weather forecast and related data hub
    // Link between weather forecast data and other sw components
    // Coding by kjurlina
    // Have a lot of fun
    internal class Program
    {
        // Main program routine
        static async Task Main(string[] args)
        {
            // Create event handler for application closing event
            try
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, e) => CurrentDomain_ProcessExit(sender, e));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            // Log application starting
            Logger.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Logger.ToLogFile("Application started");

            // Open project data
            if (Project.Open())
            {
                Logger.ToLogFile("Project data loaded sucessfully");
            }
            else
            {
                Logger.ToLogFile("Failed to load project data. Exiting application");
            }

            // Configure and start cron jobs
            // Create and configure scheduler factory
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            // Define the job and trigger for both jobs
            IJobDetail CDataJob = JobBuilder.Create<CDataJob>()
                .WithIdentity("CDataJob", "CDataJobGroup")
                .Build();
            IJobDetail FDataJob = JobBuilder.Create<FDataJob>()
                .WithIdentity("FDataJob", "FDataJobGroup")
                .Build();
            IJobDetail RDataJob = JobBuilder.Create<RDataJob>()
                .WithIdentity("RDataJob", "RDataJobGroup")
                .Build();
            ITrigger CDataTrigger = TriggerBuilder.Create()
                .WithIdentity("CDataTrigger", "CDataTriggerGroup")
                .WithCronSchedule("0 15 * ? * *") 
                .Build();
            ITrigger FDataTrigger = TriggerBuilder.Create()
                .WithIdentity("FDataTrigger", "FDataTriggerGroup")
                .WithCronSchedule("0 0 6,18 * * ?")
                .Build();
            ITrigger RDataTrigger = TriggerBuilder.Create()
                .WithIdentity("RDataTrigger", "RDataTriggerGroup")
                .WithCronSchedule("0/10 * * ? * * *")
                .Build();

            // Schedule the jobs with the triggers
            await scheduler.ScheduleJob(CDataJob, CDataTrigger);
            await scheduler.ScheduleJob(FDataJob, FDataTrigger);
            await scheduler.ScheduleJob(RDataJob, RDataTrigger);
            // Start the scheduler
            await scheduler.Start();

            Logger.ToConsole("Scheduler started. Press Ctrl+C to exit application.");

            // Wait indefinitely (block the main thread)
            var waitHandle = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                waitHandle.Set();
            };

            waitHandle.WaitOne();
        }

        // Routine to handle current weather data
        public class CDataJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                using (CDHandler CDHandler = new CDHandler())
                {
                    (bool RetVal, List<string[]> Result) = CDHandler.FetchData();
                    if (RetVal)
                    {
                        using (SqlHandler SqlHandler = new SqlHandler())
                        {
                            SqlHandler.WriteCData(Result);
                        }
                    }
                    else
                    {
                        Logger.ToLogFile("Failed to fetch current weather data. Not calling SQL writer");
                    }
                }
                return Task.CompletedTask;
            }
        }

        // Routine to handle weather forecast data
        public class FDataJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                using (FDHandler FDHandler = new FDHandler())
                {
                    (bool RetVal, List<string[]> Result) = FDHandler.FetchData();
                    if (RetVal)
                    {
                        using (SqlHandler SqlHandler = new SqlHandler())
                        {
                            SqlHandler.WriteFData(Result);
                        }
                    }
                    else
                    {
                        Logger.ToLogFile("Failed to fetch weather forecast data. Not calling SQL writer");
                    }
                }
                return Task.CompletedTask;
            }
        }

        // Routine to handle runtime data
        public class RDataJob : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                using (RTHandler RTHandler = new RTHandler())
                {
                    (bool RetVal, List<string[]> Result) = RTHandler.FetchData();
                    if (RetVal)
                    {
                        using (SqlHandler SqlHandler = new SqlHandler())
                        {
                            SqlHandler.WriteFData(Result);
                        }
                    }
                    else
                    {
                        Logger.ToLogFile("Failed to fetch runtime data. Not calling SQL writer");
                    }
                }
                return Task.CompletedTask;
            }
        }

        // Exit routine
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Logger.ToLogFile("Application closed");
            Logger.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }
}
