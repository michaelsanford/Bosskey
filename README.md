# Bosskey: Simulated Operations & Cybersecurity Dashboard

A clever, high-fidelity TUI (Terminal User Interface) "boss key" tool written in C# .NET 10. It simulates realistic activity across multiple cybersecurity, cloud infrastructure, machine learning pipelines, and mainframe operations panels, providing an authentic-looking workload screen.

Developed using [Spectre.Console](https://spectreconsole.net/) for rich layout rendering, progress bars, trees, and custom text-based sparklines.

---

## 🛠️ Features

*   **Eight Interactive Dashboards**:
    1.  **NIDS (Network Intrusion Detection)**: Live packet capture streams, subnets under inspection, threat status levels, and live intrusion traffic sparklines.
    2.  **Cloud Operations Monitor**: Multi-cloud node group states, region statuses, deployment orchestrator log streams, and CPU load sparklines.
    3.  **IAM Security Audit**: Simulated scans of Active Directory Domain controllers, Organizational Units, privilege escalation paths, and policy violations.
    4.  **Kernel Telemetry Deep Trace**: High-speed, full-screen scrolling trace of low-level driver loading, page faults, thread scheduling, and hex memory dumps.
    5.  **Database Sentinel**: Replication lag trackers, master-replica sync offsets, slow query logging, and database transaction/IOPS sparklines.
    6.  **AI Model Training (NEW!)**: Real-time distributed pre-training status. Monitor loss/perplexity graphs, parameter models, token throughput speed, and GPU metrics (VRAM, temp, wattage) for an 8x H100 GPU cluster.
    7.  **Terraform Deployments (NEW!)**: Infrastructure-as-code run console. Highlights resource state changes (Pending -> Creating -> Complete) across target cloud instances and security groups.
    8.  **z/OS Mainframe Console (NEW!)**: Authentic retro green-on-black z/OS IBM system master console displaying active JES2 jobs, tape mounts, dataset operations, and COND codes.
*   **Emergency Lockdown Mode**: Instantly simulate a critical network breach containment procedure, flushing memory maps, revoking credentials, and isolating subnets.
*   **Live Simulation Speed Controls**: Dynamically speed up, slow down, or freeze the TUI display.

---

## ⌨️ Controls & Navigation

When the application is running, use the following keys to control it:

| Key | Action | Description |
| :--- | :--- | :--- |
| `1` / `N` | **NIDS Mode** | Switch to Network Intrusion Detection Panel |
| `2` / `C` | **Cloud Mode** | Switch to Cloud Container Orchestrator Panel |
| `3` / `I` | **IAM Audit** | Switch to Identity & Access Management AD Panel |
| `4` / `K` | **Kernel Mode** | Switch to Kernel Telemetry / Hex Dump Panel |
| `5` / `D` | **DB Sentinel** | Switch to Database Performance Panel |
| `6` / `A` | **AI Training** | Switch to AI/ML LLM Pre-training Panel |
| `7` / `T` | **Terraform Run** | Switch to Infrastructure-as-Code Run Panel |
| `8` / `M` | **z/OS Mainframe**| Switch to Retro IBM z/OS Monochrome Console Panel |
| `E` / `F` | **RED ALERT** | Toggles simulated System Failure/Breach Lockdown |
| `Space` | **Pause/Resume** | Freezes the simulation feeds (to "examine" data logs) |
| `+` | **Speed Up** | Increases tick rate (faster logs and graphics updates) |
| `-` | **Slow Down** | Decreases tick rate (slower updates) |
| `Q` / `ESC` | **Exit** | Restores terminal cursor and exits cleanly |

---

## 🚀 Building & Running

### Requirements
*   [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Run from Source
```powershell
dotnet run --configuration Release
```

### Build a Single-File Self-Contained Executable
To package the tool as a single, portable executable file with all dependencies embedded (no local .NET runtime required to run):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

The resulting executable will be located at:
`bin\Release\net10.0\win-x64\publish\Bosskey.exe`
