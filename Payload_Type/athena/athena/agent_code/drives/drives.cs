﻿using Agent.Interfaces;
using System.Text.Json;
using Agent.Models;
using System.Text;

namespace Agent
{
    public class Plugin : IPlugin
    {
        public string Name => "drives";
        private IMessageManager messageManager { get; set; }
        private ILogger logger { get; set; }
        public Plugin(IMessageManager messageManager, IAgentConfig config, ILogger logger, ITokenManager tokenManager)
        {
            this.messageManager = messageManager;
            this.logger = logger;
        }

        public async Task Execute(ServerJob job)
        {
            try
            {
                List<dynamic> driveInfo = new List<dynamic>();
                var drives = DriveInfo.GetDrives();
                StringBuilder sb = new StringBuilder();
                foreach(var drive in drives)
                {
                    dynamic dyn = new System.Dynamic.ExpandoObject();
                    dyn.DriveName = drive.Name;
                    dyn.DriveType = drive.DriveType;
                    dyn.FreeSpace = drive.TotalFreeSpace;
                    dyn.TotalSpace = drive.TotalSize;
                    //    TotalSize = drive.TotalSize;
                    //dyn.VolumeLabel = drive.VolumeLabel;
                    //dyn.IsReady = drive.IsReady;
                    //dyn.RootDirectory = drive.RootDirectory;
                    //dyn.DriveFormat = drive.DriveFormat;
                    //dyn.AvailableFreeSpace = drive.AvailableFreeSpace;
                    driveInfo.Add(dyn);
                }

                string output = JsonSerializer.Serialize(driveInfo);
                await messageManager.AddResponse(new ResponseResult()
                {
                    task_id = job.task.id,
                    user_output = sb.ToString(),
                    completed = true
                });
            }
            catch (Exception e)
            {
                logger.Log(e.ToString());
            }
        }
    }
}
