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
        private static string _currentMode = "NIDS"; // NIDS, CLOUD, IAM, KERNEL, DB, REDALERT
        private static string _previousMode = "NIDS";
        private static int _totalTicks = 0;

        // Shared simulation data structures
        private static readonly List<string> NidsLogs = new();
        private static readonly List<string> CloudLogs = new();
        private static readonly List<string> DbLogs = new();
        private static readonly List<string> KernelLogs = new();
        private static readonly List<int> ThreatHistory = new();
        private static readonly List<int> CloudCpuHistory = new();
        private static readonly List<int> DbIopsHistory = new();
        
        // Audit scanner trackers
        private static int _iamAuditedCount = 0;
        private static readonly List<string> IamViolations = new();
        private static readonly List<string> IamTreeAuditedNodes = new();
        
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

            // Populate initial logs
            for (int i = 0; i < 15; i++)
            {
                NidsLogs.Add(GenerateMockNidsLog());
                CloudLogs.Add(GenerateMockCloudLog());
                DbLogs.Add(GenerateMockDbLog());
                KernelLogs.Add(GenerateMockKernelLog());
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

            // Add new logs and limit size
            NidsLogs.Add(GenerateMockNidsLog());
            if (NidsLogs.Count > 30) NidsLogs.RemoveAt(0);

            CloudLogs.Add(GenerateMockCloudLog());
            if (CloudLogs.Count > 30) CloudLogs.RemoveAt(0);

            DbLogs.Add(GenerateMockDbLog());
            if (DbLogs.Count > 30) DbLogs.RemoveAt(0);

            // Kernel logs scroll faster - add multiple occasionally
            int kernelLines = Random.Shared.Next(1, 4);
            for (int k = 0; k < kernelLines; k++)
            {
                KernelLogs.Add(GenerateMockKernelLog());
                if (KernelLogs.Count > 40) KernelLogs.RemoveAt(0);
            }

            // Update IAM Audit stats
            _iamAuditedCount += Random.Shared.Next(50, 120);
            if (_iamAuditedCount > 25000) _iamAuditedCount = 0; // Wrap around

            if (Random.Shared.Next(0, 10) == 0)
            {
                IamViolations.Add(GenerateMockIamViolation());
                if (IamViolations.Count > 10) IamViolations.RemoveAt(0);
            }
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
                    "[bold]NAVIGATION:[/] [cyan]1/N[/] NIDS | [blue]2/C[/] Cloud | [yellow]3/I[/] IAM | [green]4/K[/] Kernel | [magenta]5/D[/] DB | [red]E/F[/] RED ALERT  " +
                    $"[bold]CONTROLS:[/] [white]Space[/] Pause ({(_isPaused ? "[yellow]PAUSED[/]" : "[green]RUNNING[/]")}) | [white]+/-[/] Speed ({1000.0 / _tickRateMs:F1} ticks/s) | [white]Q/ESC[/] Exit"
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
                "REDALERT" => RenderRedAlertMode(),
                _ => new Panel(new Text("Invalid Mode Selected")).Expand()
            };
        }

        #endregion

        #region Dashboard Renderer Methods

        private static IRenderable RenderNidsMode()
        {
            var leftPanel = new Panel(new Text(string.Join("\n", NidsLogs.TakeLast(16))))
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
            string spark2 = GenerateSparkline(ThreatHistory, 12); // mix sparkline data slightly for visual variety
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
            var rightPanel = new Panel(new Text(string.Join("\n", CloudLogs.TakeLast(15))))
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
                : string.Join("\n", IamViolations.TakeLast(6));

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
            // Full screen kernel log scroll (fast-moving log streams, looks highly technical)
            var logText = string.Join("\n", KernelLogs.TakeLast(22));
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

            var logsText = new Panel(new Text(string.Join("\n", DbLogs.TakeLast(8))))
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
                // Hex memory address print
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

        #endregion
    }

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
