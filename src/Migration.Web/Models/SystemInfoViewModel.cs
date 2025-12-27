namespace DMS.Migration.Web.Models;

public class SystemInfoViewModel
{
    // Application
    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }

    // System
    public string MachineName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public bool Is64BitOS { get; set; }
    public string RuntimeVersion { get; set; } = string.Empty;

    // Process
    public int ProcessId { get; set; }
    public long WorkingSetMB { get; set; }
    public long PrivateMemoryMB { get; set; }
    public int ThreadCount { get; set; }

    // Docker
    public bool IsRunningInDocker { get; set; }
    public string DockerHostname { get; set; } = string.Empty;

    // Database
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int TotalJobs { get; set; }
    public int TotalTenants { get; set; }

    // Paths
    public string ContentRoot { get; set; } = string.Empty;
    public string WebRoot { get; set; } = string.Empty;
}
