using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Bosskey
{
    class Program
    {
        private static bool _running = true;
        private static int _tickRateMs = 250;
        private static bool _isPaused = false;
        private static string _currentMode = "NIDS"; // NIDS, CLOUD, IAM, KERNEL, DB, AI, TERRAFORM, MAINFRAME, REDALERT
        private static string _previousMode = "NIDS";
        private static int _totalTicks = 0;

        // Shared simulation data structures
        private static readonly List<string> NidsLogs = new();
        private static readonly List<string> CloudLogs = new();
        private static readonly List<string> DbLogs = new();
        private static readonly List<string> KernelLogs = new();
        private static readonly List<string> AiLogs = new();
        private static readonly List<string> TfLogs = new();
        private static readonly List<string> MfLogs = new();

        // History buffers for sparklines
        private static readonly List<int> ThreatHistory = new();
        private static readonly List<int> CloudCpuHistory = new();
        private static readonly List<int> DbIopsHistory = new();
        private static readonly List<int> AiLossHistory = new();

        // IAM / Active Directory scanner trackers
        private static int _iamAuditedCount = 0;
        private static readonly List<string> IamViolations = new();

        // Terraform deployment tracker
        private static readonly List<TfResource> TfResources = new();
        private static int _tfStepIndex = 0;

        private static readonly string Hostname = Environment.MachineName.ToLower();

        static async Task Main(string[] args)
        {
            Console.Title = "Operations Dashboard";
            Console.OutputEncoding = Encoding.UTF8;

            // Hide cursor and clear
            AnsiConsole.Cursor.Hide();
            AnsiConsole.Clear();

            // Populate initial histories
            for (int i = 0; i < 20; i++) ThreatHistory.Add(Random.Shared.Next(40, 80));
            for (int i = 0; i < 20; i++) CloudCpuHistory.Add(Random.Shared.Next(20, 60));
            for (int i = 0; i < 20; i++) DbIopsHistory.Add(Random.Shared.Next(30, 90));
            for (int i = 0; i < 20; i++) AiLossHistory.Add(Random.Shared.Next(80, 95));

            // Initialize Terraform resources
            InitializeTfResources();

            // Populate initial logs
            for (int i = 0; i < 15; i++)
            {
                NidsLogs.Add(GenerateMockNidsLog());
                CloudLogs.Add(GenerateMockCloudLog());
                DbLogs.Add(GenerateMockDbLog());
                KernelLogs.Add(GenerateMockKernelLog());
                AiLogs.Add(GenerateMockAiLog());
                TfLogs.Add(GenerateMockTfLog());
                MfLogs.Add(GenerateMockMfLog());
            }

            // Start keyboard listener thread
            _ = Task.Run(ListenToKeys);

            // Setup Layout
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header").Size(3),
                    new Layout("Body"),
                    new Layout("Footer").Size(3)
                );

            // Run rendering loop using Spectre Live Display
            await AnsiConsole.Live(layout)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    while (_running)
                    {
                        if (!_isPaused)
                        {
                            UpdateSimulationData();
                        }

                        // Render layout components
                        layout["Header"].Update(GetHeaderPanel());
                        layout["Footer"].Update(GetFooterPanel());
                        layout["Body"].Update(GetBodyContent());

                        ctx.Refresh();
                        await Task.Delay(_tickRateMs);
                    }
                });

            // Cleanup console
            AnsiConsole.Cursor.Show();
            AnsiConsole.Clear();
            Console.ResetColor();
        }

        private static void ListenToKeys()
        {
            while (_running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D1:
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.N:
                            SwitchMode("NIDS");
                            break;
                        case ConsoleKey.D2:
                        case ConsoleKey.NumPad2:
                        case ConsoleKey.C:
                            SwitchMode("CLOUD");
                            break;
                        case ConsoleKey.D3:
                        case ConsoleKey.NumPad3:
                        case ConsoleKey.I:
                            SwitchMode("IAM");
                            break;
                        case ConsoleKey.D4:
                        case ConsoleKey.NumPad4:
                        case ConsoleKey.K:
                            SwitchMode("KERNEL");
                            break;
                        case ConsoleKey.D5:
                        case ConsoleKey.NumPad5:
                        case ConsoleKey.D:
                            SwitchMode("DB");
                            break;
                        case ConsoleKey.D6:
                        case ConsoleKey.NumPad6:
                        case ConsoleKey.A:
                            SwitchMode("AI");
                            break;
                        case ConsoleKey.D7:
                        case ConsoleKey.NumPad7:
                        case ConsoleKey.T:
                            SwitchMode("TERRAFORM");
                            break;
                        case ConsoleKey.D8:
                        case ConsoleKey.NumPad8:
                        case ConsoleKey.M:
                            SwitchMode("MAINFRAME");
                            break;
                        case ConsoleKey.E:
                        case ConsoleKey.F:
                            if (_currentMode == "REDALERT")
                            {
                                SwitchMode(_previousMode);
                            }
                            else
                            {
                                SwitchMode("REDALERT");
                            }
                            break;
                        case ConsoleKey.Spacebar:
                            _isPaused = !_isPaused;
                            break;
                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                            _tickRateMs = Math.Max(50, _tickRateMs - 50);
                            break;
                        case ConsoleKey.OemMinus:
                        case ConsoleKey.Subtract:
                            _tickRateMs = Math.Min(2000, _tickRateMs + 50);
                            break;
                        case ConsoleKey.Q:
                        case ConsoleKey.Escape:
                            _running = false;
                            break;
                    }
                }
                Thread.Sleep(50);
            }
        }

        private static void SwitchMode(string newMode)
        {
            if (_currentMode != newMode)
            {
                _previousMode = _currentMode;
                _currentMode = newMode;
                AnsiConsole.Clear(); // Force clear terminal on mode transition to avoid layout glitches
            }
        }

        private static void UpdateSimulationData()
        {
            _totalTicks++;

            // Shift history buffers
            ShiftHistory(ThreatHistory, Random.Shared.Next(-8, 9), 10, 100);
            ShiftHistory(CloudCpuHistory, Random.Shared.Next(-12, 13), 5, 95);
            ShiftHistory(DbIopsHistory, Random.Shared.Next(-15, 16), 15, 100);
            
            // AI Loss drops slowly but wiggles
            int lossWiggle = Random.Shared.Next(-3, 2);
            ShiftHistory(AiLossHistory, lossWiggle, 5, 100);

            // Add new logs and limit size
            NidsLogs.Add(GenerateMockNidsLog());
            if (NidsLogs.Count > 100) NidsLogs.RemoveAt(0);

            CloudLogs.Add(GenerateMockCloudLog());
            if (CloudLogs.Count > 100) CloudLogs.RemoveAt(0);

            DbLogs.Add(GenerateMockDbLog());
            if (DbLogs.Count > 100) DbLogs.RemoveAt(0);

            // Kernel logs scroll faster - add multiple occasionally
            int kernelLines = Random.Shared.Next(1, 4);
            for (int k = 0; k < kernelLines; k++)
            {
                KernelLogs.Add(GenerateMockKernelLog());
                if (KernelLogs.Count > 100) KernelLogs.RemoveAt(0);
            }

            AiLogs.Add(GenerateMockAiLog());
            if (AiLogs.Count > 100) AiLogs.RemoveAt(0);

            // Mainframe logs scroll at moderate rate
            if (Random.Shared.Next(0, 2) == 0)
            {
                MfLogs.Add(GenerateMockMfLog());
                if (MfLogs.Count > 100) MfLogs.RemoveAt(0);
            }

            // Update IAM Audit stats
            _iamAuditedCount += Random.Shared.Next(50, 120);
            if (_iamAuditedCount > 25000) _iamAuditedCount = 0; // Wrap around

            if (Random.Shared.Next(0, 10) == 0)
            {
                IamViolations.Add(GenerateMockIamViolation());
                if (IamViolations.Count > 30) IamViolations.RemoveAt(0);
            }

            // Update Terraform deploy steps
            UpdateTfSimulation();
        }

        private static void ShiftHistory(List<int> history, int delta, int min, int max)
        {
            int newVal = history[^1] + delta;
            newVal = Math.Clamp(newVal, min, max);
            history.Add(newVal);
            if (history.Count > 40) history.RemoveAt(0);
        }

        #region UI Rendering Builders

        private static Panel GetHeaderPanel()
        {
            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string modeName = _currentMode switch
            {
                "NIDS" => "[cyan]NETWORK INTRUSION DETECTION[/]",
                "CLOUD" => "[blue]CLOUD CONTAINER ORCHESTRATOR[/]",
                "IAM" => "[yellow]IDENTITY & ACCESS MANAGEMENT SECURITY AUDIT[/]",
                "KERNEL" => "[green]KERNEL TELEMETRY DEEP TRACE[/]",
                "DB" => "[magenta]DATABASE SENTINEL & PERFORMANCE COUNTERS[/]",
                "AI" => "[blue]AI/ML DISTRIBUTED MODEL TRAINING PIPELINE[/]",
                "TERRAFORM" => "[cyan]DEVOP INFRASTRUCTURE DEPLOYMENT (TERRAFORM)[/]",
                "MAINFRAME" => "[green]IBM SYSTEM Z/OS MONOCHROME MAIN CONSOLE[/]",
                "REDALERT" => "[red]!!! EMERGENCY SYSTEM RECOVERY LOCKDOWN !!![/]",
                _ => ""
            };

            var grid = new Grid().AddColumns(2);
            grid.AddRow(
                new Markup($"[bold white]HQ-OPS-DASHBOARD[/] :: {modeName}"),
                new Markup($"[bold white]NODE:[/] [cyan]{Hostname}[/]  [bold white]TIME:[/] [yellow]{timeStr}[/]  [bold white]STATUS:[/] [green]ONLINE[/]")
            );

            return new Panel(grid)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey35)
                .Expand();
        }

        private static Panel GetFooterPanel()
        {
            var grid = new Grid().AddColumns(1);
            grid.AddRow(
                new Markup(
                    "[bold]NAV:[/] [cyan]1[/] NIDS|[blue]2[/] Cloud|[yellow]3[/] IAM|[green]4[/] Kernel|[magenta]5[/] DB|[blue]6[/] AI|[cyan]7[/] TF|[yellow]8[/] MF|[red]E[/] RED ALERT " +
                    $"[bold]CTRL:[/] [white]Space[/] Pause ({(_isPaused ? "[yellow]PAUSED[/]" : "[green]RUNNING[/]")}) | [white]+/-[/] Speed ({1000.0 / _tickRateMs:F1} ticks/s) | [white]Q/ESC[/] Exit"
                )
            );
            return new Panel(grid)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey35)
                .Expand();
        }

        private static IRenderable GetBodyContent()
        {
            return _currentMode switch
            {
                "NIDS" => RenderNidsMode(),
                "CLOUD" => RenderCloudMode(),
                "IAM" => RenderIamMode(),
                "KERNEL" => RenderKernelMode(),
                "DB" => RenderDbMode(),
                "AI" => RenderAiMode(),
                "TERRAFORM" => RenderTfMode(),
                "MAINFRAME" => RenderMfMode(),
                "REDALERT" => RenderRedAlertMode(),
                _ => new Panel(new Text("Invalid Mode Selected")).Expand()
            };
        }

        private static int GetAvailableLogHeight(int padding = 8)
        {
            try
            {
                return Math.Max(5, Console.WindowHeight - padding);
            }
            catch
            {
                return 15;
            }
        }

        #endregion

        #region Dashboard Renderer Methods

        private static IRenderable RenderNidsMode()
        {
            var leftPanel = new Panel(new Text(string.Join("\n", NidsLogs.TakeLast(GetAvailableLogHeight(8)))))
                .Header("[green] Live Packet Capture & Inspection (DPI) [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Green)
                .Expand();

            // Threat gauge
            int currentThreat = ThreatHistory[^1];
            string threatColor = currentThreat > 75 ? "red" : currentThreat > 45 ? "yellow" : "green";
            string bar = new string('█', currentThreat / 10) + new string('░', 10 - (currentThreat / 10));

            var rightGrid = new Grid().AddColumns(1);
            rightGrid.AddRow(new Markup($"[bold]Threat Signature Match Level:[/] [{threatColor}]{bar} {currentThreat}%[/]"));
            rightGrid.AddEmptyRow();

            // Subnet status table
            var table = new Table().AddColumns("Subnet Focus", "Risk Score", "State");
            table.Border(TableBorder.Square);
            table.BorderColor(Color.Grey35);
            table.AddRow("10.120.4.0/24", "[green]12 (Low)[/]", "PASSING");
            table.AddRow("172.16.80.0/20", $"[{threatColor}]{Random.Shared.Next(60, 95)} (High)[/]", "INSPECTING");
            table.AddRow("192.168.1.0/24", "[green]5 (None)[/]", "MONITORED");
            table.AddRow("10.0.99.0/24", "[yellow]48 (Med)[/]", "DEEP-SCAN");

            rightGrid.AddRow(new Panel(table).Header("[yellow] Active Intrusive Target Groups [/]").BorderColor(Color.Grey35));
            rightGrid.AddEmptyRow();

            // Sparkline of network intrusion pressure
            string sparkline = GenerateSparkline(ThreatHistory, 25);
            rightGrid.AddRow(new Markup($"[bold]Intrusion Attempt Activity (40s):[/]\n[cyan]{sparkline}[/]"));

            var rightPanel = new Panel(rightGrid)
                .Header("[cyan] Threat Analytics [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Cyan)
                .Expand();

            return new Grid()
                .AddColumns(2)
                .AddRow(leftPanel, rightPanel);
        }

        private static IRenderable RenderCloudMode()
        {
            // Left panel: Regions status
            var table = new Table().AddColumns("Region Identifier", "Status", "Pods", "IOPS load", "Telemetry (CPU)");
            table.Border(TableBorder.Square);
            table.BorderColor(Color.Blue);

            string spark1 = GenerateSparkline(CloudCpuHistory, 12);
            string spark2 = GenerateSparkline(ThreatHistory, 12);
            string spark3 = GenerateSparkline(DbIopsHistory, 12);

            table.AddRow("aws.us-east-1 (N. Virginia)", "[green]HEALTHY[/]", "142 / 142", "2.1k/s", $"[green]{spark1}[/]");
            table.AddRow("aws.eu-west-1 (Ireland)", "[green]HEALTHY[/]", "98 / 98", "840/s", $"[green]{spark2}[/]");
            table.AddRow("gcp.us-central-1 (Iowa)", "[yellow]DEGRADED[/]", "84 / 92", "4.3k/s", $"[yellow]{spark3}[/]");
            table.AddRow("azure.westeurope (Amsterdam)", "[green]HEALTHY[/]", "110 / 110", "1.9k/s", $"[green]{spark1}[/]");
            table.AddRow("aws.ap-northeast-1 (Tokyo)", "[red]WARNING[/]", "31 / 45", "110/s", $"[red]{spark2}[/]");

            var leftPanel = new Panel(table)
                .Header("[blue] Kubernetes Cluster & Node Group Registry [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Blue)
                .Expand();

            // Right panel: Orchestration logs
            var rightPanel = new Panel(new Text(string.Join("\n", CloudLogs.TakeLast(GetAvailableLogHeight(8)))))
                .Header("[cyan] Orchestrator Pod Activity Logs [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Cyan)
                .Expand();

            return new Grid()
                .AddColumns(2)
                .AddRow(leftPanel, rightPanel);
        }

        private static IRenderable RenderIamMode()
        {
            // Left: AD Tree Mock
            var tree = new Tree("[yellow]Domain: Corp.Enterprise.Local[/]");
            var dcNode = tree.AddNode("[grey]└─ OU=DomainControllers[/]");
            dcNode.AddNode("[green]├─ DC-PRD-01.corp.internal (AD-DS)[/]");
            dcNode.AddNode("[green]└─ DC-PRD-02.corp.internal (AD-DS)[/]");

            var groupNode = tree.AddNode("[grey]└─ OU=SecurityGroups[/]");
            groupNode.AddNode("[red]├─ CN=Domain Admins (Delegated)[/]");
            groupNode.AddNode("[yellow]├─ CN=Enterprise Admins (Audited)[/]");
            groupNode.AddNode("[grey]└─ CN=Backup Operators[/]");

            var usersNode = tree.AddNode("[grey]└─ OU=Accounts[/]");
            usersNode.AddNode($"[green]├─ CN=Administrator (Last scan: {_totalTicks % 10}s ago)[/]");
            usersNode.AddNode("[yellow]├─ CN=svc_backup (Scanning privilege paths...)[/]");
            usersNode.AddNode("[green]└─ CN=msanford (Audited)[/]");

            var leftPanel = new Panel(tree)
                .Header("[yellow] Active Directory Structural Audit [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Yellow)
                .Expand();

            // Right: Audit telemetry & findings
            var rightGrid = new Grid().AddColumns(1);
            rightGrid.AddRow(new Markup($"[bold]Directory Audit Progress:[/] [yellow]{_iamAuditedCount} accounts analyzed[/]"));

            // Audit progress bar
            int progress = (_iamAuditedCount * 100) / 25000;
            string bar = new string('█', progress / 10) + new string('░', 10 - (progress / 10));
            rightGrid.AddRow(new Markup($"[yellow]{bar} {progress}% Complete[/]"));
            rightGrid.AddEmptyRow();

            // Violations panel
            string violationsStr = IamViolations.Count == 0
                ? "[green]No critical identity path compromises identified.[/]"
                : string.Join("\n", IamViolations.TakeLast(GetAvailableLogHeight(14)));

            var violationsPanel = new Panel(new Text(violationsStr))
                .Header("[red] Critical Escalation Match Vector Path Alerts [/]")
                .BorderColor(Color.Red)
                .Expand();

            rightGrid.AddRow(violationsPanel);

            var rightPanel = new Panel(rightGrid)
                .Header("[cyan] Audit Parameters [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Cyan)
                .Expand();

            return new Grid()
                .AddColumns(2)
                .AddRow(leftPanel, rightPanel);
        }

        private static IRenderable RenderKernelMode()
        {
            var logText = string.Join("\n", KernelLogs.TakeLast(GetAvailableLogHeight(8)));
            return new Panel(new Text(logText))
                .Header("[green] Low-Level System Kernel Telemetry & ETW Ringbuffer Trace [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Green)
                .Expand();
        }

        private static IRenderable RenderDbMode()
        {
            // Left Panel: Databases Sync status
            var table = new Table().AddColumns("Database Name", "Role / Nodes", "Sync State", "Lag Offset", "Tablespace");
            table.Border(TableBorder.Square);
            table.BorderColor(Color.Magenta);

            table.AddRow("sales_db", "Master (10.0.5.4)", "[green]SYNCED[/]", "0.02ms", "[green]42% (240GB)[/]");
            table.AddRow("sales_db_replica", "Replica (10.0.5.12)", "[green]SYNCED[/]", "0.14ms", "[green]42% (240GB)[/]");

            int lag = DbIopsHistory[^1];
            string lagStatus = lag > 75 ? "[red]CATCHING UP[/]" : lag > 45 ? "[yellow]SLOWING[/]" : "[green]SYNCED[/]";
            string lagValue = lag > 75 ? $"{(float)lag/10:F1}s" : $"{(float)lag/100:F2}ms";

            table.AddRow("telemetry_db", "Master (10.0.6.9)", "[green]SYNCED[/]", "0.01ms", "[yellow]81% (1.8TB)[/]");
            table.AddRow("telemetry_replica", "Replica (10.0.6.14)", lagStatus, lagValue, "[yellow]81% (1.8TB)[/]");
            table.AddRow("billing_shard_01", "Master (10.0.7.2)", "[green]SYNCED[/]", "0.04ms", "[green]18% (90GB)[/]");

            var leftPanel = new Panel(table)
                .Header("[magenta] SQL Sentinel cluster Replication Monitor [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Magenta)
                .Expand();

            // Right Panel: IOPS Graph and query logs
            var rightGrid = new Grid().AddColumns(1);
            string sparkline = GenerateSparkline(DbIopsHistory, 25);
            rightGrid.AddRow(new Markup($"[bold]Database Lock IOPS Activity (40s):[/]\n[magenta]{sparkline}[/]"));
            rightGrid.AddEmptyRow();

            var logsText = new Panel(new Text(string.Join("\n", DbLogs.TakeLast(GetAvailableLogHeight(13)))))
                .Header("[cyan] Slow Query Logs & Deadlock Tracer [/]")
                .BorderColor(Color.Grey35)
                .Expand();

            rightGrid.AddRow(logsText);

            var rightPanel = new Panel(rightGrid)
                .Header("[cyan] DB Telemetry [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Cyan)
                .Expand();

            return new Grid()
                .AddColumns(2)
                .AddRow(leftPanel, rightPanel);
        }

        private static IRenderable RenderAiMode()
        {
            // Left Panel: GPU Metrics
            var table = new Table().AddColumns("GPU ID", "Utilization", "Temp", "Memory VRAM", "Power");
            table.Border(TableBorder.Square);
            table.BorderColor(Color.Blue);

            for (int i = 0; i < 8; i++)
            {
                // dynamic wiggling values
                int util = Math.Clamp(95 + Random.Shared.Next(-5, 6) - (i == 3 ? 15 : 0), 10, 100);
                int temp = Math.Clamp(72 + Random.Shared.Next(-4, 5) + (util / 10), 40, 95);
                double vramUsed = 78.2 + (Random.Shared.NextDouble() * 1.2);
                int power = 350 + Random.Shared.Next(-30, 45) + (util * 2);

                string utilColor = util > 90 ? "red" : util > 70 ? "yellow" : "green";
                string utilBar = new string('█', util / 10) + new string('░', 10 - (util / 10));

                table.AddRow(
                    $"H100-SXM-{i}",
                    $"[{utilColor}]{utilBar} {util}%[/]",
                    $"{temp}°C",
                    $"{vramUsed:F1} GB / 80.0 GB",
                    $"{power}W / 700W"
                );
            }

            var leftPanel = new Panel(table)
                .Header("[blue] Nvidia Cluster Node GPU Telemetry [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Blue)
                .Expand();

            // Right Panel: Training Stats & Checkpoints
            var rightGrid = new Grid().AddColumns(1);
            
            // AI model information
            rightGrid.AddRow(new Markup("[bold]Model Architecture:[/] [yellow]Transformer-Decoder (70B parameters)[/]"));
            rightGrid.AddRow(new Markup($"[bold]Precision Context:[/] [green]FP8 Distributed Hybrid[/]   [bold]Tokens/sec:[/] [white]{4280 + Random.Shared.Next(-150, 150)}/s[/]"));
            rightGrid.AddEmptyRow();

            // Training Loss Sparkline
            float lossVal = (float)AiLossHistory[^1] / 10.0f;
            string sparkline = GenerateSparkline(AiLossHistory, 25);
            rightGrid.AddRow(new Markup($"[bold]Training Cross-Entropy Loss (Current: {lossVal:F3}):[/]\n[cyan]{sparkline}[/]"));
            rightGrid.AddEmptyRow();

            // Eval logs
            var logsPanel = new Panel(new Text(string.Join("\n", AiLogs.TakeLast(GetAvailableLogHeight(16)))))
                .Header("[cyan] Checkpoint Evaluation Prompts [/]")
                .BorderColor(Color.Grey35)
                .Expand();

            rightGrid.AddRow(logsPanel);

            var rightPanel = new Panel(rightGrid)
                .Header("[cyan] AI Training Telemetry [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Cyan)
                .Expand();

            return new Grid()
                .AddColumns(2)
                .AddRow(leftPanel, rightPanel);
        }

        private static IRenderable RenderTfMode()
        {
            // Left Panel: Terraform Resource status list
            var table = new Table().AddColumns("Terraform Address", "Resource Type", "Change", "Execution State");
            table.Border(TableBorder.Square);
            table.BorderColor(Color.Cyan);

            foreach (var r in TfResources)
            {
                string stateStr = r.State switch
                {
                    ResourceState.Pending => "[grey]PENDING[/]",
                    ResourceState.Creating => "[yellow]CREATING...[/]",
                    ResourceState.Modifying => "[yellow]MODIFYING...[/]",
                    ResourceState.Destroying => "[red]DESTROYING...[/]",
                    ResourceState.Complete => "[green]COMPLETE[/]",
                    _ => ""
                };

                string actionStr = r.Action switch
                {
                    ResourceAction.Add => "[green]+ ADD[/]",
                    ResourceAction.Modify => "[yellow]~ MODIFY[/]",
                    ResourceAction.Destroy => "[red]- DESTROY[/]",
                    _ => ""
                };

                table.AddRow(new Text(r.Address), new Text(r.Type), new Markup(actionStr), new Markup(stateStr));
            }

            var leftPanel = new Panel(table)
                .Header("[cyan] Active Infrastructure Operations (IAC Plan) [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Cyan)
                .Expand();

            // Right Panel: Terraform live execution log
            var rightPanel = new Panel(new Text(string.Join("\n", TfLogs.TakeLast(GetAvailableLogHeight(8)))))
                .Header("[green] Terraform Run Console stdout [/]")
                .Border(BoxBorder.Square)
                .BorderColor(Color.Green)
                .Expand();

            return new Grid()
                .AddColumns(2)
                .AddRow(leftPanel, rightPanel);
        }

        private static IRenderable RenderMfMode()
        {
            // Retro monochrome IBM system z/OS layout
            var logText = string.Join("\n", MfLogs.TakeLast(GetAvailableLogHeight(8)));
            return new Panel(new Text(logText))
                .Header("[greenyellow] IBM z/OS System Master Console -- Display Console (CON1) [/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.GreenYellow)
                .Expand();
        }

        private static IRenderable RenderRedAlertMode()
        {
            var warningText = new StringBuilder();
            warningText.AppendLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            warningText.AppendLine("!!!                     CRITICAL INFRASTRUCTURE EMERGENCY                    !!!");
            warningText.AppendLine("!!!              SECURITY BREACH DETECTED - FORCED LOCKDOWN INITIATED        !!!");
            warningText.AppendLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            warningText.AppendLine();
            warningText.AppendLine($"[LOCKDOWN] Host: {Hostname}");
            warningText.AppendLine($"[LOCKDOWN] Trigger Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            warningText.AppendLine("[LOCKDOWN] Vector: Suspicious identity paths matching Domain Admin privileges");
            warningText.AppendLine("[LOCKDOWN] Action: Dropping all active connections and wiping ephemeral keys");
            warningText.AppendLine();
            warningText.AppendLine("Running secure shutdown sequence:");

            int progress = Math.Min(100, (_totalTicks * 7) % 110);

            warningText.AppendLine(progress > 15 ? " -> Revoking IAM API tokens... [SUCCESS]" : " -> Revoking IAM API tokens... [IN PROGRESS]");
            warningText.AppendLine(progress > 35 ? " -> Disabling VPC gateway endpoints... [SUCCESS]" : " -> Disabling VPC gateway endpoints... [WAITING]");
            warningText.AppendLine(progress > 55 ? " -> Purging Active Directory Kerberos TGT tickets... [SUCCESS]" : " -> Purging Active Directory Kerberos TGT tickets... [WAITING]");
            warningText.AppendLine(progress > 85 ? " -> Flushing memory mapped caches... [SUCCESS]" : " -> Flushing memory mapped caches... [WAITING]");
            warningText.AppendLine(progress >= 100 ? " -> Firewall isolation established. Subnet quarantine active. [LOCKED]" : " -> Establishing firewall isolation... [WAITING]");

            return new Panel(new Markup($"[bold red]{Markup.Escape(warningText.ToString())}[/]"))
                .Header("[blink red] !!! SYSTEM FAILURE DETECTED !!! [/]")
                .Border(BoxBorder.Double)
                .BorderColor(Color.Red)
                .Expand();
        }

        #endregion

        #region Sparkline Helper

        private static string GenerateSparkline(IEnumerable<int> values, int width)
        {
            char[] blocks = { ' ', ' ', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
            var list = values.TakeLast(width).ToList();
            while (list.Count < width) list.Insert(0, 0);
            return string.Concat(list.Select(v =>
            {
                int idx = (v * (blocks.Length - 1)) / 100;
                idx = Math.Clamp(idx, 0, blocks.Length - 1);
                return blocks[idx];
            }));
        }

        #endregion

        #region Mock Data Generators

        private static readonly string[] Protocols = { "TCP", "UDP", "ICMP" };
        private static readonly string[] FirewallActions = { "[green]ALLOW[/]", "[red]BLOCK[/]", "[yellow]INSPECT[/]" };
        private static readonly string[] CloudServices = { "aws.iam.role-eval", "aws.s3.asset-storage", "gcp.gke.node-pool-1", "gcp.gke.billing-db", "azure.vm.payment-gateway", "azure.blob.data-lake" };
        private static readonly string[] DbQueryTypes = { "SELECT", "INSERT", "UPDATE", "DELETE", "VACUUM", "ALTER" };
        private static readonly string[] Systems = { "ACPI", "PCI", "systemd", "EXT4-fs", "kernel", "auditd", "dockerd" };

        private static readonly string[] AiPrompts = {
            "Prompt: 'Draft an architectural plan for cloud migration...'",
            "Prompt: 'Optimize the following SQL deadlock transaction...'",
            "Prompt: 'Generate a script to audit open S3 bucket credentials...'",
            "Prompt: 'Explain quantum computing key distribution concepts...'"
        };

        private static readonly string[] MfJobs = { "RECON1", "DBBACKUP", "PAYROLL", "MONLOG", "CICSGRP", "RACFAUD" };
        private static readonly string[] MfSteps = { "STEP01", "STEP02", "INITS", "INDEXING", "FLUSHING" };

        private static string GenerateMockNidsLog()
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            string proto = Protocols[Random.Shared.Next(Protocols.Length)];
            string act = FirewallActions[Random.Shared.Next(FirewallActions.Length)];
            string srcIp = $"{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}";
            string dstIp = $"10.120.4.{Random.Shared.Next(1, 254)}";
            int srcPort = Random.Shared.Next(1024, 65535);
            int dstPort = Random.Shared.Next(1, 1024);

            return $"[{time}] {act} {proto} {srcIp}:{srcPort} -> {dstIp}:{dstPort} (size={Random.Shared.Next(40, 1500)}B)";
        }

        private static string GenerateMockCloudLog()
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string service = CloudServices[Random.Shared.Next(CloudServices.Length)];
            string level = Random.Shared.Next(0, 8) switch
            {
                0 => "[red]ERROR[/]",
                1 => "[yellow]WARN[/]",
                2 => "[yellow]WARN[/]",
                _ => "[cyan]INFO[/]"
            };

            string message = level switch
            {
                "[red]ERROR[/]" => $"API gateway failed to authorize endpoint request from token context path '/v2/deploy'",
                "[yellow]WARN[/]" => $"Container resource limit reached for pod node agent, auto-restructure initiated",
                _ => $"Successfully dispatched worker container thread to execution scheduler (node-pool-2)"
            };

            return $"[{time}] {level} [{service}] {message}";
        }

        private static string GenerateMockDbLog()
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string type = DbQueryTypes[Random.Shared.Next(DbQueryTypes.Length)];
            int duration = Random.Shared.Next(200, 3500);

            return duration > 1000
                ? $"[{time}] [red]SLOW[/] {type} query executed in {duration}ms: transaction root rollback required"
                : $"[{time}] [green]OK[/] {type} operation on table 'metrics_store' resolved (0.04ms)";
        }

        private static string GenerateMockKernelLog()
        {
            string time = (_totalTicks * 0.05 + Random.Shared.NextDouble()).ToString("F4");
            string sys = Systems[Random.Shared.Next(Systems.Length)];

            if (Random.Shared.Next(0, 4) == 0)
            {
                ulong address = (ulong)Random.Shared.NextLong(0x7FFF00000000, 0x7FFFFFFFFFFF);
                return $"[{time}] [grey]0x{address:X12}:  {Random.Shared.Next(10, 99)} {Random.Shared.Next(10, 99)} {Random.Shared.Next(10, 99)} {Random.Shared.Next(10, 99)}  {Random.Shared.Next(10, 99)} {Random.Shared.Next(10, 99)} {Random.Shared.Next(10, 99)} {Random.Shared.Next(10, 99)}[/]";
            }

            string msg = sys switch
            {
                "kernel" => "x86/PAT: Configuration [0-7]: WB WC UC- UC WB WP UC- UC",
                "ACPI" => "Core revision 20260115 loaded successfully",
                "systemd" => $"Reached target System Initialization. Load time: {Random.Shared.Next(20, 80)}ms",
                "EXT4-fs" => "mounted filesystem with ordered data mode. Opts: (null)",
                "auditd" => "Audit daemon logging started (version 3.0)",
                _ => "Driver state initialized: class 0x060400 status 0"
            };

            return $"[{time}] [{Color.SteelBlue}]{sys}[/]: {msg}";
        }

        private static string GenerateMockIamViolation()
        {
            string[] users = { "svc_jenkins", "backup_agent", "guest_user", "contractor_12" };
            string[] vectors = { "unconstrained delegation path", "expired password credential access", "wide IAM policy modification authorization" };

            return $"[red]ALERT[/] Identity '{users[Random.Shared.Next(users.Length)]}' matches path via '{vectors[Random.Shared.Next(vectors.Length)]}'";
        }

        private static string GenerateMockAiLog()
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string prompt = AiPrompts[Random.Shared.Next(AiPrompts.Length)];
            
            // Generate mock checkpoint responses
            string response = prompt.Contains("cloud migration")
                ? "Checkpoint: recommended multi-region node scaling with VPC isolation"
                : prompt.Contains("deadlock")
                ? "Checkpoint: resolved share-lock conflicts on table order_items"
                : prompt.Contains("S3")
                ? "Checkpoint: scanned 180 buckets, flagged 3 with public access policies"
                : "Checkpoint: verified BB84 protocol entanglement threshold (99.8%)";

            return $"[{time}] {prompt}\n[{time}]   └─ {response}";
        }

        private static string GenerateMockTfLog()
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            if (TfLogs.Count == 0)
            {
                return $"[{time}] Terraform v1.5.0 initialized on platform win-x64";
            }
            
            // We just grab from the current pipeline runner step
            return $"[{time}] runner output trace dispatched (PID={Random.Shared.Next(4000, 9000)})";
        }

        private static string GenerateMockMfLog()
        {
            string time = DateTime.Now.ToString("HH.mm.ss");
            string job = MfJobs[Random.Shared.Next(MfJobs.Length)];
            string step = MfSteps[Random.Shared.Next(MfSteps.Length)];

            return Random.Shared.Next(0, 5) switch
            {
                0 => $"{time} JOB{Random.Shared.Next(1000, 9999):D5} $HASP373 {job} STARTED - INIT {Random.Shared.Next(1, 6)} - CLASS A",
                1 => $"{time} JOB{Random.Shared.Next(1000, 9999):D5} IEF403I {job} - STARTED - TIME={time}",
                2 => $"{time} JOB{Random.Shared.Next(1000, 9999):D5} IEF142I {job} {step} - STEP WAS EXECUTED - COND CODE {Random.Shared.Next(0, 4) * 4:D4}",
                3 => $"{time} SYSTEM   IEE104I SESSION 01 ACTIVE - HOST CONSOLE ATTACHED",
                _ => $"{time} JOB{Random.Shared.Next(1000, 9999):D5} $HASP395 {job} ENDED - COND CODE 0000"
            };
        }

        #endregion

        #region Terraform Simulation Engine

        private static void InitializeTfResources()
        {
            TfResources.Clear();
            TfResources.Add(new TfResource("aws_vpc.vpc_main", "aws_vpc", ResourceAction.Add));
            TfResources.Add(new TfResource("aws_subnet.subnet_a", "aws_subnet", ResourceAction.Add));
            TfResources.Add(new TfResource("aws_security_group.allow_web", "aws_security_group", ResourceAction.Add));
            TfResources.Add(new TfResource("aws_iam_role.ec2_admin", "aws_iam_role", ResourceAction.Modify));
            TfResources.Add(new TfResource("aws_instance.web_servers[0]", "aws_instance", ResourceAction.Add));
            TfResources.Add(new TfResource("aws_instance.web_servers[1]", "aws_instance", ResourceAction.Add));
            TfResources.Add(new TfResource("aws_db_instance.db_master", "aws_db_instance", ResourceAction.Add));
            TfResources.Add(new TfResource("aws_iam_policy.wildcard_dev", "aws_iam_policy", ResourceAction.Destroy));
        }

        private static void UpdateTfSimulation()
        {
            if (TfResources.Count == 0) return;

            // Simple state machine for Terraform resources
            // Every few ticks, we transition one of the resources
            if (Random.Shared.Next(0, 3) == 0)
            {
                var target = TfResources.FirstOrDefault(r => r.State != ResourceState.Complete);
                if (target != null)
                {
                    if (target.State == ResourceState.Pending)
                    {
                        target.State = target.Action == ResourceAction.Destroy ? ResourceState.Destroying : ResourceState.Creating;
                        TfLogs.Add($"[{DateTime.Now:HH:mm:ss}] {target.Address}: {target.State.ToString().ToUpper()}...");
                    }
                    else if (target.State == ResourceState.Creating || target.State == ResourceState.Destroying || target.State == ResourceState.Modifying)
                    {
                        target.State = ResourceState.Complete;
                        string actionPastTense = target.Action == ResourceAction.Destroy ? "Destruction" : "Creation";
                        TfLogs.Add($"[{DateTime.Now:HH:mm:ss}] {target.Address}: {actionPastTense} complete after {Random.Shared.Next(5, 18)}s");
                    }
                }
                else
                {
                    // All are complete! Start a new cycle after a short wait
                    _tfStepIndex++;
                    if (_tfStepIndex % 15 == 0)
                    {
                        TfLogs.Add($"[{DateTime.Now:HH:mm:ss}] Refreshing Terraform state lookup...");
                        InitializeTfResources();
                    }
                }
            }
        }

        #endregion
    }

    #region Helper Types for Terraform Simulator

    public enum ResourceState
    {
        Pending,
        Creating,
        Modifying,
        Destroying,
        Complete
    }

    public enum ResourceAction
    {
        Add,
        Modify,
        Destroy
    }

    public class TfResource
    {
        public string Address { get; set; }
        public string Type { get; set; }
        public ResourceAction Action { get; set; }
        public ResourceState State { get; set; }

        public TfResource(string address, string type, ResourceAction action)
        {
            Address = address;
            Type = type;
            Action = action;
            State = ResourceState.Pending;
        }
    }

    #endregion

    public static class RandomExtensions
    {
        public static long NextLong(this Random random, long min, long max)
        {
            if (max <= min) return min;
            ulong uRange = (ulong)(max - min);
            ulong ulongRand;
            do
            {
                byte[] buf = new byte[8];
                random.NextBytes(buf);
                ulongRand = BitConverter.ToUInt64(buf, 0);
            } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

            return (long)(ulongRand % uRange) + min;
        }
    }
}
