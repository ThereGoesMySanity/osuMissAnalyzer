using DSharpPlus;
using System;
using System.Threading.Tasks;
using Mono.Unix;
using System.Threading;
using System.Net;
using OsuMissAnalyzer.Server.Database;
using System.IO;
using OsuMissAnalyzer.Core;
using DSharpPlus.Entities;
using System.Drawing;
using System.Text.RegularExpressions;
using ReplayAPI;
using System.Runtime.Caching.Generic;
using Mono.Options;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using DSharpPlus.EventArgs;
using System.Diagnostics;

namespace OsuMissAnalyzer.Server
{
    public class Program
    {
        static UnixPipes interruptPipe;
        [STAThread]
        public static void Main(string[] args)
        {
            interruptPipe = UnixPipes.CreatePipes();
            UnixSignal[] signals = new UnixSignal[] {
                new UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
                new UnixSignal(Mono.Unix.Native.Signum.SIGINT),
            };
            Thread signalThread = new Thread(delegate ()
            {
                int index = UnixSignal.WaitAny(signals);
                interruptPipe.Writing.Write(BitConverter.GetBytes(index), 0, 4);
            });
            signalThread.IsBackground = true;
            signalThread.Start();
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {
            var context = new ServerContext();
            if (!(await context.Init(args))) return;

            await Logger.WriteLine("Init complete");

            try
            {
                await context.Start();
                byte[] buffer = new byte[4];
                await interruptPipe.Reading.ReadAsync(buffer, 0, 4);
            }
            catch (Exception e) { await Logger.WriteLine(e, Logger.LogLevel.ALERT); }
            await context.Close();
            await Logger.WriteLine("Closed safely");
        }
    }
}